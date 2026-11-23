namespace Wii2Xbox;

internal sealed class ProfileDialog : Form
{
    private const string Automatic = "Automatic";
    private static readonly Size MinimumWindow = new(560, 365);
    private readonly TextBox name = new() { Dock = DockStyle.Fill, MaxLength = 80, AccessibleName = UiText.Get("ProfileName") };
    private readonly ComboBox orientation = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, AccessibleName = UiText.Get("Orientation") };
    private readonly Dictionary<string, ComboBox> bindings = new();
    private InfraredCalibration? infraredCalibration;
    private readonly NumericUpDown deadzone = new() { Minimum = 0, Maximum = 60, Width = 60, AccessibleName = UiText.Get("NunchukDeadZone") };
    public Profile? Result { get; private set; }

    public ProfileDialog(Profile profile)
    {
        Text = UiText.Get("ControllerProfile");
        ScrollableLayout.ConfigureForm(this, new Size(615, 375), MinimumWindow);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12), ColumnCount = 1
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(new Label { Text = UiText.Get("ProfileName"), AutoSize = true });
        var heading = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        orientation.Items.AddRange(["Horizontal", UiText.Get("Upright")]);
        ScrollableLayout.FitChoices(orientation, 120);
        heading.Controls.Add(name);
        heading.Controls.Add(orientation);
        root.Controls.Add(heading);
        var remote = AddMappingGroup("Wii Remote", Profile.Sources.Where(source => source is not ("C" or "Z")).ToArray());
        var nunchuk = AddMappingGroup("Nunchuk", ["C", "Z"]);
        var stick = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill };
        stick.Controls.Add(new Label { Text = UiText.Get("DeadZone"), AutoSize = true, Margin = new Padding(3, 7, 0, 0) });
        stick.Controls.Add(deadzone);
        nunchuk.Controls.Add(stick, 0, 1);
        nunchuk.SetColumnSpan(stick, 4);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        actions.Controls.Add(Action(UiText.Get("Import"), Import));
        actions.Controls.Add(Action(UiText.Get("Export"), Export));
        actions.Controls.Add(Action(UiText.Get("Save"), Save));
        root.Controls.Add(actions);
        Controls.Add(root);
        root.SizeChanged += (_, _) => FitContent();
        Load += (_, _) => FitContent();
        ShowProfile(profile);

        TableLayoutPanel AddMappingGroup(string title, string[] sources)
        {
            var group = new GroupBox { Text = title, Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var mapping = new TableLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 4
            };
            mapping.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            mapping.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            mapping.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            mapping.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            int rows = (sources.Length + 1) / 2;
            for (int index = 0; index < sources.Length; index++)
                AddBinding(mapping, sources[index], index / rows * 2, index % rows);
            group.Controls.Add(mapping);
            root.Controls.Add(group);
            return mapping;
        }

        void FitContent()
        {
            var area = Screen.FromControl(this).WorkingArea;
            var baseline = LogicalToDeviceUnits(MinimumWindow);
            int bindingWidth = bindings.Values.First().Items.Cast<string>().Append(Automatic)
                .Max(target => TextRenderer.MeasureText(BindingLabel(target), Font).Width) + LogicalToDeviceUnits(30);
            int labelWidth = remote.Controls.OfType<Label>().GroupBy(remote.GetColumn)
                .Sum(column => column.Max(label => label.PreferredWidth + label.Margin.Horizontal));
            int width = Math.Max(baseline.Width, labelWidth + 2 * bindingWidth + LogicalToDeviceUnits(50) + Width - ClientSize.Width);
            int height = Math.Max(baseline.Height, root.Height + Height - ClientSize.Height);
            var minimum = new Size(Math.Min(area.Width, width), Math.Min(area.Height, height));
            if (MinimumSize != minimum) MinimumSize = minimum;
            foreach (var box in bindings.Values) box.DropDownWidth = Math.Min(area.Width, bindingWidth);
        }
    }

    private void AddBinding(TableLayoutPanel mapping, string source, int column, int row)
    {
        string label = source switch
        {
            "Left" => UiText.Get("DpadLeft"), "Right" => UiText.Get("DpadRight"), "Up" => UiText.Get("DpadUp"), "Down" => UiText.Get("DpadDown"),
            "One" => "1", "Two" => "2", "Plus" => "+", "Minus" => "−", _ => source
        };
        var box = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, AccessibleName = label, FormattingEnabled = true
        };
        box.Format += (_, args) =>
        {
            if (args.ListItem is string target) args.Value = BindingLabel(target);
        };
        if (IsDirection(source)) box.Items.Add(Automatic);
        box.Items.AddRange(["None", "LeftTrigger", "RightTrigger", .. Profile.XboxButtons.Keys]);
        bindings.Add(source, box);
        mapping.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, column, row);
        mapping.Controls.Add(box, column + 1, row);
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData != Keys.Escape) return base.ProcessDialogKey(keyData);
        DialogResult = DialogResult.Cancel;
        return true;
    }

    private static Button Action(string text, Action handler)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += (_, _) => handler();
        return button;
    }

    private static bool IsDirection(string source) => source is "Up" or "Down" or "Left" or "Right";

    private static string BindingLabel(string target) => target switch
    {
        Automatic => UiText.Get("AutomaticMapping"),
        "None" => UiText.Get("DisabledBinding"),
        "LeftTrigger" => UiText.Get("LeftTriggerBinding"),
        "RightTrigger" => UiText.Get("RightTriggerBinding"),
        "LeftThumb" => UiText.Get("LeftStickBinding"),
        "RightThumb" => UiText.Get("RightStickBinding"),
        "LeftShoulder" => UiText.Get("LeftShoulderBinding"),
        "RightShoulder" => UiText.Get("RightShoulderBinding"),
        "Up" => UiText.Get("DpadUp"),
        "Down" => UiText.Get("DpadDown"),
        "Left" => UiText.Get("DpadLeft"),
        "Right" => UiText.Get("DpadRight"),
        _ => target
    };

    private void ShowProfile(Profile profile)
    {
        infraredCalibration = profile.InfraredCalibration;
        name.Text = profile.Name;
        deadzone.Value = profile.Deadzone;
        orientation.SelectedIndex = profile.Horizontal ? 0 : 1;
        foreach (var (source, box) in bindings)
            box.SelectedItem = profile.Bindings.TryGetValue(source, out var value) ? value : IsDirection(source) ? Automatic : "None";
    }

    private Profile EditedProfile()
    {
        var profile = new Profile
        {
            Name = name.Text.Trim(),
            Horizontal = orientation.SelectedIndex == 0,
            Deadzone = (int)deadzone.Value,
            InfraredCalibration = infraredCalibration,
            Bindings = bindings.Where(entry => (string?)entry.Value.SelectedItem != Automatic)
                .ToDictionary(entry => entry.Key, entry => (string)entry.Value.SelectedItem!)
        };
        profile.Validate();
        return profile;
    }

    private void Save() => TryEdit(() =>
    {
        Result = EditedProfile();
        DialogResult = DialogResult.OK;
    });

    private void Import() => TryEdit(() =>
    {
        using var file = new OpenFileDialog { Filter = UiText.Get("ProfileFileFilter") };
        if (file.ShowDialog(this) == DialogResult.OK) ShowProfile(Profile.Load(file.FileName));
    });

    private void Export() => TryEdit(() =>
    {
        var profile = EditedProfile();
        using var file = new SaveFileDialog { Filter = UiText.Get("ProfileFileFilter"), FileName = "Wii2Xbox.json" };
        if (file.ShowDialog(this) == DialogResult.OK) profile.Save(file.FileName);
    });

    private void TryEdit(Action action)
    {
        try { action(); }
        catch (Exception error) { MessageBox.Show(this, error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
