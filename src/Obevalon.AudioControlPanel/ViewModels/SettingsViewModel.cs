// SPDX-License-Identifier: GPL-3.0-or-later
using System.Diagnostics;
using System.IO;
using System.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Obevalon.AudioControlPanel.Core.Autostart;
using Obevalon.AudioControlPanel.Core.Settings;
using Obevalon.AudioControlPanel.Localization;

namespace Obevalon.AudioControlPanel.ViewModels;

/// <summary>Settings window (SPEC §3.6).</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IAutostartService _autostart;
    private readonly SettingsManager _settings;
    private AutostartStatus _autostartStatus = AutostartStatus.NotRegistered;

    public SettingsViewModel(IAutostartService autostart, SettingsManager settings)
    {
        _autostart = autostart;
        _settings = settings;
        RefreshAutostart();
    }

    // ---- Startup ----

    public bool AutostartEnabled
    {
        get => _autostartStatus.IsRegistered;
        set
        {
            if (value == _autostartStatus.IsRegistered && !(value && _autostartStatus.IsDisabledBySystem))
            {
                return;
            }

            ChangeAutostart(value);
        }
    }

    public bool AutostartDisabledBySystem => _autostartStatus.IsRegistered && _autostartStatus.IsDisabledBySystem;

    public bool AutostartNeedsRepair => _autostartStatus.NeedsRepair;

    [RelayCommand]
    private void RepairAutostart() => ChangeAutostart(true);

    // ---- Settings file ----

    public string SettingsPath => _settings.SettingsPath;

    public bool IsPortable => _settings.IsPortable;

    [RelayCommand]
    private void OpenSettingsFolder()
    {
        var arguments = File.Exists(SettingsPath)
            ? $"/select,\"{SettingsPath}\""
            : $"\"{Path.GetDirectoryName(SettingsPath)}\"";
        Process.Start(new ProcessStartInfo("explorer.exe", arguments) { UseShellExecute = true });
    }

    [ObservableProperty]
    private string? _errorMessage;

    public void RefreshAutostart()
    {
        try
        {
            _autostartStatus = _autostart.GetStatus();
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            _autostartStatus = AutostartStatus.NotRegistered;
            ErrorMessage = LocalizationManager.Instance.Format("Error_AutostartFailed", ex.Message);
        }

        OnPropertyChanged(nameof(AutostartEnabled));
        OnPropertyChanged(nameof(AutostartDisabledBySystem));
        OnPropertyChanged(nameof(AutostartNeedsRepair));
    }

    private void ChangeAutostart(bool enable)
    {
        try
        {
            if (enable)
            {
                _autostart.Enable();
            }
            else
            {
                _autostart.Disable();
            }

            ErrorMessage = null;
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            ErrorMessage = LocalizationManager.Instance.Format("Error_AutostartFailed", ex.Message);
        }

        RefreshAutostart();
    }
}
