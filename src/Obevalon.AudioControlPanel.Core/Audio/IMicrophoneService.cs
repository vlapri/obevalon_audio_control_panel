// SPDX-License-Identifier: GPL-3.0-or-later
namespace Obevalon.AudioControlPanel.Core.Audio;

/// <summary>State of the controlled microphone.</summary>
/// <param name="DeviceId">Controlled device, or null when no microphone is available.</param>
/// <param name="Volume">Volume as a scalar 0.0–1.0.</param>
public sealed record MicrophoneState(string? DeviceId, string? DeviceName, float Volume, bool IsMuted)
{
    public static MicrophoneState Unavailable { get; } = new(null, null, 0f, false);

    public bool IsAvailable => DeviceId is not null;
}

/// <summary>
/// Controls the volume and mute of one microphone: the system default one, or a selected one.
/// Events are raised on the synchronization context the service was created on (the UI thread).
/// </summary>
public interface IMicrophoneService : IDisposable
{
    /// <summary>The volume, mute state or the controlled device changed (from anywhere).</summary>
    event EventHandler? StateChanged;

    MicrophoneState State { get; }

    /// <summary>The device to control, or null for the system default microphone.</summary>
    string? SelectedDeviceId { get; set; }

    /// <summary>Sets the volume as a scalar 0.0–1.0 (values outside are clamped).</summary>
    void SetVolume(float volume);

    void SetMuted(bool muted);

    void ToggleMute();
}
