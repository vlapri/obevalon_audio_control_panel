// SPDX-License-Identifier: GPL-3.0-or-later
using System.Security.Cryptography;

namespace Obevalon.AudioControlPanel.Core.Updates;

/// <summary>Thrown when a downloaded file does not match the manifest (size or SHA-256).</summary>
public sealed class UpdateVerificationException(string message) : Exception(message);

/// <summary>Downloads an update executable and verifies its SHA-256 (SPEC F9.5, F9.6).</summary>
public sealed class UpdateDownloader(HttpClient http)
{
    /// <summary>
    /// Downloads <paramref name="asset"/> into <paramref name="directory"/> and returns the file path.
    /// The file is deleted when the download fails, is cancelled or does not match the manifest.
    /// </summary>
    /// <param name="progress">Receives values 0.0–1.0.</param>
    public async Task<string> DownloadAsync(
        UpdateAsset asset,
        string directory,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentException.ThrowIfNullOrEmpty(directory);

        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"OACP-update-{Guid.NewGuid():N}.exe");

        try
        {
            using var response = await http.GetAsync(new Uri(asset.Url), HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var total = response.Content.Headers.ContentLength ?? asset.Size;
            long received = 0;

            await using (var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    sha256.AppendData(buffer, 0, read);
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    received += read;
                    if (received > asset.Size)
                    {
                        throw new UpdateVerificationException("The download is larger than announced in the manifest.");
                    }

                    if (total > 0)
                    {
                        progress?.Report(Math.Min(1.0, (double)received / total));
                    }
                }
            }

            if (received != asset.Size)
            {
                throw new UpdateVerificationException("The download size does not match the manifest.");
            }

            var hash = Convert.ToHexStringLower(sha256.GetHashAndReset());
            if (!string.Equals(hash, asset.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new UpdateVerificationException("The SHA-256 checksum of the download does not match the manifest.");
            }

            progress?.Report(1.0);
            return path;
        }
        catch
        {
            TryDelete(path);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort; the temp folder gets cleaned up eventually.
        }
    }
}
