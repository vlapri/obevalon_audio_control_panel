// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using Obevalon.AudioControlPanel.Core;
using Obevalon.AudioControlPanel.Localization;
using Obevalon.AudioControlPanel.Platform.Shell;
using Obevalon.AudioControlPanel.ViewModels;
using DrawingIcon = System.Drawing.Icon;

namespace Obevalon.AudioControlPanel.Tray;

/// <summary>
/// System tray icon (SPEC §3.3): left click shows the window, right click opens quick actions.
/// The icon shows a badge while the microphone is muted; the tooltip shows the current state.
/// </summary>
internal sealed class TrayIconController : IDisposable
{
    // NOTIFYICONDATA.szTip holds 128 characters including the terminating zero.
    private const int MaxToolTipLength = 127;

    private readonly TaskbarIcon _taskbarIcon;
    private readonly MainViewModel _viewModel;
    private readonly UpdateViewModel _updates;
    private readonly Action _showUpdate;
    private readonly Action _showWindow;
    private readonly Action _showSettings;
    private readonly Action _showAbout;
    private readonly Action _exit;
    private readonly DrawingIcon _normalIcon;
    private readonly DrawingIcon _mutedIcon;
    private readonly DrawingIcon _updateIcon;

    public TrayIconController(
        MainViewModel viewModel,
        UpdateViewModel updates,
        Action showWindow,
        Action showSettings,
        Action showUpdate,
        Action showAbout,
        Action exit)
    {
        _viewModel = viewModel;
        _updates = updates;
        _showUpdate = showUpdate;
        _showAbout = showAbout;
        _showWindow = showWindow;
        _showSettings = showSettings;
        _exit = exit;

        var size = SystemMetrics.SmallIconSize;
        _normalIcon = LoadIcon("tray.ico", size);
        _mutedIcon = LoadIcon("tray-muted.ico", size);
        _updateIcon = LoadIcon("tray-update.ico", size);

        _taskbarIcon = new TaskbarIcon
        {
            Icon = _normalIcon,
            NoLeftClickDelay = true,
            LeftClickCommand = new RelayCommand(showWindow),
            ContextMenu = new ContextMenu(),
        };
        _taskbarIcon.PreviewTrayContextMenuOpen += (_, _) => RebuildMenu();

        _viewModel.PropertyChanged += OnStateChanged;
        _viewModel.Microphone.PropertyChanged += OnStateChanged;
        _updates.PropertyChanged += OnStateChanged;
        LocalizationManager.Instance.PropertyChanged += OnStateChanged;

        UpdateIconAndToolTip();
        _taskbarIcon.ForceCreate(enablesEfficiencyMode: false);
    }

    public void Dispose()
    {
        _viewModel.PropertyChanged -= OnStateChanged;
        _viewModel.Microphone.PropertyChanged -= OnStateChanged;
        _updates.PropertyChanged -= OnStateChanged;
        LocalizationManager.Instance.PropertyChanged -= OnStateChanged;
        _taskbarIcon.Dispose();
        _normalIcon.Dispose();
        _mutedIcon.Dispose();
        _updateIcon.Dispose();
    }

    private static DrawingIcon LoadIcon(string fileName, int size)
    {
        var resource = Application.GetResourceStream(new Uri($"pack://application:,,,/Assets/{fileName}"))
            ?? throw new InvalidOperationException($"Icon resource {fileName} not found.");
        using var stream = resource.Stream;
        return new DrawingIcon(stream, size, size);
    }

    /// <summary>Underscores would be treated as access keys in menu headers.</summary>
    private static string EscapeAccessKeys(string text) => text.Replace("_", "__", StringComparison.Ordinal);

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e) => UpdateIconAndToolTip();

    private void UpdateIconAndToolTip()
    {
        var loc = LocalizationManager.Instance;
        var microphone = _viewModel.Microphone;

        // The muted state matters more than an available update.
        _taskbarIcon.Icon = microphone.IsAvailable && microphone.IsMuted
            ? _mutedIcon
            : _updates.IsUpdateAvailable ? _updateIcon : _normalIcon;

        var output = loc.Format("Tray_ToolTipOutput", _viewModel.CurrentOutputName ?? "–");
        var mic = !microphone.IsAvailable
            ? loc["Tray_ToolTipMicNone"]
            : microphone.IsMuted
                ? loc["Tray_ToolTipMicMuted"]
                : loc.Format("Tray_ToolTipMicOn", microphone.Volume);

        var text = $"{AppInfo.ProductName}\n{output}\n{mic}";
        _taskbarIcon.ToolTipText = text.Length > MaxToolTipLength ? text[..(MaxToolTipLength - 1)] + "…" : text;
    }

    private void RebuildMenu()
    {
        var loc = LocalizationManager.Instance;
        var menu = _taskbarIcon.ContextMenu!;
        menu.Items.Clear();

        if (_updates.IsUpdateAvailable)
        {
            var update = new MenuItem
            {
                Header = loc.Format("Tray_UpdateAvailable", _updates.AvailableVersion),
                FontWeight = FontWeights.SemiBold,
            };
            update.Click += (_, _) => _showUpdate();
            menu.Items.Add(update);
            menu.Items.Add(new Separator());
        }

        var outputs = _viewModel.Outputs.Where(o => o.IsAvailable && !o.Hidden).ToList();
        if (outputs.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = loc["Tray_NoOutputs"], IsEnabled = false });
        }

        foreach (var output in outputs)
        {
            var item = new MenuItem { Header = EscapeAccessKeys(output.DisplayName), IsChecked = output.IsDefault };
            var id = output.Id;
            item.Click += (_, _) => _viewModel.SwitchToOutput(id);
            menu.Items.Add(item);
        }

        menu.Items.Add(new MenuItem { Header = loc["Outputs_Next"], Command = _viewModel.NextOutputCommand });

        menu.Items.Add(new Separator());
        var microphone = _viewModel.Microphone;
        menu.Items.Add(new MenuItem
        {
            Header = loc[microphone.IsMuted ? "Microphone_Unmute" : "Microphone_Mute"],
            Command = microphone.ToggleMuteCommand,
            IsEnabled = microphone.IsAvailable,
        });

        menu.Items.Add(new Separator());
        var open = new MenuItem { Header = loc["Tray_Open"], FontWeight = FontWeights.SemiBold };
        open.Click += (_, _) => _showWindow();
        menu.Items.Add(open);

        var settings = new MenuItem { Header = loc["Tray_Settings"] };
        settings.Click += (_, _) => _showSettings();
        menu.Items.Add(settings);

        var about = new MenuItem { Header = loc["Tray_About"] };
        about.Click += (_, _) => _showAbout();
        menu.Items.Add(about);

        var exit = new MenuItem { Header = loc["Tray_Exit"] };
        exit.Click += (_, _) => _exit();
        menu.Items.Add(exit);
    }
}
