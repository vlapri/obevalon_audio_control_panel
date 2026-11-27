// SPDX-License-Identifier: GPL-3.0-or-later
namespace Obevalon.AudioControlPanel.Core.Updates;

/// <summary>Checks whether the installed .NET Desktop Runtimes can run a new version (SPEC F9.9).</summary>
public static class RuntimeRequirement
{
    /// <summary>
    /// True when <paramref name="minimumRuntime"/> is empty, or an installed runtime has the same major
    /// version and is at least the minimum (major versions do not roll forward for apps by default).
    /// </summary>
    public static bool IsSatisfied(string? minimumRuntime, IEnumerable<Version> installedRuntimes)
    {
        ArgumentNullException.ThrowIfNull(installedRuntimes);

        if (string.IsNullOrWhiteSpace(minimumRuntime))
        {
            return true;
        }

        var minimum = SemanticVersion.Parse(minimumRuntime);
        if (minimum is null)
        {
            return false;
        }

        return installedRuntimes.Any(v =>
            v.Major == minimum.Major
            && (v.Minor > minimum.Minor || (v.Minor == minimum.Minor && Math.Max(v.Build, 0) >= minimum.Patch)));
    }
}
