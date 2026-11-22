using Microsoft.Win32;

namespace Wii2Xbox;

internal static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string PortableValueName = "Wii2Xbox";
    private const string InstalledValueName = "Wii2Xbox.Installed";
    private const string InstallerKey = @"Software\Wii2Xbox\Installer";
    internal const string BackgroundArgument = "--background";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(GetValueName(Environment.ProcessPath)) is string { Length: > 0 };
    }

    public static void SetEnabled(bool enabled)
    {
        string? executable = Environment.ProcessPath;
        string valueName = GetValueName(executable);
        if (!enabled)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (valueName == InstalledValueName)
                key?.SetValue(valueName, string.Empty, RegistryValueKind.String);
            else
                key?.DeleteValue(valueName, throwOnMissingValue: false);
            return;
        }

        if (executable is null)
            throw new IOException(UiText.Get("MissingExecutablePath"));
        string command = $"\"{executable}\" {BackgroundArgument}";
        if (Path.GetFileName(executable).Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase))
            command = $"\"{executable}\" \"{typeof(StartupRegistration).Assembly.Location}\" {BackgroundArgument}";
        if (command.Length > 260)
            throw new IOException(UiText.Get("StartupPathTooLong"));

        using var startup = Registry.CurrentUser.CreateSubKey(RunKey)
            ?? throw new IOException(UiText.Get("StartupWriteDenied"));
        startup.SetValue(valueName, command, RegistryValueKind.String);
    }

    private static string GetValueName(string? executable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(InstallerKey);
        return executable is not null &&
            string.Equals(executable, key?.GetValue("ExecutablePath") as string, StringComparison.OrdinalIgnoreCase)
            ? InstalledValueName
            : PortableValueName;
    }
}
