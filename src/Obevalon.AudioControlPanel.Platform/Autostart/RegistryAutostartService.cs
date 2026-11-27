// SPDX-License-Identifier: GPL-3.0-or-later
using Microsoft.Win32;
using Obevalon.AudioControlPanel.Core.Autostart;

namespace Obevalon.AudioControlPanel.Platform.Autostart;

/// <summary>
/// Autostart through HKCU\Software\Microsoft\Windows\CurrentVersion\Run (no administrator rights,
/// no UAC prompt at sign-in). Also reads and clears the Task Manager "disabled" flag in StartupApproved.
/// </summary>
public sealed class RegistryAutostartService(string executablePath) : IAutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    /// <summary>StartupApproved value meaning "enabled": 0x02 followed by zeros (12 bytes).</summary>
    private static readonly byte[] EnabledApproval = [0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    public AutostartStatus GetStatus()
    {
        using var run = Registry.CurrentUser.OpenSubKey(RunKey);
        if (run?.GetValue(AutostartCommand.ValueName) is not string command)
        {
            return AutostartStatus.NotRegistered;
        }

        using var approved = Registry.CurrentUser.OpenSubKey(StartupApprovedKey);
        var approval = approved?.GetValue(AutostartCommand.ValueName) as byte[];

        return new AutostartStatus(
            IsRegistered: true,
            IsPathCurrent: AutostartCommand.PointsTo(command, executablePath),
            IsDisabledBySystem: AutostartCommand.IsDisabledByStartupApproved(approval));
    }

    public void Enable()
    {
        using (var run = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            run.SetValue(AutostartCommand.ValueName, AutostartCommand.Build(executablePath), RegistryValueKind.String);
        }

        // The user explicitly turned autostart on in this application, so clear a Task Manager "disabled" flag.
        using var approved = Registry.CurrentUser.OpenSubKey(StartupApprovedKey, writable: true);
        if (approved?.GetValue(AutostartCommand.ValueName) is byte[] approval
            && AutostartCommand.IsDisabledByStartupApproved(approval))
        {
            approved.SetValue(AutostartCommand.ValueName, EnabledApproval, RegistryValueKind.Binary);
        }
    }

    public void Disable()
    {
        using (var run = Registry.CurrentUser.OpenSubKey(RunKey, writable: true))
        {
            run?.DeleteValue(AutostartCommand.ValueName, throwOnMissingValue: false);
        }

        using var approved = Registry.CurrentUser.OpenSubKey(StartupApprovedKey, writable: true);
        approved?.DeleteValue(AutostartCommand.ValueName, throwOnMissingValue: false);
    }
}
