// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;

namespace Obevalon.AudioControlPanel.Platform.Updates;

/// <summary>Finds the installed .NET Desktop Runtime versions (SPEC F9.9).</summary>
public static class DotNetRuntimeProbe
{
    /// <summary>"win-x64" or "win-arm64": the key of this build in update.json.</summary>
    public static string RuntimeIdentifier =>
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64";

    /// <summary>Versions of Microsoft.WindowsDesktop.App, including the one running this process.</summary>
    public static IReadOnlyList<Version> GetInstalledDesktopRuntimes()
    {
        var versions = new HashSet<Version> { Environment.Version };
        var architecture = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";
        var keyPath = $@"SOFTWARE\dotnet\Setup\InstalledVersions\{architecture}\sharedfx\Microsoft.WindowsDesktop.App";

        // The .NET installer writes this key to the 32-bit registry view; read both to be safe.
        foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var key = baseKey.OpenSubKey(keyPath);
                foreach (var name in key?.GetValueNames() ?? [])
                {
                    var plain = name.Split('-', '+')[0];
                    if (Version.TryParse(plain, out var version))
                    {
                        versions.Add(version);
                    }
                }
            }
            catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
            {
                // Not readable; the running runtime is still known.
            }
        }

        return [.. versions];
    }
}
