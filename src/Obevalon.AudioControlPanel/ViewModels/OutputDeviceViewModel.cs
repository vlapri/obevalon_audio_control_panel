// SPDX-License-Identifier: GPL-3.0-or-later
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Obevalon.AudioControlPanel.Core.Audio;
using Obevalon.AudioControlPanel.Core.Logic;

namespace Obevalon.AudioControlPanel.ViewModels;

/// <summary>One audio output in the list.</summary>
public sealed partial class OutputDeviceViewModel : ObservableObject
{
    private readonly Action<OutputDeviceViewModel> _switchTo;

    public OutputDeviceViewModel(OutputEntry entry, Action<OutputDeviceViewModel> switchTo)
    {
        Id = entry.Id;
        DisplayName = entry.DisplayName;
        SystemName = entry.SystemName;
        Note = entry.Note;
        State = entry.State;
        InCycle = entry.InCycle;
        Hidden = entry.Hidden;
        _isDefault = entry.IsDefault;
        _switchTo = switchTo;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string SystemName { get; }

    public string? Note { get; }

    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    /// <summary>System state, or null when the device is missing.</summary>
    public AudioDeviceState? State { get; }

    public bool IsAvailable => State == AudioDeviceState.Active;

    public bool IsMissing => State is null;

    public bool InCycle { get; }

    public bool Hidden { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SwitchCommand))]
    private bool _isDefault;

    private bool CanSwitch() => IsAvailable && !IsDefault;

    [RelayCommand(CanExecute = nameof(CanSwitch))]
    private void Switch() => _switchTo(this);
}
