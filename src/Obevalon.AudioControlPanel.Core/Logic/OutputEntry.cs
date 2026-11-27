// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core.Audio;

namespace Obevalon.AudioControlPanel.Core.Logic;

/// <summary>An audio output as shown to the user: system state merged with the user's settings.</summary>
/// <param name="State">System state, or null when the device is no longer known to the system (missing).</param>
public sealed record OutputEntry(
    string Id,
    string SystemName,
    string? CustomName,
    string? Note,
    bool InCycle,
    bool Hidden,
    AudioDeviceState? State,
    bool IsDefault)
{
    /// <summary>Custom name when set, otherwise the system name.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(CustomName) ? SystemName : CustomName;

    public bool IsAvailable => State == AudioDeviceState.Active;

    /// <summary>The device is in the settings but the system does not list it any more (SPEC F1.8).</summary>
    public bool IsMissing => State is null;
}
