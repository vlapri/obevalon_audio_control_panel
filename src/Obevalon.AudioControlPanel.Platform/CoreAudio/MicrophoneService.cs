// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Obevalon.AudioControlPanel.Core.Audio;
using Obevalon.AudioControlPanel.Platform.CoreAudio.Interop;

namespace Obevalon.AudioControlPanel.Platform.CoreAudio;

/// <summary>
/// Windows Core Audio implementation of <see cref="IMicrophoneService"/>.
/// Must be created on the UI thread; follows the system default microphone unless a device is selected.
/// </summary>
public sealed partial class MicrophoneService : IMicrophoneService
{
    private static readonly Guid EventContext = new("2E0F6B41-7C1D-4B3A-9F07-5B1C2A6E9D11");

    private readonly AudioDeviceService _devices;
    private readonly SynchronizationContext _context;
    private readonly VolumeCallback _callback;

    private IAudioEndpointVolume? _endpointVolume;
    private int _generation;
    private string? _selectedDeviceId;
    private bool _disposed;

    public MicrophoneService(AudioDeviceService devices, SynchronizationContext context)
    {
        ArgumentNullException.ThrowIfNull(devices);
        ArgumentNullException.ThrowIfNull(context);

        _devices = devices;
        _context = context;
        _callback = new VolumeCallback(this);

        _devices.DevicesChanged += OnDevicesChanged;
        _devices.DefaultDeviceChanged += OnDefaultDeviceChanged;
        Attach();
    }

    public event EventHandler? StateChanged;

    public MicrophoneState State { get; private set; } = MicrophoneState.Unavailable;

    public string? SelectedDeviceId
    {
        get => _selectedDeviceId;
        set
        {
            if (_selectedDeviceId == value)
            {
                return;
            }

            _selectedDeviceId = value;
            Attach();
        }
    }

    public void SetVolume(float volume)
    {
        if (_endpointVolume is null)
        {
            return;
        }

        var clamped = Math.Clamp(volume, 0f, 1f);
        _endpointVolume.SetMasterVolumeLevelScalar(clamped, EventContext);
        UpdateState(State with { Volume = clamped });
    }

    public void SetMuted(bool muted)
    {
        if (_endpointVolume is null)
        {
            return;
        }

        _endpointVolume.SetMute(muted ? 1 : 0, EventContext);
        UpdateState(State with { IsMuted = muted });
    }

    public void ToggleMute() => SetMuted(!State.IsMuted);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _devices.DevicesChanged -= OnDevicesChanged;
        _devices.DefaultDeviceChanged -= OnDefaultDeviceChanged;
        Detach();
    }

    private void OnDevicesChanged(object? sender, EventArgs e)
    {
        // The controlled device may have been unplugged, or a selected one plugged in.
        var target = ResolveTargetDeviceId();
        if (target != State.DeviceId)
        {
            Attach();
        }
        else if (target is not null)
        {
            // Possibly renamed.
            var name = _devices.GetDevice(target)?.Name;
            if (name is not null && name != State.DeviceName)
            {
                UpdateState(State with { DeviceName = name });
            }
        }
    }

    private void OnDefaultDeviceChanged(object? sender, DefaultDeviceChangedEventArgs e)
    {
        if (e.Flow == AudioFlow.Input && e.Role == AudioDeviceRole.Default && _selectedDeviceId is null)
        {
            Attach();
        }
    }

    private string? ResolveTargetDeviceId()
    {
        if (_selectedDeviceId is not null)
        {
            return _devices.GetDevice(_selectedDeviceId)?.IsAvailable == true ? _selectedDeviceId : null;
        }

        return _devices.GetDefaultDeviceId(AudioFlow.Input);
    }

    private void Attach()
    {
        Detach();
        if (_disposed)
        {
            return;
        }

        var deviceId = ResolveTargetDeviceId();
        if (deviceId is null)
        {
            UpdateState(MicrophoneState.Unavailable);
            return;
        }

        IMMDevice? device = null;
        try
        {
            device = _devices.OpenDevice(deviceId);
            var endpointVolume = ComHelper.Activate<IAudioEndpointVolume>(device);
            endpointVolume.RegisterControlChangeNotify(_callback);
            _endpointVolume = endpointVolume;

            var name = _devices.GetDevice(deviceId)?.Name ?? deviceId;
            UpdateState(new MicrophoneState(
                deviceId,
                name,
                endpointVolume.GetMasterVolumeLevelScalar(),
                endpointVolume.GetMute() != 0));
        }
        catch (COMException)
        {
            Detach();
            UpdateState(MicrophoneState.Unavailable);
        }
        finally
        {
            ComHelper.Release(device);
        }
    }

    private void Detach()
    {
        // Notifications from the previous endpoint that are still queued are ignored.
        Interlocked.Increment(ref _generation);

        var endpointVolume = _endpointVolume;
        _endpointVolume = null;
        if (endpointVolume is null)
        {
            return;
        }

        try
        {
            endpointVolume.UnregisterControlChangeNotify(_callback);
        }
        catch (COMException)
        {
            // The device is gone; nothing to unregister.
        }

        ComHelper.Release(endpointVolume);
    }

    private void UpdateState(MicrophoneState state)
    {
        if (state == State)
        {
            return;
        }

        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Called on a Core Audio worker thread.</summary>
    private void OnVolumeNotification(float volume, bool muted)
    {
        var generation = Volatile.Read(ref _generation);
        _context.Post(
            _ =>
            {
                if (!_disposed && generation == Volatile.Read(ref _generation) && State.IsAvailable)
                {
                    UpdateState(State with { Volume = volume, IsMuted = muted });
                }
            },
            null);
    }

    [GeneratedComClass]
    private sealed partial class VolumeCallback(MicrophoneService owner) : IAudioEndpointVolumeCallback
    {
        public unsafe void OnNotify(nint notifyData)
        {
            if (notifyData == 0)
            {
                return;
            }

            var data = *(AudioVolumeNotificationData*)notifyData;

            // Our own changes are already applied to State; echoing them back while the user
            // drags the slider would make it jump.
            if (data.EventContext != EventContext)
            {
                owner.OnVolumeNotification(data.MasterVolume, data.Muted != 0);
            }
        }
    }
}
