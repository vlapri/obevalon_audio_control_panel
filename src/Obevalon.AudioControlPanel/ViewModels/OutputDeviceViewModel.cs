// SPDX-License-Identifier: GPL-3.0-or-later
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Obevalon.AudioControlPanel.Core.Audio;

namespace Obevalon.AudioControlPanel.ViewModels;

/// <summary>One audio output in the list.</summary>
public sealed partial class OutputDeviceViewModel : ObservableObject
{
    private readonly Action<OutputDeviceViewModel> _switchTo;

    public OutputDeviceViewModel(AudioDevice device, bool isDefault, Action<OutputDeviceViewModel> switchTo)
    {
        Id = device.Id;
        Name = device.Name;
        State = device.State;
        _isDefault = isDefault;
        _switchTo = switchTo;
    }

    public string Id { get; }

    public string Name { get; }

    public AudioDeviceState State { get; }

    public bool IsAvailable => State == AudioDeviceState.Active;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SwitchCommand))]
    private bool _isDefault;

    private bool CanSwitch() => IsAvailable && !IsDefault;

    [RelayCommand(CanExecute = nameof(CanSwitch))]
    private void Switch() => _switchTo(this);
}
