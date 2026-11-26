// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;

namespace Obevalon.AudioControlPanel.Core;

/// <summary>Command line options of the application.</summary>
/// <param name="StartInTray">Start hidden in the system tray (used by autostart).</param>
/// <param name="EnableLogging">Write a diagnostic log next to the executable.</param>
/// <param name="Updated">The application was just restarted after an update.</param>
/// <param name="WaitForProcessId">Wait for this process (the previous instance) to exit before starting.</param>
public sealed record StartupOptions(
    bool StartInTray = false,
    bool EnableLogging = false,
    bool Updated = false,
    int? WaitForProcessId = null)
{
    public const string TrayArgument = "--tray";
    public const string LogArgument = "--log";
    public const string UpdatedArgument = "--updated";
    public const string WaitPidArgument = "--wait-pid";

    /// <summary>Parses command line arguments. Unknown or malformed arguments are ignored.</summary>
    public static StartupOptions Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var options = new StartupOptions();
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i].Trim();
            if (Is(arg, TrayArgument))
            {
                options = options with { StartInTray = true };
            }
            else if (Is(arg, LogArgument))
            {
                options = options with { EnableLogging = true };
            }
            else if (Is(arg, UpdatedArgument))
            {
                options = options with { Updated = true };
            }
            else if (Is(arg, WaitPidArgument) && i + 1 < args.Count)
            {
                if (int.TryParse(args[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var pid) && pid > 0)
                {
                    options = options with { WaitForProcessId = pid };
                    i++;
                }
            }
        }

        return options;
    }

    private static bool Is(string arg, string expected) =>
        string.Equals(arg, expected, StringComparison.OrdinalIgnoreCase);
}
