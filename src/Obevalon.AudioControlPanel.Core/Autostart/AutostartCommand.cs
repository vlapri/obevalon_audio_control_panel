// SPDX-License-Identifier: GPL-3.0-or-later
namespace Obevalon.AudioControlPanel.Core.Autostart;

/// <summary>Builds and inspects the command line stored in the Run registry key.</summary>
public static class AutostartCommand
{
    /// <summary>Registry value name under HKCU\...\Run and ...\StartupApproved\Run.</summary>
    public const string ValueName = "ObevalonAudioControlPanel";

    /// <summary>"C:\path\OACP.exe" --tray</summary>
    public static string Build(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        return $"\"{executablePath}\" {StartupOptions.TrayArgument}";
    }

    /// <summary>Extracts the executable path from a command line (quoted or not).</summary>
    public static string? GetExecutablePath(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        var text = command.Trim();
        if (text[0] == '"')
        {
            var end = text.IndexOf('"', 1);
            return end > 1 ? text[1..end] : null;
        }

        // Unquoted: up to ".exe" (paths may contain spaces), or up to the first space.
        var exe = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exe > 0)
        {
            return text[..(exe + 4)];
        }

        var space = text.IndexOf(' ', StringComparison.Ordinal);
        return space > 0 ? text[..space] : text;
    }

    /// <summary>True when the command starts <paramref name="executablePath"/> (case-insensitive, normalized).</summary>
    public static bool PointsTo(string? command, string executablePath)
    {
        var path = GetExecutablePath(command);
        if (path is null)
        {
            return false;
        }

        try
        {
            return string.Equals(Path.GetFullPath(path), Path.GetFullPath(executablePath), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>
    /// Interprets the StartupApproved REG_BINARY value written by Task Manager:
    /// an odd first byte (0x03, 0x07…) means disabled, an even one (0x02, 0x06…) enabled.
    /// </summary>
    public static bool IsDisabledByStartupApproved(byte[]? data) =>
        data is { Length: > 0 } && (data[0] & 0x01) == 0x01;
}
