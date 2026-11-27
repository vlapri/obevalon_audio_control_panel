// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core.Localization;

namespace Obevalon.AudioControlPanel.Core.Settings;

/// <summary>Color theme of the UI.</summary>
public enum ThemeMode
{
    /// <summary>Follow the Windows app theme.</summary>
    Auto,
    Light,
    Dark,
}

/// <summary>All persisted settings (see SPEC §3.8). Defaults are the values of a fresh installation.</summary>
public sealed class AppSettings
{
    /// <summary>Version of the settings format written by this build.</summary>
    public const int CurrentSettingsVersion = 1;

    public int SettingsVersion { get; set; } = CurrentSettingsVersion;

    /// <summary>"auto", "cs" or "en".</summary>
    public string Language { get; set; } = LanguageResolver.Auto;

    public ThemeMode Theme { get; set; } = ThemeMode.Auto;

    /// <summary>Closing the window hides it to the tray instead of exiting.</summary>
    public bool CloseToTray { get; set; } = true;

    /// <summary>Switching the output also changes the default communications device (SPEC D7).</summary>
    public bool SwitchCommunicationsDevice { get; set; } = true;

    public bool ShowSwitchOsd { get; set; } = true;

    public bool ShowHiddenDevices { get; set; }

    /// <summary>Controlled microphone, or null for the system default one.</summary>
    public string? MicrophoneDeviceId { get; set; }

    public HotkeySettings Hotkeys { get; } = new();

    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    public UpdateSettings Updates { get; } = new();

    /// <summary>Known audio outputs in the user's order (index 0 = first).</summary>
    public List<OutputDeviceSettings> Outputs { get; } = [];

    public OutputDeviceSettings? FindOutput(string deviceId) =>
        Outputs.Find(o => string.Equals(o.Id, deviceId, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Global hotkeys as text, e.g. "Ctrl+Alt+A"; null or empty = not assigned.</summary>
public sealed class HotkeySettings
{
    public string? NextOutput { get; set; }

    public string? ToggleMicMute { get; set; }

    public string? ShowWindow { get; set; }
}

public sealed class UpdateSettings
{
    public bool AutoCheck { get; set; } = true;

    public DateTimeOffset? LastCheckUtc { get; set; }

    /// <summary>Version the user chose to skip.</summary>
    public string? SkippedVersion { get; set; }

    /// <summary>Override of the update manifest URL for testing (not shown in the UI).</summary>
    public string? ManifestUrl { get; set; }
}

/// <summary>User settings of one audio output.</summary>
public sealed class OutputDeviceSettings(string id)
{
    public string Id { get; } = id;

    /// <summary>Last known system name (shown when the device is missing).</summary>
    public string? SystemName { get; set; }

    /// <summary>Custom name shown instead of the system name.</summary>
    public string? Name { get; set; }

    public string? Note { get; set; }

    /// <summary>The device takes part in "next output" cycling.</summary>
    public bool InCycle { get; set; }

    public bool Hidden { get; set; }
}
