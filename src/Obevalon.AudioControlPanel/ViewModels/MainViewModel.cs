// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Obevalon.AudioControlPanel.Core.Audio;
using Obevalon.AudioControlPanel.Core.Logic;
using Obevalon.AudioControlPanel.Core.Settings;
using Obevalon.AudioControlPanel.Localization;

namespace Obevalon.AudioControlPanel.ViewModels;

/// <summary>Main window: list of audio outputs and the microphone.</summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IAudioDeviceService _audio;
    private readonly SettingsManager _settings;

    public MainViewModel(IAudioDeviceService audio, IMicrophoneService microphone, SettingsManager settings)
    {
        _audio = audio;
        _settings = settings;
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
            var entries = DeviceCatalog.Merge(
                _settings.Current,
                _audio.GetDevices(AudioFlow.Output),
                _audio.GetDefaultDeviceId(AudioFlow.Output),
                out var settingsChanged);
            if (settingsChanged)
            {
                SaveSettings();
            }

            Outputs.Clear();
            foreach (var entry in entries)
            {
                if (!entry.Hidden || _settings.Current.ShowHiddenDevices)
                {
                    Outputs.Add(new OutputDeviceViewModel(entry, SwitchTo));
                }
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
            _audio.SetDefaultDevice(output.Id, _settings.Current.SwitchCommunicationsDevice);
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
            output.IsDefault = string.Equals(output.Id, e.DeviceId, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void SaveSettings()
    {
        if (!_settings.Save() && _settings.LastSaveError is { } error)
        {
            ReportError("Error_SettingsSaveFailed", error);
        }
    }

    private void ReportError(string messageKey, Exception ex) =>
        ErrorMessage = LocalizationManager.Instance.Format(messageKey, ex.Message);
}
