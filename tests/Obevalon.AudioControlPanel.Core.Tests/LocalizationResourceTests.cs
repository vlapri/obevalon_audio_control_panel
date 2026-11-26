// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Obevalon.AudioControlPanel.Core.Tests;

/// <summary>Every UI string must exist in both English and Czech, with the same placeholders.</summary>
public partial class LocalizationResourceTests
{
    private static readonly string LocalizationDir = Path.Combine(
        FindRepositoryRoot(), "src", "Obevalon.AudioControlPanel", "Localization");

    [Fact]
    public void EnglishAndCzech_HaveTheSameKeys()
    {
        var en = Load("Strings.resx");
        var cs = Load("Strings.cs.resx");

        Assert.Empty(en.Keys.Except(cs.Keys));
        Assert.Empty(cs.Keys.Except(en.Keys));
    }

    [Fact]
    public void AllValues_AreNotEmpty()
    {
        foreach (var file in new[] { "Strings.resx", "Strings.cs.resx" })
        {
            foreach (var (key, value) in Load(file))
            {
                Assert.False(string.IsNullOrWhiteSpace(value), $"{file}: '{key}' is empty");
            }
        }
    }

    [Fact]
    public void FormatPlaceholders_MatchBetweenLanguages()
    {
        var en = Load("Strings.resx");
        var cs = Load("Strings.cs.resx");

        foreach (var (key, enValue) in en)
        {
            if (cs.TryGetValue(key, out var csValue))
            {
                Assert.Equal(Placeholders(enValue), Placeholders(csValue));
            }
        }
    }

    private static Dictionary<string, string> Load(string fileName)
    {
        var document = XDocument.Load(Path.Combine(LocalizationDir, fileName));
        return document.Root!
            .Elements("data")
            .ToDictionary(
                d => (string)d.Attribute("name")!,
                d => (string?)d.Element("value") ?? string.Empty,
                StringComparer.Ordinal);
    }

    private static string[] Placeholders(string value) =>
        PlaceholderRegex().Matches(value).Select(m => m.Value).Distinct().Order(StringComparer.Ordinal).ToArray();

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ObevalonAudioControlPanel.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    [GeneratedRegex(@"\{\d+(:[^}]*)?\}")]
    private static partial Regex PlaceholderRegex();
}
