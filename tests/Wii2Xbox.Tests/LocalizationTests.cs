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
    [InlineData(0x0c0a, "es")] // Spanish (Spain)
    [InlineData(0x080a, "es")] // Spanish (Mexico)
    [InlineData(0x0409, "en")] // English (United States)
    [InlineData(0x0809, "en")] // English (United Kingdom)
    [InlineData(0x040c, "en")] // French
    public void AppFollowsWindowsDisplayLanguage(int displayLanguage, string expected)
    {
        var culture = UiLanguage.Resolve((ushort)displayLanguage);
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
                Assert.False(main.AutoScroll);
                Assert.False(layout.AutoScroll);
                var toolbar = Assert.Single(layout.Controls.OfType<FlowLayoutPanel>());
                var profileButton = Assert.Single(toolbar.Controls.OfType<Button>(), button => button.Text == UiText.Get("EditProfileButton"));
                Assert.False(profileButton.Enabled);
                var coffee = Assert.Single(layout.Controls.OfType<Button>(), button => button.Text == UiText.Get("CoffeeButton"));
                Assert.NotNull(coffee.Image);
                Assert.Equal(layout.GetRow(toolbar), layout.GetRow(coffee));
                var help = Assert.Single(layout.Controls.OfType<Button>(), button => button.AccessibleName == UiText.Get("TroubleshootingHelp"));
                Assert.Equal(0, layout.GetRow(help));
                Assert.Equal(1, layout.GetColumn(help));
                Assert.Equal(FlatStyle.Flat, help.FlatStyle);
                Assert.Equal(0, help.FlatAppearance.BorderSize);
                var options = Assert.Single(layout.Controls.OfType<TableLayoutPanel>());
                var version = Assert.Single(options.Controls.OfType<Label>(), label => label.Text.StartsWith("v"));
                Assert.Equal($"v{typeof(MainForm).Assembly.GetName().Version!.ToString(3)}", version.Text);
                var viewer = Assert.Single(layout.Controls.OfType<WiiRemoteView>());
                Assert.Equal(layout.RowCount - 1, layout.GetRow(viewer));
                Assert.All(layout.Controls.OfType<Label>().Cast<Control>().Append(options),
                    control => Assert.True(layout.GetRow(control) < layout.GetRow(viewer)));
                Assert.Equal(AnchorStyles.Right, version.Anchor);
                Assert.Empty(layout.Controls.OfType<FlowLayoutPanel>().SelectMany(panel => panel.Controls.OfType<ComboBox>()));
                Assert.DoesNotContain(layout.Controls.OfType<FlowLayoutPanel>().SelectMany(panel => panel.Controls.OfType<Button>()),
                    button => button.Text == "Install Xbox driver");
                Assert.Equal("Add controller", pairing.Text);
                var pairingLayout = Assert.Single(Assert.Single(pairing.Controls.OfType<ScrollableLayout>()).Controls.OfType<TableLayoutPanel>());
                Assert.Empty(pairingLayout.Controls.OfType<ComboBox>());
                Assert.Empty(pairingLayout.Controls.OfType<FlowLayoutPanel>().SelectMany(panel => panel.Controls.OfType<ComboBox>()));
                Assert.Contains(pairingLayout.Controls.OfType<Label>(), label => label.Text == UiText.Get("PairingInstructions"));
                Assert.Equal("Wii controller profile", profile.Text);
                var profileRoot = Assert.Single(Assert.Single(profile.Controls.OfType<ScrollableLayout>()).Controls.OfType<TableLayoutPanel>());
                var profileName = Assert.Single(profileRoot.Controls
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

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    public async Task MainAndPairingKeepTheirRowsSeparateAtMinimumSize(int fontPoints)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                using var font = new System.Drawing.Font("Segoe UI", fontPoints);
                using var main = new MainForm(static () => []);
                using var pairing = new PairingDialog();
                VerifyRows(main, font);
                VerifyRows(pairing, font);
                done.SetResult();
            }
            catch (Exception error) { done.SetException(error); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private static void VerifyRows(Form form, System.Drawing.Font font)
    {
        form.Font = font;
        var viewport = form.Controls.OfType<ScrollableLayout>().SingleOrDefault();
        var table = Assert.Single((viewport ?? (Control)form).Controls.OfType<TableLayoutPanel>());
        foreach (var label in table.Controls.OfType<Label>().Where(label => label.Text.Length > 0))
            label.Font = font;
        form.Size = new System.Drawing.Size(200, 150);
        Assert.Equal(form.MinimumSize, form.Size);
        form.CreateControl();
        form.PerformLayout();
        viewport?.PerformLayout();
        table.PerformLayout();
        if (viewport is null)
        {
            _ = form.Handle;
            Application.DoEvents();
            Assert.Equal(form.ClientSize, table.Size);
        }
        var rows = table.GetRowHeights();
        var list = Assert.Single(table.Controls.OfType<ListBox>().Cast<Control>().Concat(table.Controls.OfType<ListView>()));
        Assert.True(rows[table.GetRow(list)] >= 114);
        var occupied = table.Controls.Cast<Control>()
            .Where(control => control is not Label label || label.Text.Length != 0)
            .GroupBy(table.GetRow).OrderBy(row => row.Key).ToArray();
        for (int row = 1; row < occupied.Length; row++)
            Assert.True(occupied[row - 1].Max(control => control.Bottom) <= occupied[row].Min(control => control.Top),
                $"{form.GetType().Name} overlaps rows {occupied[row - 1].Key} and {occupied[row].Key} at {font.Size} pt.");
        foreach (var toolbar in table.Controls.OfType<FlowLayoutPanel>())
        {
            var controls = toolbar.Controls.Cast<Control>().ToArray();
            for (int index = 0; index < controls.Length; index++)
                Assert.DoesNotContain(controls.Skip(index + 1), control => control.Bounds.IntersectsWith(controls[index].Bounds));
        }
    }

    private static Dictionary<string, string> Catalog(ResourceManager manager, CultureInfo culture) =>
        manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!
            .Cast<DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);

    private static IEnumerable<int> Arguments(string message) =>
        Regex.Matches(message, @"\{(\d+)\}").Select(match => int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)).Order();
}
