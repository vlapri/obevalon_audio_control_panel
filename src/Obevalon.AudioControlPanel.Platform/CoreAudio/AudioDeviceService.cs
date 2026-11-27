// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Obevalon.AudioControlPanel.Core.Audio;
using Obevalon.AudioControlPanel.Platform.CoreAudio.Interop;

namespace Obevalon.AudioControlPanel.Platform.CoreAudio;

/// <summary>
/// Windows Core Audio implementation of <see cref="IAudioDeviceService"/>.
/// Must be created on the UI thread; notifications are posted to its synchronization context,
/// so no Core Audio call is ever made from inside a notification callback.
/// </summary>
public sealed partial class AudioDeviceService : IAudioDeviceService
{
    private const uint ListedStates = DeviceStates.Active | DeviceStates.Disabled | DeviceStates.Unplugged;

    private readonly SynchronizationContext _context;
    private readonly IMMDeviceEnumerator _enumerator;
    private readonly NotificationClient _notificationClient;
    private int _devicesChangedPending;
    private bool _disposed;

    public AudioDeviceService(SynchronizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _enumerator = ComHelper.Create<IMMDeviceEnumerator>(ComIds.MMDeviceEnumerator);
        _notificationClient = new NotificationClient(this);
        _enumerator.RegisterEndpointNotificationCallback(_notificationClient);
    }

    public event EventHandler? DevicesChanged;

    public event EventHandler<DefaultDeviceChangedEventArgs>? DefaultDeviceChanged;

    public IReadOnlyList<AudioDevice> GetDevices(AudioFlow flow)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var result = new List<AudioDevice>();
        var collection = _enumerator.EnumAudioEndpoints(ToDataFlow(flow), ListedStates);
        try
        {
            var count = collection.GetCount();
            for (uint i = 0; i < count; i++)
            {
                IMMDevice? device = null;
                try
                {
                    device = collection.Item(i);
                    result.Add(ReadDevice(device, flow));
                }
                catch (COMException)
                {
                    // The device disappeared while enumerating; skip it.
                }
                finally
                {
                    ComHelper.Release(device);
                }
            }
        }
        finally
        {
            ComHelper.Release(collection);
        }

        return result;
    }

    public AudioDevice? GetDevice(string deviceId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(deviceId);

        IMMDevice? device = null;
        try
        {
            device = _enumerator.GetDevice(deviceId);
            return ReadDevice(device, FlowFromId(deviceId));
        }
        catch (COMException)
        {
            return null;
        }
        finally
        {
            ComHelper.Release(device);
        }
    }

    public string? GetDefaultDeviceId(AudioFlow flow, AudioDeviceRole role = AudioDeviceRole.Default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        IMMDevice? device = null;
        try
        {
            device = _enumerator.GetDefaultAudioEndpoint(ToDataFlow(flow), ToRole(role));
            return device.GetId();
        }
        catch (COMException ex) when (ex.HResult == ComConstants.ENotFound)
        {
            return null;
        }
        finally
        {
            ComHelper.Release(device);
        }
    }

    public void SetDefaultDevice(string deviceId, bool includeCommunications)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(deviceId);

        var policy = ComHelper.Create<IPolicyConfig>(ComIds.PolicyConfigClient);
        try
        {
            policy.SetDefaultEndpoint(deviceId, ERole.Console);
            policy.SetDefaultEndpoint(deviceId, ERole.Multimedia);
            if (includeCommunications)
            {
                policy.SetDefaultEndpoint(deviceId, ERole.Communications);
            }
        }
        finally
        {
            ComHelper.Release(policy);
        }
    }

    /// <summary>Returns the raw endpoint for <see cref="MicrophoneService"/>; the caller releases it.</summary>
    internal IMMDevice OpenDevice(string deviceId) => _enumerator.GetDevice(deviceId);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);
        }
        catch (COMException)
        {
            // Shutting down; nothing to do.
        }

        ComHelper.Release(_enumerator);
    }

    private static AudioDevice ReadDevice(IMMDevice device, AudioFlow flow)
    {
        var id = device.GetId();
        var state = ToState(device.GetState());

        string? name = null;
        IPropertyStore? store = null;
        try
        {
            store = device.OpenPropertyStore(ComConstants.StgmRead);
            name = ComHelper.GetStringProperty(store, PropertyKeys.DeviceFriendlyName)
                ?? ComHelper.GetStringProperty(store, PropertyKeys.DeviceDescription);
        }
        catch (COMException)
        {
            // Properties may be unreadable for some devices; fall back to the ID.
        }
        finally
        {
            ComHelper.Release(store);
        }

        return new AudioDevice(id, string.IsNullOrWhiteSpace(name) ? id : name, flow, state);
    }

    /// <summary>
    /// Endpoint IDs start with "{0.0.0.00000000}" for render and "{0.0.1.00000000}" for capture devices.
    /// </summary>
    private static AudioFlow FlowFromId(string deviceId) =>
        deviceId.StartsWith("{0.0.1.", StringComparison.Ordinal) ? AudioFlow.Input : AudioFlow.Output;

    private static EDataFlow ToDataFlow(AudioFlow flow) => flow == AudioFlow.Input ? EDataFlow.Capture : EDataFlow.Render;

    private static ERole ToRole(AudioDeviceRole role) => role == AudioDeviceRole.Communications ? ERole.Communications : ERole.Console;

    private static AudioDeviceState ToState(uint state) => state switch
    {
        DeviceStates.Active => AudioDeviceState.Active,
        DeviceStates.Disabled => AudioDeviceState.Disabled,
        DeviceStates.Unplugged => AudioDeviceState.Unplugged,
        _ => AudioDeviceState.NotPresent,
    };

    // ---- Notifications (called on Core Audio worker threads) ----

    private void QueueDevicesChanged()
    {
        // Coalesce bursts of notifications (plugging a device fires several) into one event.
        if (Interlocked.Exchange(ref _devicesChangedPending, 1) == 1)
        {
            return;
        }

        _context.Post(
            _ =>
            {
                Volatile.Write(ref _devicesChangedPending, 0);
                if (!_disposed)
                {
                    DevicesChanged?.Invoke(this, EventArgs.Empty);
                }
            },
            null);
    }

    private void QueueDefaultDeviceChanged(EDataFlow flow, ERole role, string? deviceId)
    {
        // Console and Multimedia are switched together; report Console as "Default".
        if (role == ERole.Multimedia || flow == EDataFlow.All)
        {
            return;
        }

        var args = new DefaultDeviceChangedEventArgs(
            flow == EDataFlow.Capture ? AudioFlow.Input : AudioFlow.Output,
            role == ERole.Communications ? AudioDeviceRole.Communications : AudioDeviceRole.Default,
            deviceId);

        _context.Post(
            _ =>
            {
                if (!_disposed)
                {
                    DefaultDeviceChanged?.Invoke(this, args);
                }
            },
            null);
    }

    [GeneratedComClass]
    private sealed partial class NotificationClient(AudioDeviceService owner) : IMMNotificationClient
    {
        public void OnDeviceStateChanged(string deviceId, uint newState) => owner.QueueDevicesChanged();

        public void OnDeviceAdded(string deviceId) => owner.QueueDevicesChanged();

        public void OnDeviceRemoved(string deviceId) => owner.QueueDevicesChanged();

        public void OnDefaultDeviceChanged(EDataFlow flow, ERole role, string? defaultDeviceId) =>
            owner.QueueDefaultDeviceChanged(flow, role, defaultDeviceId);

        public void OnPropertyValueChanged(string deviceId, PropertyKey key)
        {
            // Many properties change often; only a rename matters to us.
            if (key.Equals(PropertyKeys.DeviceFriendlyName)
                || key.Equals(PropertyKeys.DeviceDescription)
                || key.Equals(PropertyKeys.DeviceInterfaceFriendlyName))
            {
                owner.QueueDevicesChanged();
            }
        }
    }
}
