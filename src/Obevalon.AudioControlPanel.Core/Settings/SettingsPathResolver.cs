// SPDX-License-Identifier: GPL-3.0-or-later
namespace Obevalon.AudioControlPanel.Core.Settings;

/// <summary>Where the settings file lives.</summary>
public sealed record SettingsLocation(string Path, bool IsPortable);

/// <summary>
/// Chooses the settings file: next to the executable (portable) when that folder is writable,
/// otherwise in %APPDATA%\Obevalon\AudioControlPanel (SPEC F8.1, F8.2).
/// </summary>
public static class SettingsPathResolver
{
    public const string FileName = "settings.ini";

    public static SettingsLocation Resolve(
        string executableDirectory,
        string appDataDirectory,
        Func<string, bool>? isDirectoryWritable = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableDirectory);
        ArgumentException.ThrowIfNullOrEmpty(appDataDirectory);

        var isWritable = isDirectoryWritable ?? IsDirectoryWritable;
        return isWritable(executableDirectory)
            ? new SettingsLocation(Path.Combine(executableDirectory, FileName), IsPortable: true)
            : new SettingsLocation(Path.Combine(appDataDirectory, "Obevalon", "AudioControlPanel", FileName), IsPortable: false);
    }

    /// <summary>Tests writability by creating and deleting a temporary file.</summary>
    public static bool IsDirectoryWritable(string directory)
    {
        try
        {
            var probe = Path.Combine(directory, $".oacp-write-test-{Guid.NewGuid():N}.tmp");
            using (File.Create(probe, 1, FileOptions.DeleteOnClose))
            {
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
