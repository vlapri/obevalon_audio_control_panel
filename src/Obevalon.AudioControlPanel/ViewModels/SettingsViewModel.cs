// SPDX-License-Identifier: GPL-3.0-or-later
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Obevalon.AudioControlPanel.Core.Audio;
using Obevalon.AudioControlPanel.Core.Autostart;
using Obevalon.AudioControlPanel.Core.Diagnostics;
using Obevalon.AudioControlPanel.Core.Hotkeys;
using Obevalon.AudioControlPanel.Core.Localization;
using Obevalon.AudioControlPanel.Core.Settings;
using Obevalon.AudioControlPanel.Hotkeys;
using Obevalon.AudioControlPanel.Localization;
using Obevalon.AudioControlPanel.Themes;

namespace Obevalon.AudioControlPanel.ViewModels;

/// <summary>Settings window (SPEC §3.6).</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IAutostartService _autostart;
    private readonly SettingsManager _settings;
    private readonly IMicrophoneService _microphone;
    private readonly HotkeyManager _hotkeys;
    private AutostartStatus _autostartStatus = AutostartStatus.NotRegistered;

    public SettingsViewModel(
        IAutostartService autostart,
        SettingsManager settings,
        IAudioDeviceService audio,
        IMicrophoneService microphone,
        HotkeyManager hotkeys,
        UpdateViewModel updates)
    {
        Updates = updates;
        _autostart = autostart;
        _settings = settings;
        _microphone = microphone;
        _hotkeys = hotkeys;

        var keys = _settings.Current.Hotkeys;
        Hotkeys =
        [
            CreateHotkey(HotkeyAction.NextOutput, "Outputs_Next", keys.NextOutput),
            CreateHotkey(HotkeyAction.ToggleMicMute, "Hotkey_ToggleMic", keys.ToggleMicMute),
            CreateHotkey(HotkeyAction.ShowWindow, "Hotkey_ShowWindow", keys.ShowWindow),
        ];

        LanguageOptions =
        [
            LocalizedOption<string>.Localized(LanguageResolver.Auto, "Settings_LanguageAuto"),
            LocalizedOption<string>.Fixed(LanguageResolver.Czech, "Čeština"),
            LocalizedOption<string>.Fixed(LanguageResolver.English, "English"),
        ];
        ThemeOptions =
        [
            LocalizedOption<ThemeMode>.Localized(ThemeMode.Auto, "Settings_ThemeAuto"),
            LocalizedOption<ThemeMode>.Localized(ThemeMode.Light, "Settings_ThemeLight"),
            LocalizedOption<ThemeMode>.Localized(ThemeMode.Dark, "Settings_ThemeDark"),
        ];

        // The empty string stands for "system default microphone" (combo boxes do not select null well).
        MicrophoneOptions = [LocalizedOption<string>.Localized(string.Empty, "Settings_MicrophoneDefault")];
        try
        {
            foreach (var device in audio.GetDevices(AudioFlow.Input).Where(d => d.IsAvailable))
            {
                MicrophoneOptions.Add(LocalizedOption<string>.Fixed(device.Id, device.Name));
            }
        }
        catch (COMException ex)
        {
            ErrorMessage = LocalizationManager.Instance.Format("Error_AudioUnavailable", ex.Message);
        }

        // Keep a selected microphone that is unplugged right now selectable.
        if (_settings.Current.MicrophoneDeviceId is { } selected && !MicrophoneOptions.Any(o => o.Value == selected))
        {
            MicrophoneOptions.Add(LocalizedOption<string>.Fixed(selected, audio.GetDevice(selected)?.Name ?? selected));
        }

        RefreshAutostart();
    }

    // ---- General ----

    public IReadOnlyList<LocalizedOption<string>> LanguageOptions { get; }

    public string Language
    {
        get => _settings.Current.Language;
        set => Update(_settings.Current.Language, value, v =>
        {
            _settings.Current.Language = v;
            LocalizationManager.Instance.SetLanguage(v);
        });
    }

    public IReadOnlyList<LocalizedOption<ThemeMode>> ThemeOptions { get; }

    public ThemeMode Theme
    {
        get => _settings.Current.Theme;
        set => Update(_settings.Current.Theme, value, v =>
        {
            _settings.Current.Theme = v;
            ThemeManager.Apply(v);
        });
    }

    /// <summary>Closing the window exits the application (inverse of CloseToTray).</summary>
    public bool ExitOnClose
    {
        get => !_settings.Current.CloseToTray;
        set => Update(!_settings.Current.CloseToTray, value, v => _settings.Current.CloseToTray = !v);
    }

    public bool SwitchCommunicationsDevice
    {
        get => _settings.Current.SwitchCommunicationsDevice;
        set => Update(_settings.Current.SwitchCommunicationsDevice, value, v => _settings.Current.SwitchCommunicationsDevice = v);
    }

    public bool ShowSwitchOsd
    {
        get => _settings.Current.ShowSwitchOsd;
        set => Update(_settings.Current.ShowSwitchOsd, value, v => _settings.Current.ShowSwitchOsd = v);
    }

    // ---- Microphone ----

    public List<LocalizedOption<string>> MicrophoneOptions { get; }

    public string MicrophoneDeviceId
    {
        get => _settings.Current.MicrophoneDeviceId ?? string.Empty;
        set => Update(MicrophoneDeviceId, value ?? string.Empty, v =>
        {
            _settings.Current.MicrophoneDeviceId = v.Length == 0 ? null : v;
            _microphone.SelectedDeviceId = _settings.Current.MicrophoneDeviceId;
        });
    }

    // ---- Hotkeys ----

    public IReadOnlyList<HotkeyItemViewModel> Hotkeys { get; }

    private HotkeyItemViewModel CreateHotkey(HotkeyAction action, string labelKey, string? text)
    {
        var gesture = HotkeyGesture.Parse(text)?.ToString() ?? string.Empty;
        var error = _hotkeys.Failed.Contains(action) ? LocalizationManager.Instance["Hotkey_InUse"] : null;
        return new HotkeyItemViewModel(action, labelKey, gesture, error, ApplyHotkey);
    }

    /// <summary>Stores a recorded hotkey; returns an error message or null. Hotkeys are re-registered when the window closes.</summary>
    private string? ApplyHotkey(HotkeyItemViewModel item, string text)
    {
        var gesture = HotkeyGesture.Parse(text);
        if (gesture is not null && Hotkeys.Any(h => h != item && HotkeyGesture.Parse(h.Gesture) == gesture))
        {
            return LocalizationManager.Instance["Hotkey_Duplicate"];
        }

        var value = gesture?.ToString();
        var keys = _settings.Current.Hotkeys;
        switch (item.Action)
        {
            case HotkeyAction.NextOutput:
                keys.NextOutput = value;
                break;
            case HotkeyAction.ToggleMicMute:
                keys.ToggleMicMute = value;
                break;
            case HotkeyAction.ShowWindow:
                keys.ShowWindow = value;
                break;
        }

        SaveSettings();
        return gesture is not null && !_hotkeys.IsAvailable(gesture) ? LocalizationManager.Instance["Hotkey_InUse"] : null;
    }

    // ---- Updates ----

    public UpdateViewModel Updates { get; }

    public bool AutoCheckUpdates
    {
        get => _settings.Current.Updates.AutoCheck;
        set => Update(_settings.Current.Updates.AutoCheck, value, v => _settings.Current.Updates.AutoCheck = v);
    }

    // ---- Diagnostics ----

    public bool Logging
    {
        get => _settings.Current.Logging;
        set => Update(_settings.Current.Logging, value, v =>
        {
            _settings.Current.Logging = v;
            if (v)
            {
                AppLog.Enable(LogDirectory);
                AppLog.Info("Logging enabled in settings");
            }
            else
            {
                AppLog.Disable();
            }
        });
    }

    public string LogDirectory => Path.Combine(Path.GetDirectoryName(_settings.SettingsPath) ?? ".", "logs");

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

    /// <summary>Applies a changed value, saves the settings and notifies the view.</summary>
    private void Update<T>(T current, T value, Action<T> apply, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
        {
            return;
        }

        apply(value);
        OnPropertyChanged(propertyName);
        SaveSettings();
    }

    private void SaveSettings()
    {
        if (!_settings.Save() && _settings.LastSaveError is { } error)
        {
            ErrorMessage = LocalizationManager.Instance.Format("Error_SettingsSaveFailed", error.Message);
        }
    }
}
