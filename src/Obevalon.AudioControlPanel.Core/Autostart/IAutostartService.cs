// SPDX-License-Identifier: GPL-3.0-or-later
namespace Obevalon.AudioControlPanel.Core.Autostart;

/// <summary>State of the "start at sign-in" registration (SPEC §3.4).</summary>
/// <param name="IsRegistered">The Run entry exists.</param>
/// <param name="IsPathCurrent">The Run entry starts this executable (false after moving the portable folder).</param>
/// <param name="IsDisabledBySystem">The user disabled the entry in Task Manager / Settings → Apps → Startup.</param>
public sealed record AutostartStatus(bool IsRegistered, bool IsPathCurrent, bool IsDisabledBySystem)
{
    public static AutostartStatus NotRegistered { get; } = new(false, false, false);

    /// <summary>The application will start at sign-in.</summary>
    public bool IsEffective => IsRegistered && IsPathCurrent && !IsDisabledBySystem;

    /// <summary>Registered, but for an executable in another location.</summary>
    public bool NeedsRepair => IsRegistered && !IsPathCurrent;
}

/// <summary>Registers the application to start at sign-in, without administrator rights.</summary>
public interface IAutostartService
{
    AutostartStatus GetStatus();

    /// <summary>Registers (or re-registers) the current executable with "--tray".</summary>
    void Enable();

    void Disable();
}
