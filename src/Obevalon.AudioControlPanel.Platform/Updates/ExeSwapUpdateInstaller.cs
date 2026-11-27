// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System.Diagnostics;
using Obevalon.AudioControlPanel.Core;
using Obevalon.AudioControlPanel.Core.Settings;

namespace Obevalon.AudioControlPanel.Platform.Updates;

/// <summary>
/// Installs an update without administrator rights and without a separate updater (SPEC F9.7):
/// the running exe is renamed to ".old" (Windows allows renaming, not overwriting, a running exe),
/// the new exe takes its place and is started; the new instance deletes the ".old" file.
/// </summary>
public sealed class ExeSwapUpdateInstaller(string executablePath)
{
    public string ExecutablePath { get; } = executablePath;

    public string OldExecutablePath => ExecutablePath + ".old";

    /// <summary>False when the application folder is read-only (e.g. Program Files, SPEC F9.8).</summary>
    public bool CanInstall =>
        Path.GetDirectoryName(ExecutablePath) is { } directory && SettingsPathResolver.IsDirectoryWritable(directory);

    /// <summary>
    /// Replaces the executable with <paramref name="downloadedExecutable"/> and starts it.
    /// The caller must exit right after this returns. On failure the original executable is restored.
    /// </summary>
    public void InstallAndRestart(string downloadedExecutable, bool startInTray)
    {
        ArgumentException.ThrowIfNullOrEmpty(downloadedExecutable);

        TryDelete(OldExecutablePath);
        File.Move(ExecutablePath, OldExecutablePath);
        try
        {
            File.Move(downloadedExecutable, ExecutablePath);

            var arguments = $"{StartupOptions.UpdatedArgument} {StartupOptions.WaitPidArgument} {Environment.ProcessId}";
            if (startInTray)
            {
                arguments += " " + StartupOptions.TrayArgument;
            }

            using var process = Process.Start(new ProcessStartInfo(ExecutablePath, arguments)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(ExecutablePath) ?? string.Empty,
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception or InvalidOperationException)
        {
            Rollback();
            throw;
        }
    }

    /// <summary>Deletes the previous version left behind by an update.</summary>
    public void CleanupOldExecutable() => TryDelete(OldExecutablePath);

    private void Rollback()
    {
        try
        {
            if (File.Exists(ExecutablePath))
            {
                File.Delete(ExecutablePath);
            }

            File.Move(OldExecutablePath, ExecutablePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Nothing more we can do; the .old file is still there for a manual fix.
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Still locked (the previous process is exiting); it is retried on the next start.
        }
    }
}
