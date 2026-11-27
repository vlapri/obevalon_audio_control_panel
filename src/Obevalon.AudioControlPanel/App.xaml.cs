// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Obevalon.AudioControlPanel.Core;
using Obevalon.AudioControlPanel.Core.Localization;
using Obevalon.AudioControlPanel.Localization;
using Obevalon.AudioControlPanel.Platform.CoreAudio;
using Obevalon.AudioControlPanel.ViewModels;
using Obevalon.AudioControlPanel.Views;

namespace Obevalon.AudioControlPanel;

public partial class App : Application
{
    private AudioDeviceService? _audio;
    private MicrophoneService? _microphone;
    private MainViewModel? _viewModel;

    internal StartupOptions Options { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        Options = StartupOptions.Parse(e.Args);

        // Language setting will be read from settings.ini (milestone M2/M6).
        LocalizationManager.Instance.SetLanguage(LanguageResolver.Auto);

        // Audio services post their notifications to the UI thread through this context.
        var uiContext = SynchronizationContext.Current ?? new DispatcherSynchronizationContext(Dispatcher);
        try
        {
            _audio = new AudioDeviceService(uiContext);
            _microphone = new MicrophoneService(_audio, uiContext);
        }
        catch (COMException ex)
        {
            ShowError(LocalizationManager.Instance.Format("Error_AudioUnavailable", ex.Message));
            Shutdown(1);
            return;
        }

        _viewModel = new MainViewModel(_audio, _microphone);

        // The tray icon comes in milestone M4; until then the window is always shown,
        // otherwise "--tray" would leave an invisible process without a way to exit.
        var window = new MainWindow { DataContext = _viewModel };
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
