// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using Obevalon.AudioControlPanel.Core.Localization;

namespace Obevalon.AudioControlPanel.Localization;

/// <summary>
/// Provides localized strings and allows switching the UI language at runtime.
/// XAML binds to the indexer through <see cref="LocExtension"/>.
/// </summary>
public sealed class LocalizationManager : INotifyPropertyChanged
{
    private static readonly ResourceManager Resources =
        new("Obevalon.AudioControlPanel.Localization.Strings", typeof(LocalizationManager).Assembly);

    private LocalizationManager()
    {
    }

    public static LocalizationManager Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The system UI culture captured at startup (before the app changes it).</summary>
    public static CultureInfo SystemUiCulture { get; } = CultureInfo.CurrentUICulture;

    public CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo(LanguageResolver.English);

    /// <summary>Returns the localized string for <paramref name="key"/>, or "[key]" if it is missing.</summary>
    public string this[string key] => Resources.GetString(key, Culture) ?? $"[{key}]";

    /// <summary>Returns a localized format string filled with <paramref name="args"/>.</summary>
    public string Format(string key, params object?[] args) => string.Format(Culture, this[key], args);

    /// <summary>Applies the language setting ("auto", "cs" or "en").</summary>
    public void SetLanguage(string? setting)
    {
        var culture = LanguageResolver.Resolve(setting, SystemUiCulture);
        if (Equals(culture, Culture))
        {
            return;
        }

        Culture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Culture)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
