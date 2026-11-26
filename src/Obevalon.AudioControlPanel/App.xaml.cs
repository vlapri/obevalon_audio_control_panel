// SPDX-License-Identifier: GPL-3.0-or-later
using System.Windows;
using System.Windows.Threading;
using Obevalon.AudioControlPanel.Core;
using Obevalon.AudioControlPanel.Core.Localization;
using Obevalon.AudioControlPanel.Localization;
using Obevalon.AudioControlPanel.Views;

namespace Obevalon.AudioControlPanel;

public partial class App : Application
{
    internal StartupOptions Options { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        Options = StartupOptions.Parse(e.Args);

        // Language setting will be read from settings.ini (milestone M2/M6).
        LocalizationManager.Instance.SetLanguage(LanguageResolver.Auto);

        // The tray icon comes in milestone M4; until then the window is always shown,
        // otherwise "--tray" would leave an invisible process without a way to exit.
        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var loc = LocalizationManager.Instance;
        MessageBox.Show(
            loc.Format("Error_Unexpected_Message", e.Exception.Message),
            $"{AppInfo.ProductName} – {loc["Error_Unexpected_Title"]}",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
