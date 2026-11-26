// SPDX-License-Identifier: GPL-3.0-or-later
namespace Obevalon.AudioControlPanel.Core.Audio;

/// <summary>Direction of an audio endpoint.</summary>
public enum AudioFlow
{
    /// <summary>Playback device (speakers, HDMI, headphones).</summary>
    Output,

    /// <summary>Recording device (microphone).</summary>
    Input,
}

/// <summary>Role of a default device.</summary>
public enum AudioDeviceRole
{
    /// <summary>The default device (Windows roles Console and Multimedia).</summary>
    Default,

    /// <summary>The default communications device (Teams, Discord, ...).</summary>
    Communications,
}

/// <summary>State of an audio endpoint.</summary>
public enum AudioDeviceState
{
    Active,
    Disabled,
    NotPresent,
    Unplugged,
}

/// <summary>An audio endpoint as reported by the system.</summary>
/// <param name="Id">Stable endpoint ID.</param>
/// <param name="Name">System friendly name, e.g. "Speakers (Realtek(R) Audio)".</param>
public sealed record AudioDevice(string Id, string Name, AudioFlow Flow, AudioDeviceState State)
{
    /// <summary>The device can be used right now.</summary>
    public bool IsAvailable => State == AudioDeviceState.Active;
}

/// <summary>Event data for a change of a default device.</summary>
public sealed class DefaultDeviceChangedEventArgs(AudioFlow flow, AudioDeviceRole role, string? deviceId) : EventArgs
{
    public AudioFlow Flow { get; } = flow;

    public AudioDeviceRole Role { get; } = role;

    /// <summary>The new default device, or null when there is none.</summary>
    public string? DeviceId { get; } = deviceId;
}
