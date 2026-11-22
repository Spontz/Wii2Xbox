using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Xunit;

namespace Wii2Xbox.Tests;

public sealed class LocalizationTests
{
    [Theory]
    [InlineData(null, 0x0c0a, "es")] // Spanish (Spain)
    [InlineData("system", 0x080a, "es")] // Spanish (Mexico)
    [InlineData("system", 0x0409, "en")] // English (United States)
    [InlineData("unknown", 0x0809, "en")] // English (United Kingdom)
    [InlineData("system", 0x040c, "en")] // French
    [InlineData("en", 0x0c0a, "en")]
    [InlineData("es", 0x0409, "es")]
    public void ExplicitPreferenceWinsOverWindowsDisplayLanguage(string? preference, int displayLanguage, string expected)
    {
        var culture = UiLanguage.Resolve(UiLanguage.Parse(preference), (ushort)displayLanguage);
        Assert.Equal(expected, culture.Name);
    }

    [Fact]
    public void BothCatalogsProvideTheSameKeysAndArguments()
    {
        var manager = new ResourceManager("Wii2Xbox.Resources.UiText", typeof(UiText).Assembly);
        var english = Catalog(manager, CultureInfo.InvariantCulture);
        var spanish = Catalog(manager, CultureInfo.GetCultureInfo("es"));
        Assert.NotEmpty(english);
        Assert.Equal(english.Keys.Order(), spanish.Keys.Order());
        foreach (var (key, text) in english)
        {
            Assert.False(string.IsNullOrWhiteSpace(text));
            Assert.False(string.IsNullOrWhiteSpace(spanish[key]));
            Assert.Equal(Arguments(text), Arguments(spanish[key]));
        }

        Assert.Equal("Add controller…", UiText.Get("AddControllerButton", CultureInfo.GetCultureInfo("en")));
        Assert.Equal("Añadir mando…", UiText.Get("AddControllerButton", CultureInfo.GetCultureInfo("es")));
        Assert.Equal("Controller output", UiText.Get("ControllerOutput", CultureInfo.GetCultureInfo("en")));
        Assert.Equal("Salida del mando", UiText.Get("ControllerOutput", CultureInfo.GetCultureInfo("es")));
    }

    [Fact]
    public async Task EnglishFormsExposeLocalizedControlsWithoutChangingWindowsPreferences()
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                using var main = new MainForm(static () => []);
                using var pairing = new PairingDialog();
                using var profile = new ProfileDialog(new Profile { Name = "Perfil personalizado" });
                var layout = Assert.Single(main.Controls.OfType<TableLayoutPanel>());
                var picker = Assert.Single(layout.Controls.OfType<FlowLayoutPanel>()
                    .SelectMany(panel => panel.Controls.OfType<ComboBox>()));
                Assert.Equal("Language", picker.AccessibleName);
                Assert.Equal(new[] { "System", "Español", "English" }, picker.Items.Cast<string>());
                Assert.Equal("Add controller", pairing.Text);
                Assert.Equal("Wii controller profile", profile.Text);
                var profileName = Assert.Single(profile.Controls.OfType<TableLayoutPanel>().Single().Controls
                    .OfType<TableLayoutPanel>().SelectMany(panel => panel.Controls.OfType<TextBox>()));
                Assert.Equal("Perfil personalizado", profileName.Text);
                done.SetResult();
            }
            catch (Exception error) { done.SetException(error); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private static Dictionary<string, string> Catalog(ResourceManager manager, CultureInfo culture) =>
        manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!
            .Cast<DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);

    private static IEnumerable<int> Arguments(string message) =>
        Regex.Matches(message, @"\{(\d+)\}").Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)).Order();
}
