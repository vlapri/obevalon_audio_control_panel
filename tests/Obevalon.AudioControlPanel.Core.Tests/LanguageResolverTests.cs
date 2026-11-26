// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;
using Obevalon.AudioControlPanel.Core.Localization;

namespace Obevalon.AudioControlPanel.Core.Tests;

public class LanguageResolverTests
{
    [Theory]
    [InlineData("auto", "cs-CZ", "cs")]
    [InlineData("auto", "en-US", "en")]
    [InlineData("auto", "de-DE", "en")]
    [InlineData("cs", "en-US", "cs")]
    [InlineData("en", "cs-CZ", "en")]
    [InlineData(" CS ", "en-US", "cs")]
    [InlineData(null, "cs-CZ", "cs")]
    [InlineData("garbage", "de-DE", "en")]
    public void Resolve_ReturnsExpectedLanguage(string? setting, string system, string expected)
    {
        var culture = LanguageResolver.Resolve(setting, CultureInfo.GetCultureInfo(system));

        Assert.Equal(expected, culture.Name);
    }
}
