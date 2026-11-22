using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Wii2Xbox;

internal enum UpdateDistribution { Installer, Portable }

internal sealed record ReleaseAsset(Version Version, Uri Url, long Size, string Sha256);
internal sealed record StagedUpdate(ReleaseAsset Release, string Directory, string AssetPath);

internal sealed class AppUpdater : IDisposable
{
    internal const string UpdateMutex = @"Local\Wii2Xbox.Update";
    internal const string ResultArgument = "--update-result=";
    internal const string ManifestName = "update-files.txt";
    internal static readonly Uri ReleasesUrl = new("https://api.github.com/repos/Spontz/Wii2Xbox/releases?per_page=100");
    private const string DownloadRoot = "https://github.com/Spontz/Wii2Xbox/releases/download/";
    private const long MaxAssetSize = 256L * 1024 * 1024;
    private static readonly Regex Tag = new(@"\Av(0|[1-9]\d{0,2})\.(0|[1-9]\d{0,2})\.(0|[1-9]\d{0,4})-(installer|portable)\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private readonly string executable;
    private readonly UpdateDistribution distribution;
    private readonly Version current;
    private readonly HttpClient client;

    internal AppUpdater(string executable, UpdateDistribution distribution, Version current, HttpMessageHandler? handler = null)
    {
        this.executable = executable;
        this.distribution = distribution;
        this.current = current;
        client = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
        {
            Timeout = TimeSpan.FromSeconds(30),
            MaxResponseContentBufferSize = 4 * 1024 * 1024
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Wii2Xbox/" + current);
    }

    internal static AppUpdater? ForCurrentProcess()
    {
        if (!VigemBusSetup.IsBundled || Environment.ProcessPath is not { } path ||
            !Path.GetFileName(path).Equals("Wii2Xbox.exe", StringComparison.OrdinalIgnoreCase) ||
            typeof(AppUpdater).Assembly.GetName().Version is not { } version) return null;
        try
        {
            bool installed = StartupRegistration.IsInstalledExecutable(path);
            if (!installed && !File.Exists(Path.Combine(Path.GetDirectoryName(path)!, ManifestName))) return null;
            return new(path, installed ? UpdateDistribution.Installer : UpdateDistribution.Portable,
                new Version(version.Major, version.Minor, version.Build));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            Debug.WriteLine(error);
            return null;
        }
    }

    internal async Task<StagedUpdate?> CheckAndStage(CancellationToken cancellation)
    {
        string metadata = await client.GetStringAsync(ReleasesUrl, cancellation).ConfigureAwait(false);
        if (SelectRelease(metadata, current, distribution) is not { } release) return null;
        string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wii2Xbox", "Updates");
        if (Directory.Exists(root) && (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("The update cache cannot be a reparse point.");
        string directory = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string asset = Path.Combine(directory, Path.GetFileName(release.Url.LocalPath));
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            deadline.CancelAfter(TimeSpan.FromMinutes(10));
            await Download(release, asset, deadline.Token).ConfigureAwait(false);
            return new(release, directory, asset);
        }
        catch
        {
            RemoveStage(directory);
            throw;
        }
    }

    internal static ReleaseAsset? SelectRelease(string metadata, Version current, UpdateDistribution distribution)
    {
        string flavor = distribution == UpdateDistribution.Installer ? "installer" : "portable";
        string filename = distribution == UpdateDistribution.Installer ? "Wii2Xbox-win-x64.msi" : "Wii2Xbox-win-x64.zip";
        ReleaseAsset? latest = null;
        foreach (var release in JsonSerializer.Deserialize<GitHubRelease[]>(metadata) ?? [])
        {
            if (release is null || release.Draft || release.Prerelease || release.Tag is not { } tag) continue;
            Match match = Tag.Match(tag);
            if (!match.Success || match.Groups[4].Value != flavor ||
                !Version.TryParse(tag[1..tag.LastIndexOf('-')], out var version) ||
                version.Major > 255 || version.Minor > 255 || version.Build > 65535 ||
                version <= current || latest is not null && version <= latest.Version) continue;
            string url = DownloadRoot + tag + "/" + filename;
            var asset = release.Assets?.FirstOrDefault(asset => asset is not null && asset.Name == filename && asset.State == "uploaded" &&
                asset.Url == url && asset.Size is > 0 and <= MaxAssetSize &&
                asset.Digest is { Length: 71 } digest && digest.StartsWith("sha256:", StringComparison.Ordinal) &&
                digest.AsSpan(7).IndexOfAnyExcept("0123456789abcdefABCDEF") == -1);
            if (asset is not null)
                latest = new(version, new Uri(url), asset.Size, asset.Digest![7..]);
        }
        return latest;
    }

    internal async Task Download(ReleaseAsset release, string destination, CancellationToken cancellation)
    {
        Uri uri = release.Url;
        for (int redirect = 0; redirect <= 4; redirect++)
        {
            if (!AllowedDownload(uri))
                throw new IOException("Unexpected update download origin.");
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);
            if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308 &&
                response.Headers.Location is { } location)
            {
                uri = new Uri(uri, location);
                continue;
            }
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is long length && length != release.Size)
                throw new IOException("Update size does not match the release.");
            await using var source = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
            await using var destinationFile = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            byte[] buffer = new byte[81920];
            long bytes = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellation).ConfigureAwait(false)) != 0)
            {
                bytes += read;
                if (bytes > release.Size) throw new IOException("Update exceeded the release size.");
                hash.AppendData(buffer, 0, read);
                await destinationFile.WriteAsync(buffer.AsMemory(0, read), cancellation).ConfigureAwait(false);
            }
            if (bytes != release.Size || !Convert.ToHexString(hash.GetHashAndReset()).Equals(release.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Update size or digest does not match the release.");
            return;
        }
        throw new IOException("Too many update download redirects.");
    }

    private static bool AllowedDownload(Uri url) =>
        url.Scheme == Uri.UriSchemeHttps && url.IsDefaultPort && url.UserInfo.Length == 0 &&
        (url.Host is "release-assets.githubusercontent.com" or "objects.githubusercontent.com" ||
         url.Host == "github.com" && url.AbsoluteUri.StartsWith(DownloadRoot, StringComparison.Ordinal));

    internal async Task<UpdateHandoff?> PrepareHandoff(StagedUpdate staged, bool background)
    {
        string nonce = Guid.NewGuid().ToString("N");
        string readyName = @"Local\Wii2Xbox.UpdateReady." + nonce;
        string startName = @"Local\Wii2Xbox.UpdateStart." + nonce;
        using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, readyName);
        var start = new EventWaitHandle(false, EventResetMode.ManualReset, startName);
        try
        {
            using var resource = typeof(AppUpdater).Assembly.GetManifestResourceStream("Wii2Xbox.ApplyUpdate")!;
            using var reader = new StreamReader(resource);
            string code = Convert.ToBase64String(Encoding.Unicode.GetBytes(reader.ReadToEnd()));
            var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory,
                "WindowsPowerShell", "v1.0", "powershell.exe"))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = staged.Directory
            };
            foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-EncodedCommand", code })
                psi.ArgumentList.Add(argument);
            using var currentProcess = Process.GetCurrentProcess();
            psi.Environment["WII2XBOX_UPDATE"] = JsonSerializer.Serialize(new
            {
                Executable = executable,
                staged.AssetPath,
                staged.Directory,
                staged.Release.Sha256,
                Version = staged.Release.Version.ToString(),
                Installer = distribution == UpdateDistribution.Installer,
                Background = background,
                ProcessId = currentProcess.Id,
                ProcessStarted = currentProcess.StartTime.ToUniversalTime().Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ReadyEvent = readyName,
                StartEvent = startName,
                Mutex = UpdateMutex
            });
            using var helper = Process.Start(psi);
            if (helper is not null && await Task.Run(() => ready.WaitOne(TimeSpan.FromSeconds(5))).ConfigureAwait(false))
                return new UpdateHandoff(start);
        }
        catch { start.Dispose(); throw; }
        start.Dispose();
        return null;
    }

    internal sealed class UpdateHandoff(EventWaitHandle start) : IDisposable
    {
        internal void Begin() => start.Set();
        public void Dispose() => start.Dispose();
    }

    internal static void RemoveStage(string directory)
    {
        try { Directory.Delete(directory, recursive: true); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { Debug.WriteLine(error); }
    }

    public void Dispose() => client.Dispose();

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string? Tag,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool Prerelease,
        [property: JsonPropertyName("assets")] GitHubAsset[]? Assets);

    private sealed record GitHubAsset(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("browser_download_url")] string? Url,
        [property: JsonPropertyName("state")] string? State,
        [property: JsonPropertyName("size")] long Size,
        [property: JsonPropertyName("digest")] string? Digest);
}
