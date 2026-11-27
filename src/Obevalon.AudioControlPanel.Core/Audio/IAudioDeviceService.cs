// SPDX-License-Identifier: GPL-3.0-or-later
namespace Obevalon.AudioControlPanel.Core.Audio;

/// <summary>
/// Lists audio endpoints and changes the default device.
/// Events are raised on the synchronization context the service was created on (the UI thread).
/// </summary>
public interface IAudioDeviceService : IDisposable
{
    /// <summary>Devices were added, removed, enabled, disabled, plugged, unplugged or renamed.</summary>
    event EventHandler? DevicesChanged;

    /// <summary>A default device changed (by this application or by anybody else).</summary>
    event EventHandler<DefaultDeviceChangedEventArgs>? DefaultDeviceChanged;

    /// <summary>Returns active, disabled and unplugged devices (not the "not present" ghosts).</summary>
    IReadOnlyList<AudioDevice> GetDevices(AudioFlow flow);

    /// <summary>Returns a device by ID in any state, or null when the system does not know it.</summary>
    AudioDevice? GetDevice(string deviceId);

    /// <summary>Returns the ID of the current default device, or null when there is none.</summary>
    string? GetDefaultDeviceId(AudioFlow flow, AudioDeviceRole role = AudioDeviceRole.Default);

    /// <summary>
    /// Makes the device the default one (Windows roles Console and Multimedia) and optionally
    /// also the default communications device.
    /// </summary>
    void SetDefaultDevice(string deviceId, bool includeCommunications);
}
