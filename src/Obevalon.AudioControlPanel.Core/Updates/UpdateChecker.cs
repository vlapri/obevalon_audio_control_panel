// SPDX-License-Identifier: GPL-3.0-or-later
namespace Obevalon.AudioControlPanel.Core.Updates;

/// <summary>Outcome of an update check.</summary>
public enum UpdateCheckStatus
{
    /// <summary>No newer version (or the newer one was skipped by the user).</summary>
    UpToDate,

    /// <summary>A newer version can be installed.</summary>
    Available,

    /// <summary>A newer version exists but needs a newer .NET Desktop Runtime (SPEC F9.9).</summary>
    RuntimeRequired,

    /// <summary>The release has no executable for this platform.</summary>
    NoAssetForPlatform,

    /// <summary>The manifest signature is missing or invalid; nothing may be installed.</summary>
    InvalidSignature,

    /// <summary>The manifest is not valid.</summary>
    InvalidManifest,

    /// <summary>Network or server error.</summary>
    NetworkError,

    /// <summary>No trusted signing key is built into this version, so updates are disabled.</summary>
    NotConfigured,
}

public sealed record UpdateCheckResult(
    UpdateCheckStatus Status,
    UpdateManifest? Manifest = null,
    UpdateAsset? Asset = null,
    string? Error = null)
{
    public SemanticVersion? Version => SemanticVersion.Parse(Manifest?.Version);
}

/// <summary>Downloads and verifies update.json (SPEC F9.2, F9.6).</summary>
public sealed class UpdateChecker(HttpClient http, IReadOnlyList<string> trustedPublicKeys)
{
    /// <summary>Default location of the manifest of the latest stable release.</summary>
    public static Uri DefaultManifestUri { get; } = new(AppInfo.RepositoryUrl + "/releases/latest/download/update.json");

    /// <param name="skippedVersion">Version the user chose to skip; ignored when <paramref name="manual"/> is true.</param>
    /// <param name="isRuntimeSatisfied">Tells whether the installed .NET runtimes satisfy a minimum version.</param>
    public async Task<UpdateCheckResult> CheckAsync(
        Uri manifestUri,
        SemanticVersion currentVersion,
        string runtimeIdentifier,
        string? skippedVersion,
        bool manual,
        Func<string?, bool> isRuntimeSatisfied,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifestUri);
        ArgumentNullException.ThrowIfNull(currentVersion);
        ArgumentNullException.ThrowIfNull(isRuntimeSatisfied);

        if (trustedPublicKeys.Count == 0)
        {
            return new UpdateCheckResult(UpdateCheckStatus.NotConfigured);
        }

        byte[] manifestBytes;
        string signature;
        try
        {
            manifestBytes = await http.GetByteArrayAsync(manifestUri, cancellationToken).ConfigureAwait(false);
            signature = await http.GetStringAsync(new Uri(manifestUri + ".sig"), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return new UpdateCheckResult(UpdateCheckStatus.NetworkError, Error: ex.Message);
        }

        if (!UpdateSignature.Verify(manifestBytes, signature, trustedPublicKeys))
        {
            return new UpdateCheckResult(UpdateCheckStatus.InvalidSignature);
        }

        var manifest = UpdateManifest.Parse(manifestBytes);
        var version = SemanticVersion.Parse(manifest?.Version);
        if (manifest is null || version is null)
        {
            return new UpdateCheckResult(UpdateCheckStatus.InvalidManifest);
        }

        var skipped = SemanticVersion.Parse(skippedVersion);
        if (version <= currentVersion || (!manual && skipped is not null && version <= skipped))
        {
            return new UpdateCheckResult(UpdateCheckStatus.UpToDate, manifest);
        }

        if (!manifest.Assets.TryGetValue(runtimeIdentifier, out var asset)
            || !Uri.TryCreate(asset.Url, UriKind.Absolute, out var assetUri)
            || assetUri.Scheme != Uri.UriSchemeHttps
            || asset.Sha256.Length != 64)
        {
            return new UpdateCheckResult(UpdateCheckStatus.NoAssetForPlatform, manifest);
        }

        return isRuntimeSatisfied(manifest.MinimumRuntime)
            ? new UpdateCheckResult(UpdateCheckStatus.Available, manifest, asset)
            : new UpdateCheckResult(UpdateCheckStatus.RuntimeRequired, manifest, asset);
    }
}
