using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace Wii2Xbox.Tests;

public sealed class AppUpdaterTests
{
    private const string Official = "https://github.com/Spontz/Wii2Xbox/releases/download/";
    private static readonly byte[] Package = [0x01, 0x05, 0xa0];
    private static readonly string Digest = Convert.ToHexString(SHA256.HashData(Package));

    [Fact]
    public void SelectsTheHighestNewerStableReleaseForEachDistribution()
    {
        var releases = JsonSerializer.Serialize(new[]
        {
            Release("v1.3.0-installer"), Release("v1.9.0-portable"), Release("v1.10.0-installer"),
            Release("v2.0.0-installer", draft: true), Release("v2.1.0-installer", prerelease: true),
            Release("v3.0.0-installer", approvedUrl: false), Release("v1.2.0-installer")
        });
        Assert.Equal(new Version(1, 10, 0), AppUpdater.SelectRelease(releases, new(1, 2, 0), UpdateDistribution.Installer)?.Version);
        Assert.Equal(new Version(1, 9, 0), AppUpdater.SelectRelease(releases, new(1, 2, 0), UpdateDistribution.Portable)?.Version);
    }

    [Fact]
    public void IgnoresIdenticalOlderOrUnverifiedPackages()
    {
        var releases = JsonSerializer.Serialize(new[]
        {
            Release("v1.2.0-portable"), Release("v1.1.9-portable"),
            Release("v1.4.0-portable", digest: "sha256:bad"), Release("v1.4.0-installer")
        });
        Assert.Null(AppUpdater.SelectRelease(releases, new(1, 2, 0), UpdateDistribution.Portable));
    }

    [Fact]
    public async Task VerifiesTheDigestAfterFollowingAGitHubAssetRedirect()
    {
        var calls = new List<string>();
        using var updater = new AppUpdater("Wii2Xbox.exe", UpdateDistribution.Portable, new(1, 2, 0), new Responses(url =>
        {
            calls.Add(url.Host);
            if (url.Host == "github.com")
                return new(HttpStatusCode.Found) { Headers = { Location = new Uri("https://release-assets.githubusercontent.com/test") } };
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent(Package) };
        }));
        string path = TemporaryFile();
        try
        {
            await updater.Download(VerifiedAsset(), path, CancellationToken.None);
            Assert.Equal(Package, File.ReadAllBytes(path));
            Assert.Equal(new[] { "github.com", "release-assets.githubusercontent.com" }, calls);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task RejectsAnUnexpectedRedirectBeforeRequestingIt()
    {
        int calls = 0;
        using var updater = new AppUpdater("Wii2Xbox.exe", UpdateDistribution.Portable, new(1, 2, 0), new Responses(_ =>
        {
            calls++;
            return new(HttpStatusCode.Found) { Headers = { Location = new Uri("https://example.invalid/asset") } };
        }));
        string path = TemporaryFile();
        await Assert.ThrowsAsync<IOException>(() => updater.Download(VerifiedAsset(), path, CancellationToken.None));
        Assert.Equal(1, calls);
        Assert.False(File.Exists(path));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectsWrongSizeAndWrongDigest(bool sameSize)
    {
        byte[] corrupt = sameSize ? [1, 2, 3] : [1, 2];
        using var updater = new AppUpdater("Wii2Xbox.exe", UpdateDistribution.Portable, new(1, 2, 0), new Responses(
            _ => new(HttpStatusCode.OK) { Content = new ByteArrayContent(corrupt) }));
        string path = TemporaryFile();
        try { await Assert.ThrowsAsync<IOException>(() => updater.Download(VerifiedAsset(), path, CancellationToken.None)); }
        finally { File.Delete(path); }
    }

    private static string TemporaryFile() => Path.Combine(Path.GetTempPath(), "Wii2Xbox-" + Guid.NewGuid().ToString("N"));

    private static ReleaseAsset VerifiedAsset() =>
        new(new(1, 3, 0), new Uri(Official + "v1.3.0-portable/Wii2Xbox-win-x64.zip"), Package.Length, Digest);

    private static object Release(string tag, bool draft = false, bool prerelease = false,
        bool approvedUrl = true, string? digest = null)
    {
        string name = tag.EndsWith("-installer", StringComparison.Ordinal) ? "Wii2Xbox-win-x64.msi" : "Wii2Xbox-win-x64.zip";
        return new
        {
            tag_name = tag, draft, prerelease,
            assets = new[]
            {
                new
                {
                    name, browser_download_url = (approvedUrl ? Official : "https://example.invalid/") + tag + "/" + name,
                    size = Package.Length, state = "uploaded", digest = digest ?? "sha256:" + Digest
                }
            }
        };
    }

    private sealed class Responses(Func<Uri, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request.RequestUri!));
    }
}
