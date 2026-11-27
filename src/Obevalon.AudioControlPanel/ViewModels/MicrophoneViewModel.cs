// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Obevalon.AudioControlPanel.Core.Audio;

namespace Obevalon.AudioControlPanel.ViewModels;

/// <summary>Microphone volume and mute.</summary>
public sealed partial class MicrophoneViewModel : ObservableObject, IDisposable
{
    private readonly IMicrophoneService _microphone;
    private readonly Action<Exception> _reportError;
    private bool _syncing;

    public MicrophoneViewModel(IMicrophoneService microphone, Action<Exception> reportError)
    {
        _microphone = microphone;
        _reportError = reportError;
        _microphone.StateChanged += OnStateChanged;
        SyncFromService();
    }

    /// <summary>Raised after the user toggled mute here, in the tray or by a hotkey (argument: muted).</summary>
    public event EventHandler<bool>? MuteToggled;

    /// <summary>Volume in percent (0–100).</summary>
    [ObservableProperty]
    private double _volume;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private bool _isAvailable;

    [ObservableProperty]
    private string? _deviceName;

    [RelayCommand]
    private void ToggleMute()
    {
        try
        {
            _microphone.ToggleMute();
            MuteToggled?.Invoke(this, _microphone.State.IsMuted);
        }
        catch (COMException ex)
        {
            _reportError(ex);
        }
    }

    partial void OnVolumeChanged(double value)
    {
        if (_syncing)
        {
            return;
        }

        try
        {
            _microphone.SetVolume((float)(value / 100.0));
        }
        catch (COMException ex)
        {
            _reportError(ex);
        }
    }

    public void Dispose() => _microphone.StateChanged -= OnStateChanged;

    private void OnStateChanged(object? sender, EventArgs e) => SyncFromService();

    private void SyncFromService()
    {
        var state = _microphone.State;
        _syncing = true;
        try
        {
            IsAvailable = state.IsAvailable;
            DeviceName = state.DeviceName;
            IsMuted = state.IsMuted;
            Volume = Math.Round(state.Volume * 100.0);
        }
        finally
        {
            _syncing = false;
        }
    }
}
