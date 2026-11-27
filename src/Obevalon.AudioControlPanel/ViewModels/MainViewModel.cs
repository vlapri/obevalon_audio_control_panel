// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Obevalon.AudioControlPanel.Core.Audio;
using Obevalon.AudioControlPanel.Localization;

namespace Obevalon.AudioControlPanel.ViewModels;

/// <summary>Main window: list of audio outputs and the microphone.</summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    // Will come from settings.ini (milestone M2, SPEC D7).
    private const bool SwitchCommunicationsDevice = true;

    private readonly IAudioDeviceService _audio;

    public MainViewModel(IAudioDeviceService audio, IMicrophoneService microphone)
    {
        _audio = audio;
        Microphone = new MicrophoneViewModel(microphone, ex => ReportError("Error_MicrophoneFailed", ex));

        _audio.DevicesChanged += OnDevicesChanged;
        _audio.DefaultDeviceChanged += OnDefaultDeviceChanged;
        RefreshOutputs();
    }

    public ObservableCollection<OutputDeviceViewModel> Outputs { get; } = [];

    public MicrophoneViewModel Microphone { get; }

    public bool HasOutputs => Outputs.Count > 0;

    [ObservableProperty]
    private string? _errorMessage;

    public void Dispose()
    {
        _audio.DevicesChanged -= OnDevicesChanged;
        _audio.DefaultDeviceChanged -= OnDefaultDeviceChanged;
        Microphone.Dispose();
    }

    private void RefreshOutputs()
    {
        try
        {
            var defaultId = _audio.GetDefaultDeviceId(AudioFlow.Output);

            // Ordering and hiding come from settings in milestones M2/M3; for now available devices first.
            var devices = _audio.GetDevices(AudioFlow.Output)
                .OrderBy(d => d.IsAvailable ? 0 : 1)
                .ThenBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase);

            Outputs.Clear();
            foreach (var device in devices)
            {
                Outputs.Add(new OutputDeviceViewModel(device, device.Id == defaultId, SwitchTo));
            }
        }
        catch (COMException ex)
        {
            ReportError("Error_AudioUnavailable", ex);
        }

        OnPropertyChanged(nameof(HasOutputs));
    }

    private void SwitchTo(OutputDeviceViewModel output)
    {
        try
        {
            _audio.SetDefaultDevice(output.Id, SwitchCommunicationsDevice);
            ErrorMessage = null;
        }
        catch (COMException ex)
        {
            ReportError("Error_SwitchFailed", ex);
        }
    }

    private void OnDevicesChanged(object? sender, EventArgs e) => RefreshOutputs();

    private void OnDefaultDeviceChanged(object? sender, DefaultDeviceChangedEventArgs e)
    {
        if (e.Flow != AudioFlow.Output || e.Role != AudioDeviceRole.Default)
        {
            return;
        }

        foreach (var output in Outputs)
        {
            output.IsDefault = output.Id == e.DeviceId;
        }
    }

    private void ReportError(string messageKey, Exception ex) =>
        ErrorMessage = LocalizationManager.Instance.Format(messageKey, ex.Message);
}
