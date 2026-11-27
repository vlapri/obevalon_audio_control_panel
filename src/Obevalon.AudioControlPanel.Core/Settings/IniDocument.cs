// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text;

namespace Obevalon.AudioControlPanel.Core.Settings;

/// <summary>
/// Minimal INI document: ordered sections with ordered key/value entries.
/// Section and key names are case-insensitive. Comments are ignored when parsing;
/// comments can be attached to sections and entries for writing.
/// Values are stored raw (no inline comment stripping), so they may contain ';' or '#'.
/// </summary>
public sealed class IniDocument
{
    private readonly List<IniSection> _sections = [];

    /// <summary>Comment lines written at the top of the file.</summary>
    public List<string> HeaderComments { get; } = [];

    public IReadOnlyList<IniSection> Sections => _sections;

    public IniSection? GetSection(string name) =>
        _sections.Find(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

    public IniSection GetOrAddSection(string name)
    {
        var section = GetSection(name);
        if (section is null)
        {
            section = new IniSection(name);
            _sections.Add(section);
        }

        return section;
    }

    public string? GetValue(string section, string key) => GetSection(section)?.GetValue(key);

    public static IniDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var document = new IniDocument();
        IniSection? current = null;

        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } rawLine)
        {
            var line = rawLine.Trim().TrimStart('﻿');
            if (line.Length == 0 || line[0] is ';' or '#')
            {
                continue;
            }

            if (line[0] == '[')
            {
                var end = line.LastIndexOf(']');
                current = end > 1 ? document.GetOrAddSection(line[1..end].Trim()) : null;
                continue;
            }

            var separator = line.IndexOf('=', StringComparison.Ordinal);
            if (current is null || separator <= 0)
            {
                // Entries outside a section and lines without "=" are ignored.
                continue;
            }

            current.SetValue(line[..separator].Trim(), line[(separator + 1)..].Trim());
        }

        return document;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        foreach (var comment in HeaderComments)
        {
            builder.Append("; ").Append(comment).Append("\r\n");
        }

        foreach (var section in _sections)
        {
            if (builder.Length > 0)
            {
                builder.Append("\r\n");
            }

            if (section.Comment is { } sectionComment)
            {
                builder.Append("; ").Append(sectionComment).Append("\r\n");
            }

            builder.Append('[').Append(section.Name).Append("]\r\n");
            foreach (var entry in section.Entries)
            {
                if (entry.Comment is { } entryComment)
                {
                    builder.Append("; ").Append(entryComment).Append("\r\n");
                }

                builder.Append(entry.Key).Append('=').Append(entry.Value).Append("\r\n");
            }
        }

        return builder.ToString();
    }
}

/// <summary>A section of an <see cref="IniDocument"/>.</summary>
public sealed class IniSection(string name)
{
    private readonly List<IniEntry> _entries = [];

    public string Name { get; } = name;

    /// <summary>Optional comment written above the section header.</summary>
    public string? Comment { get; set; }

    public IReadOnlyList<IniEntry> Entries => _entries;

    public string? GetValue(string key) =>
        _entries.Find(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;

    /// <summary>Adds or replaces a value. Line breaks in the value are replaced by spaces.</summary>
    public void SetValue(string key, string? value, string? comment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var sanitized = (value ?? string.Empty).ReplaceLineEndings(" ").Trim();
        var index = _entries.FindIndex(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            _entries[index] = _entries[index] with { Value = sanitized, Comment = comment ?? _entries[index].Comment };
        }
        else
        {
            _entries.Add(new IniEntry(key, sanitized, comment));
        }
    }
}

/// <summary>A key/value entry of an <see cref="IniSection"/>.</summary>
public sealed record IniEntry(string Key, string Value, string? Comment = null);
