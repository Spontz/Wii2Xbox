namespace Wii2Xbox;

public sealed class MainForm : Form
{
    private const int OutputColumn = 4;
    private const int DeviceListRow = 2;
    private const int StartupRow = 3;
    private const int RemoteRow = 6;
    private const int RemoteHeight = 165;
    private const int DeviceListHeight = 114;
    private static readonly Size MinimumWindow = new(690, 450);
    private readonly string[] outputLabels = ["Xbox", UiText.Get("OutputIr"), UiText.Get("OutputCombined")];
    private readonly ListView devices = new()
    {
        Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false
    };
    private readonly Dictionary<string, ControllerSession> sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SessionRecovery> recovery = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, WiiDevice> discovered = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SessionMode> outputModes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> unavailableXboxOutputs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<WiiDevice[]> enumerateDevices;
    private readonly Icon appIcon;
    private readonly System.Windows.Forms.Timer display = new() { Interval = 200 };
    private readonly System.Windows.Forms.Timer buttonDisplay = new() { Interval = 33 };
    private readonly WiiRemoteView remoteView = new() { Dock = DockStyle.Fill };
    private readonly ProfileStore profileStore = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wii2Xbox", "profiles"));
    private readonly Dictionary<string, CoreInputMap> profileMaps = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Profile> profiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Label info = new() { AutoSize = true, Visible = false, Padding = new Padding(0, 8, 0, 0) };
    private readonly Label xboxStatus = new() { AutoSize = true, Visible = false, Padding = new Padding(0, 4, 0, 0) };
    private readonly CheckBox startup = new()
    {
        Text = UiText.Get("StartWithWindows"), AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 0, 0)
    };
    private readonly ContextMenuStrip outputMenu = new()
    {
        AccessibleName = UiText.Get("ControllerOutput"), ShowImageMargin = false, ShowCheckMargin = true
    };
    private string? outputMenuDevice;
    private bool closing;
    private bool canClose;
    private bool suspended;
    private bool finding;
    private bool recovering;
    private bool deviceRefreshRequested;
    private bool resizingColumns;
    private bool pairingTransition;
    private bool installingXbox;
    private bool driverOfferPending;
    private bool xboxChecked;
    private bool initialized;
    private bool exitRequested;
    private readonly AppUpdater? updater;
    private readonly string? updateResult;
    private readonly CancellationTokenSource updateCancellation = new();
    private readonly System.Windows.Forms.Timer updateTimer = new() { Interval = 30000 };
    private DateTime nextUpdateCheck;
    private bool checkingUpdate;
    private bool updateHandoff;
    private bool modalOperation;
    private StagedUpdate? stagedUpdate;
    private ToolStripMenuItem? restartUpdate;
    private NotifyIcon? trayIcon;
    private Icon? trayImage;
    internal bool XboxReady { get; private set; }

    public MainForm() : this(WiiDevice.Find) { }

    internal MainForm(Func<WiiDevice[]> enumerateDevices, AppUpdater? updater = null, string? updateResult = null)
    {
        this.enumerateDevices = enumerateDevices;
        this.updater = updater;
        this.updateResult = updateResult;
        nextUpdateCheck = updateResult is null ? DateTime.MinValue : DateTime.UtcNow.AddHours(6);
        updateTimer.Tick += async (_, _) => await CheckForUpdates();
        using (var icon = typeof(MainForm).Assembly.GetManifestResourceStream("Wii2Xbox.AppIcon")
            ?? throw new InvalidOperationException(UiText.Get("MissingAppIcon")))
            appIcon = new Icon(icon);
        Text = "Wii2Xbox";
        Icon = appIcon;
        ScrollableLayout.ConfigureForm(this, new Size(850, 520), MinimumWindow);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        bool fitPending = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 7 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteHeight));
        layout.Controls.Add(new Label
        {
            Text = UiText.Get("WiiControllers"), Font = new Font("Segoe UI", 16, FontStyle.Bold),
            Dock = DockStyle.Fill, AutoEllipsis = true, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        string helpText = UiText.Get("TroubleshootingHelp");
        var help = new HelpIconButton
        {
            AccessibleName = helpText, Anchor = AnchorStyles.Top | AnchorStyles.Right, Margin = new Padding(3, 3, 6, 3)
        };
        help.Click += (_, _) => OpenWebPage(
            "https://github.com/Spontz/Wii2Xbox/blob/main/docs/troubleshooting.md", "TroubleshootingPageFailed");
        help.FontChanged += (_, _) => SizeHelpIcon();
        help.DpiChangedAfterParent += (_, _) => SizeHelpIcon();
        SizeHelpIcon();
        var helpTip = new ToolTip();
        helpTip.SetToolTip(help, helpText);
        help.Disposed += (_, _) => helpTip.Dispose();
        layout.Controls.Add(help, 1, 0);
        var actions = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill };
        actions.Controls.Add(Action(UiText.Get("AddControllerButton"), () => _ = PairDevice()));
        var profile = Action(UiText.Get("EditProfileButton"), EditProfile);
        profile.Enabled = false;
        devices.SelectedIndexChanged += (_, _) => profile.Enabled = SelectedId is not null;
        actions.Controls.Add(profile);
        actions.Controls.Add(Action(UiText.Get("CalibrateIrButton"), CalibrateInfrared));
        layout.Controls.Add(actions, 0, 1);
        var support = Action(UiText.Get("CoffeeButton"), () => OpenWebPage(
            "https://ko-fi.com/ivanfernandezgarcia", "SupportPageFailed"));
        support.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        // Match the first button's inset inside the neighboring flow panel.
        support.Margin = new Padding(3, 6, 6, 3);
        support.TextImageRelation = TextImageRelation.ImageBeforeText;
        support.FontChanged += (_, _) => RefreshCoffeeIcon(support);
        support.DpiChangedAfterParent += (_, _) => RefreshCoffeeIcon(support);
        support.Disposed += (_, _) => support.Image?.Dispose();
        RefreshCoffeeIcon(support);
        layout.Controls.Add(support, 1, 1);
        startup.Click += (_, _) => UpdateStartup();
        info.TextChanged += (_, _) => info.Visible = info.Text.Length != 0;
        xboxStatus.TextChanged += (_, _) => xboxStatus.Visible = xboxStatus.Text.Length != 0;
        devices.Columns.Add(UiText.Get("ControllerColumn"));
        devices.Columns.Add("Xbox");
        devices.Columns.Add(UiText.Get("StatusColumn"));
        devices.Columns.Add(UiText.Get("ProfileColumn"));
        devices.Columns.Add(UiText.Get("OutputColumn"));
        for (int index = 0; index < outputLabels.Length; index++)
        {
            var mode = (SessionMode)index;
            outputMenu.Items.Add(outputLabels[index], null, (_, _) =>
            {
                if (outputMenuDevice is { } id) SetOutputMode(id, mode);
            });
        }
        devices.MouseClick += (_, args) =>
        {
            var hit = devices.HitTest(args.Location);
            if (args.Button == MouseButtons.Left && hit.Item is { } row && hit.SubItem == row.SubItems[OutputColumn])
                ShowOutputMenu(row);
        };
        devices.KeyDown += (_, args) =>
        {
            if (args.KeyCode != Keys.F4 || devices.SelectedItems.Count == 0) return;
            args.Handled = true;
            ShowOutputMenu(devices.SelectedItems[0]);
        };
        devices.ClientSizeChanged += (_, _) => QueueFit();
        devices.HandleCreated += (_, _) => QueueFit();
        devices.FontChanged += (_, _) => QueueFit();
        devices.ColumnWidthChanging += (_, args) => args.Cancel = !resizingColumns;
        AddFullWidth(devices, DeviceListRow);
        var options = new TableLayoutPanel
        {
            AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty, ColumnCount = 2, RowCount = 1
        };
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        options.Controls.Add(startup, 0, 0);
        AddFullWidth(options, StartupRow);
        AddFullWidth(info, 4);
        AddFullWidth(xboxStatus, 5);
        AddFullWidth(remoteView, RemoteRow);
        var version = typeof(MainForm).Assembly.GetName().Version!;
        options.Controls.Add(new Label
        {
            Text = $"v{version.ToString(3)}", AutoSize = true, Anchor = AnchorStyles.Right,
            Margin = new Padding(3, 8, 6, 0)
        }, 1, 0);
        Controls.Add(layout);
        layout.Layout += (_, _) => QueueFit();
        HandleCreated += (_, _) => QueueFit();
        Load += (_, _) => QueueFit();
        DpiChanged += (_, _) => QueueFit();
        display.Tick += async (_, _) =>
        {
            StartAvailableSessions();
            if (Visible) ShowSessions();
            await RecoverSessions();
        };
        display.Start();
        buttonDisplay.Tick += (_, _) =>
        {
            if (WindowState != FormWindowState.Minimized) remoteView.UpdateButtons(sessions.Values);
        };
        VisibleChanged += (_, _) =>
        {
            buttonDisplay.Enabled = Visible && !closing;
            if (buttonDisplay.Enabled) remoteView.UpdateButtons(sessions.Values);
        };
        Shown += async (_, _) => await InitializeOnce();
        FormClosing += CloseSessions;

        void SizeHelpIcon()
        {
            int size = help.Font.Height + help.LogicalToDeviceUnits(8);
            help.Size = new Size(size, size);
        }

        void AddFullWidth(Control control, int row)
        {
            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 2);
        }

        void QueueFit()
        {
            if (fitPending || !IsHandleCreated || Disposing || IsDisposed) return;
            fitPending = true;
            BeginInvoke(() =>
            {
                fitPending = false;
                if (!IsDisposed) FitMainLayout(layout, actions, support);
            });
        }
    }

    private void FitMainLayout(TableLayoutPanel layout, FlowLayoutPanel actions, Button support)
    {
        int wrapWidth = layout.ClientSize.Width - layout.Padding.Horizontal;
        info.MaximumSize = new Size(Math.Max(1, wrapWidth - info.Margin.Horizontal), 0);
        xboxStatus.MaximumSize = new Size(Math.Max(1, wrapWidth - xboxStatus.Margin.Horizontal), 0);

        int[] rows = layout.GetRowHeights();
        int fixedHeight = layout.Padding.Vertical + rows.Sum() - rows[DeviceListRow] - rows[RemoteRow];
        int minList = LogicalToDeviceUnits(DeviceListHeight);
        int preferredRemote = LogicalToDeviceUnits(RemoteHeight);
        int widestAction = actions.Controls.Cast<Control>().Max(button => button.PreferredSize.Width + button.Margin.Horizontal);
        var baseline = LogicalToDeviceUnits(MinimumWindow);
        var area = Screen.FromControl(this).WorkingArea;
        int minWidth = layout.Padding.Horizontal + actions.Margin.Horizontal + widestAction
            + support.PreferredSize.Width + support.Margin.Horizontal + Width - ClientSize.Width;
        int minHeight = fixedHeight + minList + preferredRemote + Height - ClientSize.Height;
        var minimum = new Size(Math.Min(area.Width, Math.Max(baseline.Width, minWidth)),
            Math.Min(area.Height, Math.Max(baseline.Height, minHeight)));
        if (MinimumSize != minimum) MinimumSize = minimum;

        int viewerHeight = Math.Clamp(layout.ClientSize.Height - fixedHeight - minList, 0, preferredRemote);
        if (layout.RowStyles[RemoteRow].Height != viewerHeight)
            layout.RowStyles[RemoteRow].Height = viewerHeight;
        ResizeDeviceColumns();
    }

    internal void StartResident(bool background)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(UiText.Get("OpenApp"), null, (_, _) => RestoreWindow());
        if (updater is not null)
        {
            restartUpdate = new ToolStripMenuItem(UiText.Get("RestartToUpdate")) { Available = false };
            restartUpdate.Click += async (_, _) => await ApplyStagedUpdate(explicitRestart: true);
            menu.Items.Add(restartUpdate);
        }
        menu.Items.Add(UiText.Get("Exit"), null, (_, _) =>
        {
            if (installingXbox)
            {
                RestoreWindow();
                xboxStatus.Text = UiText.Get("WaitUntilDriverInstalled");
                return;
            }
            exitRequested = true;
            try { Close(); }
            finally { exitRequested = false; }
        });
        trayImage = new Icon(appIcon, SystemInformation.SmallIconSize);
        trayIcon = new NotifyIcon
        {
            Text = "Wii2Xbox", Icon = trayImage, ContextMenuStrip = menu, Visible = true
        };
        trayIcon.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left) RestoreWindow();
        };
        trayIcon.BalloonTipClicked += (_, _) => RestoreWindow();
        if (!background) Show();
        else
        {
            _ = Handle;
            BeginInvoke(new Action(async () => await InitializeOnce()));
        }
    }

    internal void RestoreWindow()
    {
        if (closing || IsDisposed) return;
        Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        ShowSessions();
        Activate();
        _ = OfferXboxDriver();
    }

    private async Task InitializeOnce()
    {
        if (initialized) return;
        initialized = true;
        string? startupError = null;
        try { startup.Checked = StartupRegistration.IsEnabled(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            startup.Enabled = false;
            startupError = UiText.Get("StartupReadFailed");
        }
        await Task.WhenAll(FindDevices(), CheckXbox());
        if (startupError is not null && !closing && !IsDisposed) info.Text = startupError;
        if (closing || IsDisposed) return;
        if (updateResult is "failed" or "restart-required")
        {
            string message = UiText.Get(updateResult == "failed" ? "UpdateFailed" : "UpdateRestartRequired");
            info.Text = message;
            if (!Visible) trayIcon?.ShowBalloonTip(5000, "Wii2Xbox", message, ToolTipIcon.Warning);
        }
        if (updater is not null)
        {
            updateTimer.Start();
            _ = CheckForUpdates();
        }
    }

    private async Task CheckForUpdates()
    {
        if (updater is null || closing || checkingUpdate || updateHandoff) return;
        if (stagedUpdate is not null)
        {
            await ApplyStagedUpdate(explicitRestart: false);
            return;
        }
        if (DateTime.UtcNow < nextUpdateCheck) return;

        checkingUpdate = true;
        nextUpdateCheck = DateTime.UtcNow.AddHours(6);
        try
        {
            var staged = await updater.CheckAndStage(updateCancellation.Token);
            if (staged is null) return;
            if (closing || IsDisposed)
            {
                AppUpdater.RemoveStage(staged.Directory);
                return;
            }
            stagedUpdate = staged;
            if (restartUpdate is not null) restartUpdate.Available = true;
            await ApplyStagedUpdate(explicitRestart: false);
        }
        catch (Exception error) when (error is HttpRequestException or IOException or UnauthorizedAccessException
                                         or OperationCanceledException or System.Text.Json.JsonException)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }
        finally { checkingUpdate = false; }
    }

    private async Task ApplyStagedUpdate(bool explicitRestart)
    {
        if (updater is null || stagedUpdate is not { } staged || updateHandoff || UpdateBlocked(explicitRestart))
            return;

        updateHandoff = true;
        try
        {
            using var handoff = await updater.PrepareHandoff(staged, !Visible || WindowState == FormWindowState.Minimized);
            if (handoff is null) throw new IOException("The update helper did not start.");
            if (UpdateBlocked(explicitRestart) || IsDisposed) return;
            handoff.Begin();
            stagedUpdate = null; // The helper owns the verified package from here.
            exitRequested = true;
            Close();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
                                     System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            System.Diagnostics.Debug.WriteLine(error);
            AppUpdater.RemoveStage(staged.Directory);
            stagedUpdate = null;
            if (restartUpdate is not null) restartUpdate.Available = false;
            if (!closing) info.Text = UiText.Get("UpdateFailed");
        }
        finally { updateHandoff = false; }
    }

    private bool UpdateBlocked(bool explicitRestart) =>
        closing || suspended || installingXbox || modalOperation || pairingTransition || finding || recovering ||
        !explicitRestart && sessions.Values.Any(session => !session.Completion.IsCompleted);

    private DialogResult ShowOwnedDialog(Form dialog)
    {
        modalOperation = true;
        try { return dialog.ShowDialog(this); }
        finally { modalOperation = false; }
    }

    private void UpdateStartup()
    {
        try { StartupRegistration.SetEnabled(startup.Checked); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            startup.Checked = !startup.Checked;
            info.Text = UiText.Format("StartupChangeFailed", error.Message);
        }
    }

    private async Task CheckXbox(string? installationResult = null, bool restartRequired = false)
    {
        var status = await Task.Run(() => XboxOutput.CheckDriver());
        if (closing || IsDisposed) return;
        xboxChecked = true;
        XboxReady = status.Ready;
        if (status.Ready) unavailableXboxOutputs.Clear();
        driverOfferPending = installationResult is null && status.CanInstall && VigemBusSetup.IsBundled && !restartRequired;
        string message = status.Message;
        xboxStatus.Text = installationResult is null ? (status.Ready ? "" : message) : $"{installationResult} {message}";
        if (!status.Ready && !Visible && installationResult is null)
            trayIcon?.ShowBalloonTip(5000, "Wii2Xbox", message, ToolTipIcon.Warning);
        StartAvailableSessions();
        await OfferXboxDriver();
    }

    private async Task OfferXboxDriver()
    {
        if (!driverOfferPending || !Visible || closing || IsDisposed) return;
        driverOfferPending = false;
        string question = $"{xboxStatus.Text}{Environment.NewLine}{Environment.NewLine}{UiText.Get("DriverInstallQuestion")}";
        DialogResult answer;
        modalOperation = true;
        try
        {
            answer = MessageBox.Show(this, question, "Wii2Xbox", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
        }
        finally { modalOperation = false; }
        if (answer == DialogResult.Yes)
            await InstallXbox();
    }

    private async Task InstallXbox()
    {
        installingXbox = true;
        xboxStatus.Text = UiText.Get("InstallingDriver");
        string result;
        bool restartRequired = false;
        try
        {
            int code = await Task.Run(VigemBusSetup.Install);
            restartRequired = code is 1641 or 3010;
            result = code switch
            {
                0 => UiText.Get("DriverInstallerFinished"),
                1641 or 3010 => UiText.Get("DriverRestartNeeded"),
                1602 => UiText.Get("InstallationCanceled"),
                _ => UiText.Format("DriverInstallerExitCode", code)
            };
        }
        catch (System.ComponentModel.Win32Exception error) when (error.NativeErrorCode == 1223)
        {
            result = UiText.Get("InstallationCanceled");
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            result = UiText.Format("DriverInstallerLaunchFailed", error.Message);
        }
        finally { installingXbox = false; }

        if (!closing && !IsDisposed) await CheckXbox(result, restartRequired);
    }

    private static Button Action(string label, Action handler)
    {
        var button = new Button { Text = label, AutoSize = true };
        button.Click += (_, _) => handler();
        return button;
    }

    private sealed class HelpIconButton : Button
    {
        public HelpIconButton()
        {
            Text = "?";
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int inset = LogicalToDeviceUnits(3);
            var circle = Rectangle.Inflate(ClientRectangle, -inset, -inset);
            using var outline = new Pen(ForeColor, LogicalToDeviceUnits(Focused && ShowFocusCues ? 2 : 1));
            e.Graphics.DrawEllipse(outline, circle);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    private static void RefreshCoffeeIcon(Button button)
    {
        int size = button.Font.Height;
        var icon = new Bitmap(size, size);
        using (var canvas = Graphics.FromImage(icon))
        using (var pen = new Pen(!SystemInformation.HighContrast && Application.IsDarkModeEnabled
            ? Color.White : SystemColors.ControlText, 1.8f))
        {
            canvas.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            canvas.ScaleTransform(size / 24f, size / 24f);
            pen.StartCap = pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
            canvas.DrawLines(pen, [new Point(3, 8), new Point(16, 8), new Point(16, 14),
                new Point(13, 18), new Point(6, 18), new Point(3, 14), new Point(3, 8)]);
            canvas.DrawArc(pen, 13, 9, 8, 7, -90, 180);
            canvas.DrawLine(pen, 2, 21, 19, 21);
            canvas.DrawLine(pen, 7, 5, 7, 2);
            canvas.DrawLine(pen, 12, 5, 12, 2);
        }
        var previous = button.Image;
        button.Image = icon;
        previous?.Dispose();
        button.Padding = new Padding(button.LogicalToDeviceUnits(6), 0, button.LogicalToDeviceUnits(6), 0);
    }

    private void OpenWebPage(string url, string failureMessage)
    {
        try
        {
            using var browser = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            info.Text = UiText.Get(failureMessage);
        }
    }

    private async Task FindDevices(bool announce = true)
    {
        if (finding || suspended || closing) return;
        finding = true;
        if (announce) info.Text = UiText.Get("SearchingPaired");
        try
        {
            var found = await Task.Run(enumerateDevices);
            if (closing || suspended) return;
            var selected = SelectedId;
            foreach (var device in found)
            {
                if (!discovered.ContainsKey(device.Id) &&
                    sessions.TryGetValue(device.Id, out var old) && old.Completion.IsCompleted)
                {
                    recovery.Remove(device.Id);
                    unavailableXboxOutputs.Remove(device.Id);
                }
            }
            discovered.Clear();
            foreach (var device in found) discovered.Add(device.Id, device);
            foreach (var row in devices.Items.Cast<ListViewItem>().ToArray())
            {
                if (row.Tag is not string id || !discovered.ContainsKey(id) && !sessions.ContainsKey(id))
                    devices.Items.Remove(row);
            }
            foreach (var device in found)
            {
                if (devices.Items.ContainsKey(device.Id)) continue;
                string name = device.BluetoothAddress is ulong address
                    ? $"{device.Model} · {address & 0xffff:X4}" : device.Model;
                var row = AddDeviceRow(device.Id, name);
                row.Selected = device.Id == selected;
            }
            if (devices.SelectedItems.Count == 0 && devices.Items.Count > 0) devices.Items[0].Selected = true;
            StartAvailableSessions();
            if (announce) info.Text = "";
        }
        catch (Exception error)
        {
            discovered.Clear();
            if (announce && !closing) info.Text = error.Message;
        }
        finally
        {
            finding = false;
        }
    }

    private async Task PairDevice()
    {
        if (pairingTransition) return;
        pairingTransition = true;
        try { await PairAndSelectDevice(); }
        finally { pairingTransition = false; }
    }

    private async Task PairAndSelectDevice()
    {
        using var dialog = new PairingDialog();
        if (ShowOwnedDialog(dialog) != DialogResult.OK || dialog.PairedAddress is not ulong address) return;
        info.Text = "";
        string id = $"bluetooth:{address:X12}";
        long deadline = Environment.TickCount64 + 5000;
        while (!closing && !suspended)
        {
            await FindDevices(announce: false);
            if (closing || suspended) return;
            if (discovered.ContainsKey(id) && devices.Items[id] is { } row)
            {
                row.Selected = true;
                row.Focused = true;
                row.EnsureVisible();
                return;
            }
            if (Environment.TickCount64 >= deadline) break;
            await Task.Delay(300);
        }
    }

    private string? SelectedId => devices.SelectedItems.Count == 0 ? null : devices.SelectedItems[0].Tag as string;

    private SessionMode GetOutputMode(string id) => outputModes.GetValueOrDefault(id, SessionMode.Xbox);

    private void ShowOutputMenu(ListViewItem row)
    {
        if (closing || row.Tag is not string id) return;
        outputMenuDevice = id;
        var current = GetOutputMode(id);
        for (int index = 0; index < outputMenu.Items.Count; index++)
            ((ToolStripMenuItem)outputMenu.Items[index]).Checked = index == (int)current;

        var cell = row.SubItems[OutputColumn].Bounds;
        outputMenu.Show(devices, new Point(cell.Left, cell.Bottom));
    }

    private void SetOutputMode(string id, SessionMode mode)
    {
        if (closing || devices.Items[id] is not { } row || GetOutputMode(id) == mode) return;
        outputModes[id] = mode;
        row.SubItems[OutputColumn].Text = $"{outputLabels[(int)mode]}  ▾";
        recovery.Remove(id);
        unavailableXboxOutputs.Remove(id);
        if (sessions.TryGetValue(id, out var session) && !session.Completion.IsCompleted)
            _ = Task.Run(session.Stop);
        StartAvailableSessions();
        if (mode is SessionMode.Mouse or SessionMode.XboxAndMouse)
            info.Text = UiText.Get("IrControlsHint");
    }

    private void StartAvailableSessions()
    {
        if (closing || suspended || updateHandoff) return;
        bool promoting = false;
        foreach (var (id, device) in discovered)
        {
            var desired = GetOutputMode(id);
            if (!xboxChecked && desired != SessionMode.Mouse) continue;

            sessions.TryGetValue(id, out var previous);
            if (recovery.TryGetValue(id, out var current))
            {
                if (previous is { Completion.IsCompleted: true, XboxOutputUnavailable: true })
                {
                    unavailableXboxOutputs.Add(id);
                    recovery.Remove(id);
                }
                else
                {
                    if (!promoting && current.Mode == SessionMode.Buttons &&
                        previous is { Completion.IsCompleted: false } &&
                        !unavailableXboxOutputs.Contains(id) && HasCapacity(desired))
                    {
                        recovery.Remove(id);
                        _ = Task.Run(previous.Stop);
                        promoting = true;
                    }
                    continue;
                }
            }
            if (previous is { Completion.IsCompleted: false }) continue;

            var mode = !unavailableXboxOutputs.Contains(id) && HasCapacity(desired) ? desired : SessionMode.Buttons;
            var plan = new SessionRecovery(mode);
            recovery.Add(id, plan);
            StartSession(id, device, plan);
        }
    }

    private void StartSession(string id, WiiDevice device, SessionRecovery plan)
    {
        var profile = GetProfile(id);
        if (!profileMaps.TryGetValue(id, out var map))
            profileMaps[id] = map = new CoreInputMap(profile);
        TrackSession(id, new ControllerSession(device, map, mode: plan.Mode));
        plan.SessionStarted();
    }

    internal void TrackSession(string id, ControllerSession session)
    {
        if (!devices.Items.ContainsKey(id)) AddDeviceRow(id, session.Model);
        sessions[id] = session;
    }

    private ListViewItem AddDeviceRow(string id, string name)
    {
        var row = new ListViewItem(new[] { name, "—", UiText.Get("Available"), "", $"{outputLabels[(int)GetOutputMode(id)]}  ▾" })
        {
            Name = id, Tag = id
        };
        devices.Items.Add(row);
        return row;
    }

    private void ResizeDeviceColumns()
    {
        int width = devices.ClientSize.Width;
        if (width <= 0 || resizingColumns) return;

        int padding = LogicalToDeviceUnits(14);
        int[] minimum = devices.Columns.Cast<ColumnHeader>()
            .Select(column => TextRenderer.MeasureText(column.Text, devices.Font).Width + padding)
            .ToArray();
        int total = minimum.Sum();
        if (total <= width)
        {
            int remaining = width - total;
            int xboxExtra = Math.Clamp(LogicalToDeviceUnits(66) - minimum[1], 0, remaining);
            minimum[1] += xboxExtra;
            remaining -= xboxExtra;
            int outputLabel = outputLabels.Max(label => TextRenderer.MeasureText($"{label}  ▾", devices.Font).Width);
            int outputTarget = Math.Min(width / 3, Math.Max(LogicalToDeviceUnits(152), outputLabel + padding));
            int outputExtra = Math.Clamp(outputTarget - minimum[OutputColumn], 0, remaining);
            minimum[OutputColumn] += outputExtra;
            remaining -= outputExtra;
            int nameExtra = remaining / 3;
            int statusExtra = remaining * 5 / 12;
            minimum[0] += nameExtra;
            minimum[2] += statusExtra;
            minimum[3] += remaining - nameExtra - statusExtra;
        }
        else
        {
            int available = width;
            for (int i = 0; i < minimum.Length - 1; i++)
            {
                minimum[i] = minimum[i] * width / total;
                available -= minimum[i];
            }
            minimum[OutputColumn] = available;
        }
        resizingColumns = true;
        try
        {
            for (int i = 0; i < minimum.Length; i++)
                if (devices.Columns[i].Width > minimum[i]) devices.Columns[i].Width = minimum[i];
            for (int i = 0; i < minimum.Length; i++)
                if (devices.Columns[i].Width < minimum[i]) devices.Columns[i].Width = minimum[i];
        }
        finally { resizingColumns = false; }
    }

    private bool HasCapacity(SessionMode mode) =>
        mode == SessionMode.Buttons ||
        (mode == SessionMode.Mouse || XboxReady && sessions.Values.Count(s => s.UsesXbox && !s.Completion.IsCompleted) < 4) &&
        (mode == SessionMode.Xbox || !MouseInUse());

    private bool MouseInUse() => sessions.Values.Any(session => session.UsesMouse && !session.Completion.IsCompleted);

    private async Task RecoverSessions()
    {
        if (closing || suspended || finding || recovering || updateHandoff) return;
        long now = Environment.TickCount64;
        var due = recovery.Where(pair => sessions.TryGetValue(pair.Key, out var session) &&
            session.Completion.IsCompleted && pair.Value.HasRetry(now, session.ReconnectRecommended) &&
            pair.Value.IsDue(now) && HasCapacity(pair.Value.Mode)).ToArray();
        if (due.Length == 0 && !deviceRefreshRequested) return;
        recovering = true;
        deviceRefreshRequested = false;
        try
        {
            await FindDevices(announce: false);
            if (closing || suspended) return;
            foreach (var (id, plan) in due)
            {
                if (!recovery.TryGetValue(id, out var current) || current != plan ||
                    !sessions[id].Completion.IsCompleted || !HasCapacity(plan.Mode) ||
                    !plan.TryTakeRetry(Environment.TickCount64)) continue;
                if (discovered.TryGetValue(id, out var device)) StartSession(id, device, plan);
            }
        }
        finally { recovering = false; }
    }

    protected override void WndProc(ref Message m)
    {
        const int PowerBroadcast = 0x0218;
        const int DeviceChange = 0x0219;
        if (m.Msg == PowerBroadcast && !closing)
        {
            if (m.WParam == 4) // PBT_APMSUSPEND
            {
                suspended = true;
                foreach (var (id, plan) in recovery)
                {
                    var session = sessions[id];
                    plan.Suspend(!session.Completion.IsCompleted);
                    if (!session.Completion.IsCompleted) _ = Task.Run(session.Stop);
                }
            }
            else if (m.WParam == 18) // PBT_APMRESUMEAUTOMATIC
            {
                suspended = false;
                deviceRefreshRequested = true;
            }
        }
        if (m.Msg == DeviceChange && m.WParam == 7) // DBT_DEVNODES_CHANGED
            deviceRefreshRequested = true;
        base.WndProc(ref m);
    }

    private Profile GetProfile(string id)
    {
        if (profiles.TryGetValue(id, out var cached)) return cached;
        try { return profiles[id] = profileStore.Read(id, discovered.GetValueOrDefault(id)?.HidPath); }
        catch (Exception error)
        {
            info.Text = UiText.Format("ProfileLoadFailed", error.Message);
            return profiles[id] = new Profile();
        }
    }

    private void EditProfile()
    {
        if (SelectedId is not { } id) return;
        using var dialog = new ProfileDialog(GetProfile(id));
        if (ShowOwnedDialog(dialog) != DialogResult.OK || dialog.Result is not { } updated) return;
        SaveProfile(id, updated, UiText.Get("ProfileSaved"));
    }

    private void CalibrateInfrared()
    {
        if (SelectedId is not { } id || !sessions.TryGetValue(id, out var session) ||
            session.Completion.IsCompleted || !session.InfraredReady)
        {
            info.Text = UiText.Get("EnableIrBeforeCalibration");
            return;
        }
        var capture = session.StartInfraredCalibration();
        try
        {
            using var dialog = new InfraredCalibrationDialog(session, capture);
            var result = ShowOwnedDialog(dialog);
            if (result == DialogResult.Abort)
            {
                info.Text = UiText.Get("CalibrationSessionLost");
                return;
            }
            if (result != DialogResult.OK) return;
            var previous = GetProfile(id);
            var updated = new Profile
            {
                Name = previous.Name, Horizontal = previous.Horizontal, Deadzone = previous.Deadzone,
                Bindings = previous.Bindings, InfraredCalibration = dialog.Result
            };
            SaveProfile(id, updated, dialog.Result is null ? UiText.Get("IrCalibrationReset") : UiText.Get("IrCalibrationSaved"));
        }
        finally { session.EndInfraredCalibration(capture); }
    }

    private void SaveProfile(string id, Profile updated, string success)
    {
        try
        {
            var map = new CoreInputMap(updated);
            profileStore.Save(id, updated);
            profiles[id] = updated;
            profileMaps[id] = map;
            if (sessions.TryGetValue(id, out var session) && !session.Completion.IsCompleted)
                session.ApplyProfile(map);
            if (devices.Items[id] is { } row) row.SubItems[3].Text = updated.Name;
            info.Text = success;
        }
        catch (Exception error) { info.Text = UiText.Format("ProfileSaveFailed", error.Message); }
    }

    private void ShowSessions()
    {
        foreach (ListViewItem row in devices.Items)
        {
            if (row.Tag is not string id) continue;
            sessions.TryGetValue(id, out var session);
            if (session is null || session.Completion.IsCompleted && !recovery.ContainsKey(id))
            {
                row.SubItems[1].Text = "—";
                row.SubItems[2].Text = GetOutputMode(id) switch
                {
                    _ when !discovered.ContainsKey(id) => UiText.Get("Disconnected"),
                    SessionMode.Xbox or SessionMode.XboxAndMouse when !XboxReady => UiText.Get("WaitingForXboxDriver"),
                    SessionMode.Mouse or SessionMode.XboxAndMouse when MouseInUse() => UiText.Get("WaitingForIrMouse"),
                    _ => UiText.Get("WaitingForXboxSlot")
                };
                if (session is null) continue;
            }
            else
            {
                bool waiting = !suspended && session.Completion.IsCompleted &&
                    recovery.TryGetValue(id, out var plan) &&
                    plan.HasRetry(Environment.TickCount64, session.ReconnectRecommended);
                row.SubItems[1].Text = !session.Completion.IsCompleted && session.Player is int player ? player.ToString() : "—";
                row.SubItems[2].Text = waiting ? UiText.Format("RetryingSession", session.Status) : session.Status;
            }
            row.SubItems[3].Text = session.Completion.IsCompleted && profiles.TryGetValue(id, out var saved)
                ? saved.Name : session.ProfileName;
        }
    }

    private async void CloseSessions(object? sender, FormClosingEventArgs args)
    {
        if (canClose || args.Cancel) return;
        if (trayIcon is not null && !exitRequested && args.CloseReason == CloseReason.UserClosing)
        {
            args.Cancel = true;
            Hide();
            return;
        }
        if (closing)
        {
            args.Cancel = args.CloseReason != CloseReason.WindowsShutDown;
            return;
        }
        closing = true;
        updateCancellation.Cancel();
        updateTimer.Stop();
        display.Stop();
        buttonDisplay.Stop();
        recovery.Clear();
        if (sessions.Values.All(session => session.Completion.IsCompleted))
        {
            canClose = true;
            return;
        }
        var closed = Task.WhenAll(sessions.Values.Select(async session =>
        {
            await Task.Run(session.Stop).ConfigureAwait(false);
            await session.Completion.ConfigureAwait(false);
        }));
        if (args.CloseReason == CloseReason.WindowsShutDown)
        {
            _ = Task.WhenAny(closed, Task.Delay(1200)).GetAwaiter().GetResult();
            canClose = true;
            return;
        }
        args.Cancel = true;
        Enabled = false;
        await Task.WhenAny(closed, Task.Delay(2800));
        canClose = true;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            updateCancellation.Cancel();
            updateCancellation.Dispose();
            updateTimer.Dispose();
            if (stagedUpdate is { } staged) AppUpdater.RemoveStage(staged.Directory);
            display.Dispose();
            buttonDisplay.Dispose();
            outputMenu.Dispose();
            var menu = trayIcon?.ContextMenuStrip;
            trayIcon?.Dispose();
            menu?.Dispose();
            trayImage?.Dispose();
        }
        base.Dispose(disposing);
        if (disposing) appIcon.Dispose();
    }
}
