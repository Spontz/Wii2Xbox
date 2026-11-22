using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Wii2Xbox;

internal sealed class ProfileStore(string directory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public Profile Read(string deviceId, string? formerId = null)
    {
        try { return Profile.Load(PathFor(deviceId)); }
        catch (FileNotFoundException)
        {
            if (formerId is not null && !string.Equals(deviceId, formerId, StringComparison.OrdinalIgnoreCase))
            {
                try { return Profile.Load(PathFor(formerId)); }
                catch (FileNotFoundException) { }
            }
            return new Profile();
        }
        catch (DirectoryNotFoundException) { return new Profile(); }
    }

    public void Save(string deviceId, Profile profile)
    {
        profile.Validate();
        Directory.CreateDirectory(directory);
        Write(PathFor(deviceId), profile);
    }

    private string PathFor(string deviceId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(deviceId.ToUpperInvariant()));
        return Path.Combine(directory, Convert.ToHexString(hash) + ".json");
    }

    public static void Write(string path, Profile profile)
    {
        profile.Validate();
        var target = Path.GetFullPath(path);
        var temporary = Path.Combine(Path.GetDirectoryName(target)!, Guid.NewGuid().ToString("N") + ".tmp");
        bool created = false;
        try
        {
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                created = true;
                JsonSerializer.Serialize(file, profile, JsonOptions);
                file.Flush(flushToDisk: true);
            }
            File.Move(temporary, target, overwrite: true);
            created = false;
        }
        finally
        {
            if (created) File.Delete(temporary);
        }
    }
}
