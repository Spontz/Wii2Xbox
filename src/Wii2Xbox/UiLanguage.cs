using System.Globalization;
using System.Resources;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Wii2Xbox;

internal enum UiLanguageMode { System, Spanish, English }

internal static class UiLanguage
{
    private const string SettingsKey = @"Software\Wii2Xbox\Preferences";
    private const string ValueName = "Language";
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es");

    internal static CultureInfo Culture { get; private set; } = English;
    internal static UiLanguageMode Mode { get; private set; }

    internal static void Initialize()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(SettingsKey);
            Mode = Parse(key?.GetValue(ValueName) as string);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }

        Culture = Resolve(Mode, GetUserDefaultUILanguage());
    }

    internal static UiLanguageMode Parse(string? value) => value switch
    {
        "es" => UiLanguageMode.Spanish,
        "en" => UiLanguageMode.English,
        _ => UiLanguageMode.System
    };

    internal static CultureInfo Resolve(UiLanguageMode mode, ushort displayLanguage) => mode switch
    {
        UiLanguageMode.Spanish => Spanish,
        UiLanguageMode.English => English,
        _ => (displayLanguage & 0x03ff) == 0x0a ? Spanish : English
    };

    internal static void Save(UiLanguageMode mode)
    {
        using var key = Registry.CurrentUser.CreateSubKey(SettingsKey)
            ?? throw new IOException("Wii2Xbox preferences are unavailable.");
        key.SetValue(ValueName, mode switch
        {
            UiLanguageMode.Spanish => "es",
            UiLanguageMode.English => "en",
            _ => "system"
        });
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern ushort GetUserDefaultUILanguage();
}

internal static class UiText
{
    private static readonly ResourceManager Resources = new("Wii2Xbox.Resources.UiText", typeof(UiText).Assembly);

    internal static string Get(string key) => Get(key, UiLanguage.Culture);

    internal static string Get(string key, CultureInfo culture) => Resources.GetString(key, culture)
        ?? throw new InvalidOperationException($"Missing UI resource: {key}");

    internal static string Format(string key, params object?[] arguments) =>
        string.Format(UiLanguage.Culture, Get(key), arguments);
}
