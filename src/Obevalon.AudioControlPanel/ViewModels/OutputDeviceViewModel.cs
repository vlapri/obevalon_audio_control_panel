// SPDX-License-Identifier: GPL-3.0-or-later
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Obevalon.AudioControlPanel.Core.Audio;
using Obevalon.AudioControlPanel.Core.Logic;

namespace Obevalon.AudioControlPanel.ViewModels;

/// <summary>One audio output in the list, with its user settings.</summary>
public sealed partial class OutputDeviceViewModel : ObservableObject
{
    private readonly MainViewModel _owner;

    public OutputDeviceViewModel(OutputEntry entry, MainViewModel owner, bool isFirst, bool isLast)
    {
        _owner = owner;
        Id = entry.Id;
        DisplayName = entry.DisplayName;
        SystemName = entry.SystemName;
        CustomName = entry.CustomName;
        Note = entry.Note;
        State = entry.State;
        InCycle = entry.InCycle;
        Hidden = entry.Hidden;
        IsFirst = isFirst;
        IsLast = isLast;
        _isDefault = entry.IsDefault;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string SystemName { get; }

    public string? CustomName { get; }

    public string? Note { get; }

    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    /// <summary>System state, or null when the device is missing.</summary>
    public AudioDeviceState? State { get; }

    public bool IsAvailable => State == AudioDeviceState.Active;

    public bool IsMissing => State is null;

    public bool InCycle { get; }

    public bool Hidden { get; }

    public bool IsFirst { get; }

    public bool IsLast { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SwitchCommand))]
    private bool _isDefault;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editNote = string.Empty;

    private bool CanSwitch() => IsAvailable && !IsDefault;

    [RelayCommand(CanExecute = nameof(CanSwitch))]
    private void Switch() => _owner.SwitchTo(this);

    private bool CanMoveUp() => !IsFirst;

    [RelayCommand(CanExecute = nameof(CanMoveUp))]
    private void MoveUp() => _owner.Move(this, -1);

    private bool CanMoveDown() => !IsLast;

    [RelayCommand(CanExecute = nameof(CanMoveDown))]
    private void MoveDown() => _owner.Move(this, +1);

    [RelayCommand]
    private void ToggleCycle() => _owner.SetInCycle(this, !InCycle);

    [RelayCommand]
    private void ToggleHidden() => _owner.SetHidden(this, !Hidden);

    [RelayCommand]
    private void Remove() => _owner.Remove(this);

    [RelayCommand]
    private void BeginEdit()
    {
        EditName = CustomName ?? string.Empty;
        EditNote = Note ?? string.Empty;
        IsEditing = true;
    }

    [RelayCommand]
    private void SaveEdit()
    {
        IsEditing = false;
        _owner.Rename(this, EditName, EditNote);
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;
}
