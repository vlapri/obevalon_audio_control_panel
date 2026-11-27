// SPDX-License-Identifier: GPL-3.0-or-later
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Obevalon.AudioControlPanel.Core.Hotkeys;

namespace Obevalon.AudioControlPanel.ViewModels;

/// <summary>One configurable hotkey in the settings window.</summary>
public sealed partial class HotkeyItemViewModel : ObservableObject
{
    private readonly Func<HotkeyItemViewModel, string, string?> _apply;
    private string _gesture;

    /// <param name="apply">Validates and stores a new gesture; returns an error message or null.</param>
    internal HotkeyItemViewModel(HotkeyAction action, string labelKey, string gesture, string? error, Func<HotkeyItemViewModel, string, string?> apply)
    {
        Action = action;
        Label = LocalizedOption<HotkeyAction>.Localized(action, labelKey);
        _gesture = gesture;
        _error = error;
        _apply = apply;
    }

    public HotkeyAction Action { get; }

    /// <summary>Localized label (follows language changes).</summary>
    public LocalizedOption<HotkeyAction> Label { get; }

    /// <summary>Text form of the hotkey, e.g. "Ctrl+Alt+A"; empty = not assigned.</summary>
    public string Gesture
    {
        get => _gesture;
        set
        {
            value ??= string.Empty;
            if (value == _gesture)
            {
                return;
            }

            _gesture = value;
            OnPropertyChanged();
            Error = _apply(this, value);
        }
    }

    [ObservableProperty]
    private string? _error;

    [RelayCommand]
    private void Clear() => Gesture = string.Empty;
}
