// SPDX-License-Identifier: GPL-3.0-or-later
namespace Obevalon.AudioControlPanel.Core.Settings;

/// <summary>Loads and saves <see cref="AppSettings"/>.</summary>
public interface ISettingsStore
{
    /// <summary>Full path of the settings file.</summary>
    string SettingsPath { get; }

    /// <summary>The settings file is next to the executable (portable mode).</summary>
    bool IsPortable { get; }

    /// <summary>Loads the settings; returns defaults when the file is missing or unreadable.</summary>
    AppSettings Load();

    /// <summary>Saves the settings atomically. Throws <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> on failure.</summary>
    void Save(AppSettings settings);
}
