// SPDX-License-Identifier: GPL-3.0-or-later
using Obevalon.AudioControlPanel.Core.Settings;

namespace Obevalon.AudioControlPanel.Core.Tests;

public class IniDocumentTests
{
    [Fact]
    public void Parse_ReadsSectionsAndValues()
    {
        var document = IniDocument.Parse("""
            ; comment
            # another comment
            [General]
            Language = cs
            Theme=dark

            [Output:{0.0.0.00000000}.{abc}]
            Name=Projektor
            """);

        Assert.Equal("cs", document.GetValue("General", "Language"));
        Assert.Equal("dark", document.GetValue("General", "Theme"));
        Assert.Equal("Projektor", document.GetValue("Output:{0.0.0.00000000}.{abc}", "Name"));
    }

    [Fact]
    public void Parse_IsCaseInsensitive()
    {
        var document = IniDocument.Parse("[general]\nlanguage=en");

        Assert.Equal("en", document.GetValue("GENERAL", "Language"));
    }

    [Fact]
    public void Parse_KeepsSemicolonsAndHashesInValues()
    {
        var document = IniDocument.Parse("[A]\nNote=Big speakers; left #2\nUrl=a=b");

        Assert.Equal("Big speakers; left #2", document.GetValue("A", "Note"));
        Assert.Equal("a=b", document.GetValue("A", "Url"));
    }

    [Fact]
    public void Parse_IgnoresGarbage()
    {
        var document = IniDocument.Parse("orphan=1\n[Broken\nno separator\n=novalue\n[Ok]\nKey=Value\n");

        Assert.Single(document.Sections);
        Assert.Equal("Value", document.GetValue("Ok", "Key"));
    }

    [Fact]
    public void Parse_LastDuplicateWins_AndSectionsMerge()
    {
        var document = IniDocument.Parse("[A]\nKey=1\n[B]\nX=y\n[a]\nkey=2\n");

        Assert.Equal(2, document.Sections.Count);
        Assert.Equal("2", document.GetValue("A", "Key"));
    }

    [Fact]
    public void Parse_HandlesBomAndWindowsLineEndings()
    {
        var document = IniDocument.Parse("﻿[A]\r\nKey=Value\r\n");

        Assert.Equal("Value", document.GetValue("A", "Key"));
    }

    [Fact]
    public void SetValue_ReplacesLineBreaks()
    {
        var document = new IniDocument();
        document.GetOrAddSection("A").SetValue("Note", "line1\nline2\r\nline3");

        Assert.Equal("line1 line2 line3", document.GetValue("A", "Note"));
    }

    [Fact]
    public void ToString_RoundTrips()
    {
        var document = new IniDocument();
        document.HeaderComments.Add("header");
        var section = document.GetOrAddSection("General");
        section.Comment = "section comment";
        section.SetValue("Language", "cs", "auto | cs | en");
        section.SetValue("Empty", null);

        var text = document.ToString();
        var parsed = IniDocument.Parse(text);

        Assert.Contains("; header", text, StringComparison.Ordinal);
        Assert.Contains("; auto | cs | en", text, StringComparison.Ordinal);
        Assert.Equal("cs", parsed.GetValue("General", "Language"));
        Assert.Equal(string.Empty, parsed.GetValue("General", "Empty"));
    }
}
