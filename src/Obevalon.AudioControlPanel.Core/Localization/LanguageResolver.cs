// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;

namespace Obevalon.AudioControlPanel.Core.Localization;

/// <summary>Resolves the UI culture from the language setting and the system language.</summary>
public static class LanguageResolver
{
    public const string Auto = "auto";
    public const string Czech = "cs";
    public const string English = "en";

    /// <summary>Languages the UI is translated to.</summary>
    public static IReadOnlyList<string> SupportedLanguages { get; } = [English, Czech];

    /// <summary>
    /// Returns the culture to use for the UI.
    /// <paramref name="setting"/> is "auto", "cs" or "en"; anything else behaves like "auto".
    /// With "auto", Czech is used when the system UI language is Czech, otherwise English.
    /// </summary>
    public static CultureInfo Resolve(string? setting, CultureInfo systemUiCulture)
    {
        ArgumentNullException.ThrowIfNull(systemUiCulture);

        var normalized = setting?.Trim().ToLowerInvariant();
        var language = normalized switch
        {
            Czech => Czech,
            English => English,
            _ => string.Equals(systemUiCulture.TwoLetterISOLanguageName, Czech, StringComparison.OrdinalIgnoreCase)
                ? Czech
                : English,
        };

        return CultureInfo.GetCultureInfo(language);
    }
}
