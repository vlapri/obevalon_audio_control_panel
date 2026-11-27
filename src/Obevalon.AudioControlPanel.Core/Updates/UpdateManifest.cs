// SPDX-License-Identifier: GPL-3.0-or-later
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Obevalon.AudioControlPanel.Core.Updates;

/// <summary>Contents of update.json published with every release (SPEC §5.5).</summary>
public sealed record UpdateManifest(
    int Schema,
    string Version,
    DateTimeOffset? ReleaseDate,
    string? ReleaseNotesUrl,
    string? MinimumRuntime,
    IReadOnlyDictionary<string, UpdateAsset> Assets)
{
    public const int CurrentSchema = 1;

    /// <summary>Parses the manifest; returns null when it is not valid JSON or misses required data.</summary>
    public static UpdateManifest? Parse(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            var manifest = JsonSerializer.Deserialize(utf8Json, UpdateJsonContext.Default.UpdateManifest);
            return manifest is { Schema: CurrentSchema, Assets: not null }
                && SemanticVersion.Parse(manifest.Version) is not null
                ? manifest
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, UpdateJsonContext.Default.UpdateManifest);
}

/// <summary>One downloadable executable in <see cref="UpdateManifest.Assets"/>, keyed by runtime identifier.</summary>
public sealed record UpdateAsset(string Url, string Sha256, long Size);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(UpdateManifest))]
internal sealed partial class UpdateJsonContext : JsonSerializerContext;
