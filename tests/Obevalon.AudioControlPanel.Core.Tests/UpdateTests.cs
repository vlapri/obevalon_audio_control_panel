// SPDX-License-Identifier: GPL-3.0-or-later
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Obevalon.AudioControlPanel.Core.Updates;

namespace Obevalon.AudioControlPanel.Core.Tests;

public sealed class UpdateTests : IDisposable
{
    private const string Rid = "win-x64";
    private static readonly Uri ManifestUri = new("https://example.test/releases/latest/download/update.json");

    private readonly ECDsa _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "oacp-update-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _key.Dispose();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private string PublicKey => _key.ExportSubjectPublicKeyInfoPem();

    private static byte[] Exe { get; } = Encoding.ASCII.GetBytes("MZ fake executable");

    private static string ExeHash => Convert.ToHexStringLower(SHA256.HashData(Exe));

    private static UpdateManifest Manifest(string version = "1.2.0", string? minimumRuntime = "10.0.0") => new(
        UpdateManifest.CurrentSchema,
        version,
        new DateTimeOffset(2027, 1, 15, 12, 0, 0, TimeSpan.Zero),
        "https://example.test/releases/tag/v" + version,
        minimumRuntime,
        new Dictionary<string, UpdateAsset>
        {
            [Rid] = new("https://example.test/download/OACP-win-x64.exe", ExeHash, Exe.Length),
        });

    private (byte[] Json, string Signature) Signed(UpdateManifest manifest)
    {
        var json = Encoding.UTF8.GetBytes(manifest.ToJson());
        return (json, Convert.ToBase64String(_key.SignData(json, HashAlgorithmName.SHA256)));
    }

    private static HttpClient Client(byte[] json, string signature, byte[]? exe = null) =>
        new(new FakeHandler(uri => uri.AbsolutePath switch
        {
            var p when p.EndsWith(".json.sig", StringComparison.Ordinal) => Encoding.ASCII.GetBytes(signature),
            var p when p.EndsWith(".json", StringComparison.Ordinal) => json,
            var p when p.EndsWith(".exe", StringComparison.Ordinal) => exe ?? Exe,
            _ => null,
        }));

    private Task<UpdateCheckResult> Check(
        HttpClient client,
        string current = "1.0.0",
        string? skipped = null,
        bool manual = false,
        bool runtimeOk = true,
        IReadOnlyList<string>? keys = null) =>
        new UpdateChecker(client, keys ?? [PublicKey]).CheckAsync(
            ManifestUri, SemanticVersion.Parse(current)!, Rid, skipped, manual, _ => runtimeOk);

    [Fact]
    public async Task NewerVersion_IsAvailable()
    {
        var (json, signature) = Signed(Manifest());

        var result = await Check(Client(json, signature));

        Assert.Equal(UpdateCheckStatus.Available, result.Status);
        Assert.Equal("1.2.0", result.Version!.ToString());
        Assert.Equal(ExeHash, result.Asset!.Sha256);
    }

    [Theory]
    [InlineData("1.2.0")]
    [InlineData("1.3.0")]
    public async Task SameOrOlderVersion_IsUpToDate(string current)
    {
        var (json, signature) = Signed(Manifest());

        Assert.Equal(UpdateCheckStatus.UpToDate, (await Check(Client(json, signature), current)).Status);
    }

    [Fact]
    public async Task SkippedVersion_IsIgnoredOnlyForAutomaticChecks()
    {
        var (json, signature) = Signed(Manifest());

        Assert.Equal(UpdateCheckStatus.UpToDate, (await Check(Client(json, signature), skipped: "1.2.0")).Status);
        Assert.Equal(UpdateCheckStatus.Available, (await Check(Client(json, signature), skipped: "1.2.0", manual: true)).Status);
        Assert.Equal(UpdateCheckStatus.Available, (await Check(Client(json, signature), skipped: "1.1.0")).Status);
    }

    [Fact]
    public async Task TamperedManifest_IsRejected()
    {
        var (json, signature) = Signed(Manifest());
        var tampered = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(json).Replace("1.2.0", "9.9.9", StringComparison.Ordinal));

        Assert.Equal(UpdateCheckStatus.InvalidSignature, (await Check(Client(tampered, signature))).Status);
    }

    [Fact]
    public async Task SignatureByAnotherKey_IsRejected()
    {
        var (json, signature) = Signed(Manifest());
        using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var result = await Check(Client(json, signature), keys: [other.ExportSubjectPublicKeyInfoPem()]);

        Assert.Equal(UpdateCheckStatus.InvalidSignature, result.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64!")]
    public async Task MissingOrGarbageSignature_IsRejected(string signature)
    {
        var (json, _) = Signed(Manifest());

        Assert.Equal(UpdateCheckStatus.InvalidSignature, (await Check(Client(json, signature))).Status);
    }

    [Fact]
    public async Task NoTrustedKeys_DisablesUpdates()
    {
        var (json, signature) = Signed(Manifest());

        Assert.Equal(UpdateCheckStatus.NotConfigured, (await Check(Client(json, signature), keys: [])).Status);
    }

    [Fact]
    public async Task MissingPlatform_IsReported()
    {
        var manifest = Manifest() with { Assets = new Dictionary<string, UpdateAsset>() };
        var (json, signature) = Signed(manifest);

        Assert.Equal(UpdateCheckStatus.NoAssetForPlatform, (await Check(Client(json, signature))).Status);
    }

    [Fact]
    public async Task NonHttpsAsset_IsRejected()
    {
        var manifest = Manifest() with
        {
            Assets = new Dictionary<string, UpdateAsset> { [Rid] = new("http://example.test/OACP.exe", ExeHash, Exe.Length) },
        };
        var (json, signature) = Signed(manifest);

        Assert.Equal(UpdateCheckStatus.NoAssetForPlatform, (await Check(Client(json, signature))).Status);
    }

    [Fact]
    public async Task MissingRuntime_IsReported()
    {
        var (json, signature) = Signed(Manifest());

        Assert.Equal(UpdateCheckStatus.RuntimeRequired, (await Check(Client(json, signature), runtimeOk: false)).Status);
    }

    [Fact]
    public async Task NetworkError_IsReported()
    {
        using var client = new HttpClient(new FakeHandler(_ => null));

        Assert.Equal(UpdateCheckStatus.NetworkError, (await Check(client)).Status);
    }

    [Fact]
    public async Task SignedGarbage_IsInvalidManifest()
    {
        var json = Encoding.UTF8.GetBytes("{\"schema\": 1}");
        var signature = Convert.ToBase64String(_key.SignData(json, HashAlgorithmName.SHA256));

        Assert.Equal(UpdateCheckStatus.InvalidManifest, (await Check(Client(json, signature))).Status);
    }

    [Fact]
    public void Manifest_RoundTripsThroughJson()
    {
        var manifest = Manifest();

        var parsed = UpdateManifest.Parse(Encoding.UTF8.GetBytes(manifest.ToJson()))!;

        Assert.Equal(manifest.Version, parsed.Version);
        Assert.Equal(manifest.MinimumRuntime, parsed.MinimumRuntime);
        Assert.Equal(manifest.Assets[Rid], parsed.Assets[Rid]);
        Assert.Contains("\"releaseNotesUrl\"", manifest.ToJson(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Download_VerifiesHashAndReportsProgress()
    {
        var (json, signature) = Signed(Manifest());
        var progress = new List<double>();

        var path = await new UpdateDownloader(Client(json, signature))
            .DownloadAsync(Manifest().Assets[Rid], _directory, new SyncProgress(progress.Add));

        Assert.Equal(Exe, await File.ReadAllBytesAsync(path));
        Assert.Equal(1.0, progress[^1]);
    }

    [Fact]
    public async Task Download_WithWrongContent_IsRejectedAndDeleted()
    {
        var (json, signature) = Signed(Manifest());
        var evil = Encoding.ASCII.GetBytes("MZ evil executable");

        await Assert.ThrowsAsync<UpdateVerificationException>(() =>
            new UpdateDownloader(Client(json, signature, evil)).DownloadAsync(Manifest().Assets[Rid], _directory));

        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("10.0.0", true)]
    [InlineData("10.0.5", true)]
    [InlineData("10.0.6", false)]
    [InlineData("10.1.0", false)]
    [InlineData("11.0.0", false)]
    [InlineData("garbage", false)]
    public void RuntimeRequirement_ComparesWithinMajorVersion(string? minimum, bool expected)
    {
        Assert.Equal(expected, RuntimeRequirement.IsSatisfied(minimum, [new Version(8, 0, 20), new Version(10, 0, 5)]));
    }

    [Fact]
    public void EmbeddedSigningKey_IsValidP256Key()
    {
        var key = Assert.Single(UpdateSignature.TrustedPublicKeys);
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(key);

        Assert.Equal(256, ecdsa.KeySize);
    }

    [Fact]
    public void ParsePublicKeys_ReadsMultipleBlocks()
    {
        using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var text = "# comment\n" + PublicKey + "\n" + other.ExportSubjectPublicKeyInfoPem();

        Assert.Equal(2, UpdateSignature.ParsePublicKeys(text).Count);
        Assert.Empty(UpdateSignature.ParsePublicKeys(null));
    }

    private sealed class SyncProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }

    private sealed class FakeHandler(Func<Uri, byte[]?> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = respond(request.RequestUri!);
            if (body is null)
            {
                throw new HttpRequestException("Network unreachable");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) });
        }
    }
}
