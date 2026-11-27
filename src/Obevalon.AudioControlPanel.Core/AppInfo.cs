// SPDX-License-Identifier: GPL-3.0-or-later
using System.Reflection;

namespace Obevalon.AudioControlPanel.Core;

/// <summary>Static information about the application.</summary>
public static class AppInfo
{
    /// <summary>Full product name (not localized, it is a brand name).</summary>
    public const string ProductName = "Obevalon Audio Control Panel";

    /// <summary>Short name, also the executable name.</summary>
    public const string ShortName = "OACP";

    public const string Author = "Obevalon";

    public const string RepositoryUrl = "https://github.com/vlapri/obevalon_audio_control_panel";

    public const string ReleasesUrl = RepositoryUrl + "/releases";

    public const string LicenseName = "GNU GPL v3";

    public const string LicenseUrl = RepositoryUrl + "/blob/main/LICENSE";

    /// <summary>Application version, e.g. "1.2.0" or "0.0.0-dev" for local builds.</summary>
    public static string Version { get; } = ReadVersion(Assembly.GetEntryAssembly() ?? typeof(AppInfo).Assembly);

    internal static string ReadVersion(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return NormalizeVersion(informational);
    }

    /// <summary>Removes build metadata ("+commit") from a version string.</summary>
    internal static string NormalizeVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "0.0.0-dev";
        }

        var plus = version.IndexOf('+', StringComparison.Ordinal);
        return (plus >= 0 ? version[..plus] : version).Trim();
    }
}
