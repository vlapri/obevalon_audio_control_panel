// SPDX-License-Identifier: GPL-3.0-or-later
using System.Globalization;

namespace Obevalon.AudioControlPanel.Core.Updates;

/// <summary>A SemVer 2.0 version (major.minor.patch[-prerelease]); build metadata is ignored.</summary>
public sealed class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    private SemanticVersion(int major, int minor, int patch, string? preRelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
    }

    public int Major { get; }

    public int Minor { get; }

    public int Patch { get; }

    /// <summary>Pre-release part without the dash, e.g. "beta.1"; null for a release.</summary>
    public string? PreRelease { get; }

    public bool IsPreRelease => PreRelease is not null;

    public static SemanticVersion? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var value = text.Trim();
        if (value.StartsWith('v') || value.StartsWith('V'))
        {
            value = value[1..];
        }

        var plus = value.IndexOf('+', StringComparison.Ordinal);
        if (plus >= 0)
        {
            value = value[..plus];
        }

        string? preRelease = null;
        var dash = value.IndexOf('-', StringComparison.Ordinal);
        if (dash >= 0)
        {
            preRelease = value[(dash + 1)..];
            value = value[..dash];
            if (preRelease.Length == 0 || preRelease.Split('.').Any(p => p.Length == 0))
            {
                return null;
            }
        }

        var parts = value.Split('.');
        if (parts.Length != 3
            || !TryParsePart(parts[0], out var major)
            || !TryParsePart(parts[1], out var minor)
            || !TryParsePart(parts[2], out var patch))
        {
            return null;
        }

        return new SemanticVersion(major, minor, patch, preRelease);
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var result = Major.CompareTo(other.Major);
        if (result == 0)
        {
            result = Minor.CompareTo(other.Minor);
        }

        if (result == 0)
        {
            result = Patch.CompareTo(other.Patch);
        }

        return result != 0 ? result : ComparePreRelease(PreRelease, other.PreRelease);
    }

    public bool Equals(SemanticVersion? other) => other is not null && CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is SemanticVersion other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, PreRelease);

    public override string ToString() =>
        PreRelease is null ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{PreRelease}";

    public static bool operator >(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) > 0;

    public static bool operator <(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) < 0;

    public static bool operator >=(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) >= 0;

    public static bool operator <=(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) <= 0;

    public static bool operator ==(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) == 0;

    public static bool operator !=(SemanticVersion? left, SemanticVersion? right) => Compare(left, right) != 0;

    private static int Compare(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? (right is null ? 0 : -1) : left.CompareTo(right);

    private static bool TryParsePart(string text, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value)
        && (text.Length == 1 || text[0] != '0');

    /// <summary>A release is newer than any of its pre-releases; identifiers compare per SemVer §11.</summary>
    private static int ComparePreRelease(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return left is null ? (right is null ? 0 : 1) : -1;
        }

        var a = left.Split('.');
        var b = right.Split('.');
        for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            var aNumeric = int.TryParse(a[i], NumberStyles.None, CultureInfo.InvariantCulture, out var aNumber);
            var bNumeric = int.TryParse(b[i], NumberStyles.None, CultureInfo.InvariantCulture, out var bNumber);
            var result = (aNumeric, bNumeric) switch
            {
                (true, true) => aNumber.CompareTo(bNumber),
                (true, false) => -1,
                (false, true) => 1,
                _ => string.CompareOrdinal(a[i], b[i]),
            };

            if (result != 0)
            {
                return result;
            }
        }

        return a.Length.CompareTo(b.Length);
    }
}
