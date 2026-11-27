// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Obevalon.AudioControlPanel.Localization;

namespace Obevalon.AudioControlPanel.ViewModels;

/// <summary>An option for a combo box whose label follows the UI language.</summary>
public sealed class LocalizedOption<T> : ObservableObject
{
    private readonly string? _labelKey;
    private readonly string? _fixedLabel;

    private LocalizedOption(T value, string? labelKey, string? fixedLabel)
    {
        Value = value;
        _labelKey = labelKey;
        _fixedLabel = fixedLabel;
        if (labelKey is not null)
        {
            // Weak subscription: options do not keep themselves alive through the singleton.
            PropertyChangedEventManager.AddHandler(LocalizationManager.Instance, OnLanguageChanged, string.Empty);
        }
    }

    public T Value { get; }

    private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Label));

    public string Label => _fixedLabel ?? LocalizationManager.Instance[_labelKey!];

    /// <summary>Label from the localization resources.</summary>
    public static LocalizedOption<T> Localized(T value, string labelKey) => new(value, labelKey, null);

    /// <summary>Label that is not translated (e.g. a language's own name or a device name).</summary>
    public static LocalizedOption<T> Fixed(T value, string label) => new(value, null, label);
}
