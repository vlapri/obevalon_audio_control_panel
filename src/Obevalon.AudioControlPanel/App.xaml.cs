// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Threading;
using Obevalon.AudioControlPanel.Core;
using Obevalon.AudioControlPanel.Core.Autostart;
using Obevalon.AudioControlPanel.Core.Settings;
using Obevalon.AudioControlPanel.Localization;
using Obevalon.AudioControlPanel.Platform.Autostart;
using Obevalon.AudioControlPanel.Platform.CoreAudio;
using Obevalon.AudioControlPanel.Platform.Shell;
using Obevalon.AudioControlPanel.Themes;
using Obevalon.AudioControlPanel.Tray;
using Obevalon.AudioControlPanel.ViewModels;
using Obevalon.AudioControlPanel.Views;

namespace Obevalon.AudioControlPanel;

public partial class App : Application
{
    /// <summary>How long a restarted (updated) instance waits for the previous one to exit.</summary>
    private static readonly TimeSpan PreviousInstanceTimeout = TimeSpan.FromSeconds(10);

    private SingleInstance? _singleInstance;
    private SettingsManager? _settings;
    private AudioDeviceService? _audio;
    private MicrophoneService? _microphone;
    private MainViewModel? _viewModel;
    private MainWindow? _window;
    private TrayIconController? _tray;
    private IAutostartService? _autostart;
    private SettingsWindow? _settingsWindow;
    private bool _exiting;

    internal StartupOptions Options { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        Options = StartupOptions.Parse(e.Args);

        WaitForPreviousProcess(Options.WaitForProcessId);
        var timeout = Options.WaitForProcessId is null && !Options.Updated ? TimeSpan.Zero : PreviousInstanceTimeout;
        _singleInstance = SingleInstance.TryAcquire(timeout, () => Dispatcher.BeginInvoke(ShowMainWindow));
        if (_singleInstance is null)
        {
            // Already running: bring that instance to the front and quit (SPEC F3.7).
            SingleInstance.SignalRunningInstance();
            Shutdown();
            return;
        }

        _settings = new SettingsManager(CreateSettingsStore());
        LocalizationManager.Instance.SetLanguage(_settings.Current.Language);
        ThemeManager.Apply(_settings.Current.Theme);

        // Audio services post their notifications to the UI thread through this context.
        var uiContext = SynchronizationContext.Current ?? new DispatcherSynchronizationContext(Dispatcher);
        try
        {
            _audio = new AudioDeviceService(uiContext);
            _microphone = new MicrophoneService(_audio, uiContext)
            {
                SelectedDeviceId = _settings.Current.MicrophoneDeviceId,
            };
        }
        catch (COMException ex)
        {
            ShowError(LocalizationManager.Instance.Format("Error_AudioUnavailable", ex.Message));
            Shutdown(1);
            return;
        }

        _viewModel = new MainViewModel(_audio, _microphone, _settings);

        _window = new MainWindow { DataContext = _viewModel };
        _window.RestorePosition(_settings.Current.WindowLeft, _settings.Current.WindowTop);
        _window.Closing += OnMainWindowClosing;
        MainWindow = _window;

        _autostart = new RegistryAutostartService(Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "OACP.exe"));
        _tray = new TrayIconController(_viewModel, ShowMainWindow, ShowSettings, ExitApplication);

        // Autostart starts hidden in the tray (SPEC F3.6).
        if (!Options.StartInTray)
        {
            ShowMainWindow();
        }

        Dispatcher.BeginInvoke(CheckAutostartPath, DispatcherPriority.ApplicationIdle);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _viewModel?.Dispose();
        _microphone?.Dispose();
        _audio?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    /// <summary>Shows, restores and activates the main window.</summary>
    internal void ShowMainWindow()
    {
        if (_window is null || _exiting)
        {
            return;
        }

        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }

        // Bring to the foreground even when another application is active.
        _window.Activate();
        _window.Topmost = true;
        _window.Topmost = false;
        _window.Focus();
    }

    /// <summary>Shows the settings window (one at a time).</summary>
    internal void ShowSettings()
    {
        if (_autostart is null || _settings is null || _audio is null || _microphone is null || _exiting)
        {
            return;
        }

        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow { DataContext = new SettingsViewModel(_autostart, _settings, _audio, _microphone) };
        if (_window is { IsVisible: true })
        {
            _settingsWindow.Owner = _window;
        }
        else
        {
            _settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    /// <summary>Exits the application (tray menu "Exit").</summary>
    internal void ExitApplication()
    {
        if (_exiting)
        {
            return;
        }

        _exiting = true;
        _settingsWindow?.Close();
        _window?.Close();
        Shutdown();
    }

    private void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_exiting)
        {
            return;
        }

        if (_settings?.Current.CloseToTray != false)
        {
            // Closing the window only hides it to the tray (SPEC F3.5).
            e.Cancel = true;
            _window?.Hide();
            return;
        }

        _exiting = true;
        Dispatcher.BeginInvoke(Shutdown);
    }

    /// <summary>Offers to fix autostart when the portable folder was moved (SPEC F4.4).</summary>
    private void CheckAutostartPath()
    {
        if (_autostart is null)
        {
            return;
        }

        try
        {
            if (!_autostart.GetStatus().NeedsRepair)
            {
                return;
            }

            var answer = MessageBox.Show(
                LocalizationManager.Instance["Autostart_RepairPrompt"],
                AppInfo.ProductName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes)
            {
                _autostart.Enable();
            }
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            ShowError(LocalizationManager.Instance.Format("Error_AutostartFailed", ex.Message));
        }
    }

    /// <summary>After an update the new instance waits for the old process to exit (SPEC F9.7).</summary>
    private static void WaitForPreviousProcess(int? processId)
    {
        if (processId is not { } pid)
        {
            return;
        }

        try
        {
            using var process = Process.GetProcessById(pid);
            process.WaitForExit(PreviousInstanceTimeout);
        }
        catch (ArgumentException)
        {
            // Already exited.
        }
        catch (InvalidOperationException)
        {
            // Already exited.
        }
    }

    /// <summary>settings.ini next to the exe (portable), or in %APPDATA% when that folder is read-only.</summary>
    private static IniSettingsStore CreateSettingsStore()
    {
        var executableDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var location = SettingsPathResolver.Resolve(executableDirectory, appData);
        return new IniSettingsStore(location.Path, location.IsPortable);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowError(LocalizationManager.Instance.Format("Error_Unexpected_Message", e.Exception.Message));
        e.Handled = true;
    }

    private static void ShowError(string message) =>
        MessageBox.Show(
            message,
            $"{AppInfo.ProductName} – {LocalizationManager.Instance["Error_Unexpected_Title"]}",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
}
