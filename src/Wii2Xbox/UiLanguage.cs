using System.Globalization;
using System.Resources;
using System.Runtime.InteropServices;

namespace Wii2Xbox;

internal static class UiLanguage
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es");

    internal static CultureInfo Culture { get; private set; } = English;
    internal static void Initialize() => Culture = Resolve(GetUserDefaultUILanguage());

    internal static CultureInfo Resolve(ushort displayLanguage) =>
        (displayLanguage & 0x03ff) == 0x0a ? Spanish : English;

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
