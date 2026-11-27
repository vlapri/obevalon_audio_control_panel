// SPDX-License-Identifier: GPL-3.0-or-later
using System.Reflection;
using System.Security.Cryptography;

namespace Obevalon.AudioControlPanel.Core.Updates;

/// <summary>
/// Verifies the ECDSA P-256 / SHA-256 signature of update.json (IEEE P1363 format, Base64).
/// The trusted public keys are embedded from update-signing-keys.pem (SPEC §5.5).
/// </summary>
public static class UpdateSignature
{
    private const string KeysResourceName = "Obevalon.AudioControlPanel.Core.Updates.update-signing-keys.pem";

    /// <summary>Trusted public keys (PEM). Empty means updates are not configured yet.</summary>
    public static IReadOnlyList<string> TrustedPublicKeys { get; } = LoadEmbeddedKeys();

    /// <summary>True when <paramref name="signatureBase64"/> is a valid signature of <paramref name="data"/> by any of the keys.</summary>
    public static bool Verify(ReadOnlySpan<byte> data, string? signatureBase64, IReadOnlyList<string> publicKeysPem)
    {
        ArgumentNullException.ThrowIfNull(publicKeysPem);

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(signatureBase64?.Trim() ?? string.Empty);
        }
        catch (FormatException)
        {
            return false;
        }

        if (signature.Length == 0)
        {
            return false;
        }

        foreach (var pem in publicKeysPem)
        {
            using var ecdsa = ECDsa.Create();
            try
            {
                ecdsa.ImportFromPem(pem);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (ecdsa.KeySize == 256 && ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA256))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Splits a PEM file into individual "PUBLIC KEY" blocks.</summary>
    public static IReadOnlyList<string> ParsePublicKeys(string? pemText)
    {
        const string Begin = "-----BEGIN PUBLIC KEY-----";
        const string End = "-----END PUBLIC KEY-----";

        var keys = new List<string>();
        if (string.IsNullOrEmpty(pemText))
        {
            return keys;
        }

        var position = 0;
        while (true)
        {
            var start = pemText.IndexOf(Begin, position, StringComparison.Ordinal);
            if (start < 0)
            {
                break;
            }

            var end = pemText.IndexOf(End, start, StringComparison.Ordinal);
            if (end < 0)
            {
                break;
            }

            keys.Add(pemText[start..(end + End.Length)]);
            position = end + End.Length;
        }

        return keys;
    }

    private static IReadOnlyList<string> LoadEmbeddedKeys()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(KeysResourceName);
        if (stream is null)
        {
            return [];
        }

        using var reader = new StreamReader(stream);
        return ParsePublicKeys(reader.ReadToEnd());
    }
}
