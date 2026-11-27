// SPDX-License-Identifier: GPL-3.0-or-later
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Obevalon.AudioControlPanel.Core;
using Obevalon.AudioControlPanel.Core.Settings;
using Obevalon.AudioControlPanel.Localization;
using Obevalon.AudioControlPanel.Platform.CoreAudio;
using Obevalon.AudioControlPanel.ViewModels;
using Obevalon.AudioControlPanel.Views;

namespace Obevalon.AudioControlPanel;

public partial class App : Application
{
    private SettingsManager? _settings;
    private AudioDeviceService? _audio;
    private MicrophoneService? _microphone;
    private MainViewModel? _viewModel;

    internal StartupOptions Options { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        Options = StartupOptions.Parse(e.Args);

        _settings = new SettingsManager(CreateSettingsStore());
        LocalizationManager.Instance.SetLanguage(_settings.Current.Language);

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

        // The tray icon comes in milestone M4; until then the window is always shown,
        // otherwise "--tray" would leave an invisible process without a way to exit.
        var window = new MainWindow { DataContext = _viewModel };
        window.RestorePosition(_settings.Current.WindowLeft, _settings.Current.WindowTop);
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _viewModel?.Dispose();
        _microphone?.Dispose();
        _audio?.Dispose();
        base.OnExit(e);
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
