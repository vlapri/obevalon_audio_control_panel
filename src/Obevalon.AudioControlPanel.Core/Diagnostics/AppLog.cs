// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using System.Text;

namespace Obevalon.AudioControlPanel.Core.Diagnostics;

/// <summary>
/// Optional diagnostic log (SPEC N6): off by default, enabled by "--log" or "Logging=true" in settings.ini.
/// Writes one file per day (oacp-YYYY-MM-DD.log) and keeps the last 14 days. Never throws.
/// </summary>
public static class AppLog
{
    private const int KeepDays = 14;
    private static readonly Lock Gate = new();
    private static string? _directory;

    public static bool IsEnabled => _directory is not null;

    /// <summary>Starts logging into <paramref name="directory"/> and removes old log files.</summary>
    public static void Enable(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            _directory = directory;
            foreach (var file in Directory.EnumerateFiles(directory, "oacp-*.log"))
            {
                if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-KeepDays))
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _directory = null;
        }
    }

    /// <summary>Stops logging.</summary>
    public static void Disable() => _directory = null;

    public static void Info(string message) => Write("INFO ", message, null);

    public static void Warning(string message, Exception? exception = null) => Write("WARN ", message, exception);

    public static void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    /// <summary>File name for a given day (for tests and the settings window).</summary>
    public static string FileNameFor(DateTime localDate) =>
        "oacp-" + localDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log";

    private static void Write(string level, string message, Exception? exception)
    {
        var directory = _directory;
        if (directory is null)
        {
            return;
        }

        var now = DateTime.Now;
        var line = new StringBuilder()
            .Append(now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
            .Append(' ').Append(level).Append(' ').Append(message);
        if (exception is not null)
        {
            line.Append(" | ").Append(exception.GetType().Name).Append(": ").Append(exception.Message)
                .Append(" (0x").Append(exception.HResult.ToString("X8", CultureInfo.InvariantCulture)).Append(')');
            if (exception.StackTrace is { } stack)
            {
                line.AppendLine().Append(stack);
            }
        }

        line.AppendLine();
        lock (Gate)
        {
            try
            {
                File.AppendAllText(Path.Combine(directory, FileNameFor(now)), line.ToString(), Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Logging must never break the application.
            }
        }
    }
}
