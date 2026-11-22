using System.Diagnostics;
using System.Security.Cryptography;

namespace Wii2Xbox;

internal static class VigemBusSetup
{
    private const string ResourceName = "Wii2Xbox.ViGEmBusSetup";
    private const string InstallerHash = "89220A7865076B342892F98865F3499FB7C4CFD673159E89D352C360FD014C6A";

    public static bool IsBundled => typeof(VigemBusSetup).Assembly.GetManifestResourceInfo(ResourceName) is not null;

    public static async Task<int> Install()
    {
        using var payload = typeof(VigemBusSetup).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new IOException("Esta compilación no incluye el instalador de ViGEmBus.");
        var folder = Directory.CreateTempSubdirectory("Wii2Xbox-");
        var path = Path.Combine(folder.FullName, "ViGEmBusSetup.exe");
        try
        {
            using (var extracted = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                payload.CopyTo(extracted);

            // Keep the verified file open while it runs so it cannot be replaced before elevation.
            using var installer = File.OpenRead(path);
            if (Convert.ToHexString(SHA256.HashData(installer)) != InstallerHash)
                throw new IOException("El instalador integrado de ViGEmBus no coincide con la copia oficial.");

            using var process = Process.Start(new ProcessStartInfo(path, "/exenoui /qn /norestart")
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = folder.FullName
            }) ?? throw new IOException("Windows no pudo abrir el instalador de ViGEmBus.");

            await process.WaitForExitAsync().ConfigureAwait(false);
            return process.ExitCode;
        }
        finally
        {
            try
            {
                File.Delete(path);
                folder.Delete();
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine(error);
            }
        }
    }
}
