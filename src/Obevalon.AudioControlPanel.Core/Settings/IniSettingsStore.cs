// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text;

namespace Obevalon.AudioControlPanel.Core.Settings;

/// <summary>Stores settings in an INI file (UTF-8, atomic replace on save).</summary>
public sealed class IniSettingsStore(string settingsPath, bool isPortable) : ISettingsStore
{
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    public string SettingsPath { get; } = settingsPath;

    public bool IsPortable { get; } = isPortable;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            var text = File.ReadAllText(SettingsPath, Encoding.UTF8);
            return SettingsSerializer.Read(IniDocument.Parse(text));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write to a temporary file first, then replace, so a crash never leaves a half-written file.
        var temporaryPath = SettingsPath + ".tmp";
        File.WriteAllText(temporaryPath, SettingsSerializer.Write(settings).ToString(), Utf8WithBom);
        File.Move(temporaryPath, SettingsPath, overwrite: true);
    }
}
