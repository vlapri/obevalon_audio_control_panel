// SPDX-License-Identifier: GPL-3.0-or-later
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    /// <summary>Show outputs the user has hidden (to un-hide them).</summary>
    public bool ShowHiddenDevices
    {
        get => _settings.Current.ShowHiddenDevices;
        set
        {
            if (_settings.Current.ShowHiddenDevices == value)
            {
                return;
            }

            _settings.Current.ShowHiddenDevices = value;
            OnPropertyChanged();
            SaveAndRefresh();
        }
    }

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _infoMessage;

    /// <summary>Display name of the current default output (for the tray tooltip).</summary>
    [ObservableProperty]
    private string? _currentOutputName;

    private IReadOnlyList<OutputEntry> _lastEntries = [];

    public void Dispose()
    {
        _audio.DevicesChanged -= OnDevicesChanged;
        _audio.DefaultDeviceChanged -= OnDefaultDeviceChanged;
        Microphone.Dispose();
    }

    /// <summary>Switches to the next output in the cycle (SPEC F1.5).</summary>
    [RelayCommand]
    public void NextOutput()
    {
        try
        {
            var next = CycleSelector.Next(MergeOutputs());
            if (next is null)
            {
                InfoMessage = LocalizationManager.Instance["Info_CycleNeedsTwo"];
                return;
            }

            SwitchTo(next);
        }
        catch (COMException ex)
        {
            ReportError("Error_SwitchFailed", ex);
        }
    }

    /// <summary>Remembers the main window position (SPEC §3.8, [Window]).</summary>
    public void SaveWindowPosition(double left, double top)
    {
        if (_settings.Current.WindowLeft == left && _settings.Current.WindowTop == top)
        {
            return;
        }

        _settings.Current.WindowLeft = left;
        _settings.Current.WindowTop = top;
        SaveSettings();
    }

    internal void SwitchTo(OutputDeviceViewModel output) => SwitchTo(output.Id);

    /// <summary>Switches the default output (used by the tray menu).</summary>
    public void SwitchToOutput(string deviceId) => SwitchTo(deviceId);

    internal void Move(OutputDeviceViewModel output, int offset)
    {
        // Move past hidden outputs that are not shown, so one click always moves visibly.
        var visible = Outputs.Select(o => o.Id).ToList();
        var index = visible.IndexOf(output.Id);
        var targetIndex = index + offset;
        if (index < 0 || targetIndex < 0 || targetIndex >= visible.Count)
        {
            return;
        }

        var all = _settings.Current.Outputs;
        var from = all.FindIndex(o => o.Id == output.Id);
        var to = all.FindIndex(o => o.Id == visible[targetIndex]);
        if (from >= 0 && to >= 0 && DeviceCatalog.Move(_settings.Current, output.Id, to - from))
        {
            SaveAndRefresh();
        }
    }

    internal void SetInCycle(OutputDeviceViewModel output, bool inCycle) =>
        UpdateOutput(output, o => o.InCycle = inCycle);

    internal void SetHidden(OutputDeviceViewModel output, bool hidden) =>
        UpdateOutput(output, o => o.Hidden = hidden);

    internal void Rename(OutputDeviceViewModel output, string? name, string? note) =>
        UpdateOutput(output, o =>
        {
            o.Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
            o.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        });

    internal void Remove(OutputDeviceViewModel output)
    {
        if (DeviceCatalog.Remove(_settings.Current, output.Id))
        {
            SaveAndRefresh();
        }
    }

    private void UpdateOutput(OutputDeviceViewModel output, Action<OutputDeviceSettings> update)
    {
        var settings = _settings.Current.FindOutput(output.Id);
        if (settings is null)
        {
            return;
        }

        update(settings);
        SaveAndRefresh();
    }

    private void SwitchTo(string deviceId)
    {
        try
        {
            _audio.SetDefaultDevice(deviceId, _settings.Current.SwitchCommunicationsDevice);
            ErrorMessage = null;
            InfoMessage = null;
        }
        catch (COMException ex)
        {
            ReportError("Error_SwitchFailed", ex);
        }
    }

    private IReadOnlyList<OutputEntry> MergeOutputs()
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

        return entries;
    }

    private void RefreshOutputs()
    {
        try
        {
            _lastEntries = MergeOutputs();
            CurrentOutputName = _lastEntries.FirstOrDefault(e => e.IsDefault)?.DisplayName;
            var visible = _lastEntries
                .Where(e => !e.Hidden || _settings.Current.ShowHiddenDevices)
                .ToList();

            Outputs.Clear();
            for (var i = 0; i < visible.Count; i++)
            {
                Outputs.Add(new OutputDeviceViewModel(visible[i], this, isFirst: i == 0, isLast: i == visible.Count - 1));
            }
        }
        catch (COMException ex)
        {
            ReportError("Error_AudioUnavailable", ex);
        }

        OnPropertyChanged(nameof(HasOutputs));
    }

    private void SaveAndRefresh()
    {
        SaveSettings();
        RefreshOutputs();
    }

    private void SaveSettings()
    {
        if (!_settings.Save() && _settings.LastSaveError is { } error)
        {
            ReportError("Error_SettingsSaveFailed", error);
        }
    }

    private void OnDevicesChanged(object? sender, EventArgs e)
    {
        // Do not rebuild the list while the user is typing a name or note.
        if (!Outputs.Any(o => o.IsEditing))
        {
            RefreshOutputs();
        }
    }

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

        CurrentOutputName = _lastEntries
            .FirstOrDefault(o => string.Equals(o.Id, e.DeviceId, StringComparison.OrdinalIgnoreCase))?.DisplayName;
    }

    private void ReportError(string messageKey, Exception ex) =>
        ErrorMessage = LocalizationManager.Instance.Format(messageKey, ex.Message);
}
