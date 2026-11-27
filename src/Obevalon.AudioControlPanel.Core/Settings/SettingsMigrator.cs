// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;

namespace Obevalon.AudioControlPanel.Core.Settings;

/// <summary>
/// Upgrades settings files written by older versions. Each format change bumps
/// <see cref="AppSettings.CurrentSettingsVersion"/> and adds a step here.
/// Files from newer versions are read as well as possible and rewritten in the current format.
/// </summary>
public static class SettingsMigrator
{
    /// <summary>Returns the format version of a document; files without a version are version 1.</summary>
    public static int GetVersion(IniDocument document)
    {
        var value = document.GetValue(SettingsSerializer.GeneralSection, "SettingsVersion");
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version) && version > 0
            ? version
            : 1;
    }

    /// <summary>Upgrades the document in place to the current format.</summary>
    public static void Migrate(IniDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var version = GetVersion(document);

        // Example for the future:
        // if (version < 2) { /* rename keys, convert values */ version = 2; }

        if (version < AppSettings.CurrentSettingsVersion)
        {
            version = AppSettings.CurrentSettingsVersion;
        }

        if (version <= AppSettings.CurrentSettingsVersion)
        {
            document.GetOrAddSection(SettingsSerializer.GeneralSection)
                .SetValue("SettingsVersion", version.ToString(CultureInfo.InvariantCulture));
        }
    }
}
