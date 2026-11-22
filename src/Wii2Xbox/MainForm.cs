namespace Wii2Xbox;

public sealed class MainForm : Form
{
    private const int OutputColumn = 4;
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
    private readonly Label info = new() { AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
    private readonly Label xboxStatus = new() { AutoSize = true, Padding = new Padding(0, 4, 0, 0) };
    private readonly Label languageNotice = new() { AutoSize = true, Visible = false };
    private readonly Button installXbox = new() { Text = UiText.Get("InstallXboxDriver"), AutoSize = true, Visible = false };
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
    private bool pairingTransition;
    private bool installingXbox;
    private bool xboxChecked;
    private bool initialized;
    private bool exitRequested;
    private NotifyIcon? trayIcon;
    private Icon? trayImage;
    internal bool XboxReady { get; private set; }

    public MainForm() : this(WiiDevice.Find) { }

    internal MainForm(Func<WiiDevice[]> enumerateDevices)
    {
        this.enumerateDevices = enumerateDevices;
        using (var icon = typeof(MainForm).Assembly.GetManifestResourceStream("Wii2Xbox.AppIcon")
            ?? throw new InvalidOperationException(UiText.Get("MissingAppIcon")))
            appIcon = new Icon(icon);
        Text = "Wii2Xbox";
        Icon = appIcon;
        ClientSize = new Size(850, 520);
        MinimumSize = new Size(690, 420);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 8 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 165));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = UiText.Get("WiiControllers"), Font = new Font("Segoe UI", 22, FontStyle.Bold), AutoSize = true }, 0, 0);
        layout.Controls.Add(CreateLanguagePicker(), 1, 0);
        var actions = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill };
        actions.Controls.Add(Action(UiText.Get("AddControllerButton"), () => _ = PairDevice()));
        actions.Controls.Add(Action(UiText.Get("EditProfileButton"), EditProfile));
        actions.Controls.Add(Action(UiText.Get("CalibrateIrButton"), CalibrateInfrared));
        actions.Controls.Add(installXbox);
        layout.Controls.Add(actions, 0, 1);
        var support = Action(UiText.Get("CoffeeButton"), OpenSupportPage);
        support.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        layout.Controls.Add(support, 1, 1);
        startup.Click += (_, _) => UpdateStartup();
        installXbox.Click += async (_, _) => await InstallXbox();
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
        devices.ClientSizeChanged += (_, _) => ResizeDeviceColumns();
        devices.HandleCreated += (_, _) => ResizeDeviceColumns();
        AddFullWidth(devices, 2);
        AddFullWidth(startup, 3);
        AddFullWidth(remoteView, 4);
        AddFullWidth(info, 5);
        AddFullWidth(xboxStatus, 6);
        AddFullWidth(languageNotice, 7);
        layout.SizeChanged += (_, _) =>
        {
            var limit = new Size(Math.Max(1, layout.ClientSize.Width - layout.Padding.Horizontal - 8), 0);
            info.MaximumSize = xboxStatus.MaximumSize = languageNotice.MaximumSize = limit;
        };
        Controls.Add(layout);
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

        void AddFullWidth(Control control, int row)
        {
            layout.Controls.Add(control, 0, row);
            layout.SetColumnSpan(control, 2);
        }
    }

    private Control CreateLanguagePicker()
    {
        var options = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right, AutoSize = true, WrapContents = false
        };
        var label = new Label
        {
            Text = UiText.Get("Language"), AutoSize = true, Margin = new Padding(3, 7, 0, 0)
        };
        var picker = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, Width = 100, AccessibleName = label.Text
        };
        picker.Items.AddRange([UiText.Get("FollowSystem"), "Español", "English"]);
        var savedMode = UiLanguage.Mode;
        picker.SelectedIndex = (int)savedMode;
        picker.SelectionChangeCommitted += (_, _) =>
        {
            var selected = (UiLanguageMode)picker.SelectedIndex;
            try
            {
                UiLanguage.Save(selected);
                savedMode = selected;
                languageNotice.Text = UiText.Get("LanguageRestart");
                languageNotice.Visible = selected != UiLanguage.Mode;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                System.Diagnostics.Debug.WriteLine(error);
                picker.SelectedIndex = (int)savedMode;
                languageNotice.Text = UiText.Get("LanguageSaveFailed");
                languageNotice.Visible = true;
            }
        };
        options.Controls.AddRange([label, picker]);
        return options;
    }

    internal void StartResident(bool background)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(UiText.Get("OpenApp"), null, (_, _) => RestoreWindow());
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
        trayIcon.DoubleClick += (_, _) => RestoreWindow();
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
        installXbox.Enabled = false;
        var status = await Task.Run(() => XboxOutput.CheckDriver());
        if (closing || IsDisposed) return;
        xboxChecked = true;
        XboxReady = status.Ready;
        if (status.Ready) unavailableXboxOutputs.Clear();
        installXbox.Visible = status.CanInstall && VigemBusSetup.IsBundled && !restartRequired;
        installXbox.Enabled = true;
        string message = installXbox.Visible
            ? UiText.Format("DriverInstallHint", status.Message)
            : status.Message;
        xboxStatus.Text = installationResult is null ? (status.Ready ? "" : message) : $"{installationResult} {message}";
        if (!status.Ready && !Visible && installationResult is null)
            trayIcon?.ShowBalloonTip(5000, "Wii2Xbox", message, ToolTipIcon.Warning);
        StartAvailableSessions();
    }

    private async Task InstallXbox()
    {
        installingXbox = true;
        installXbox.Enabled = false;
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

    private void OpenSupportPage()
    {
        try
        {
            using var browser = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                "https://ko-fi.com/ivanfernandezgarcia") { UseShellExecute = true });
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            info.Text = UiText.Get("SupportPageFailed");
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
            ResizeDeviceColumns();
            if (devices.SelectedItems.Count == 0 && devices.Items.Count > 0) devices.Items[0].Selected = true;
            StartAvailableSessions();
            if (announce)
                info.Text = found.Length == 0
                    ? UiText.Get("NoControllersHint")
                    : UiText.Get("ControllersReadyHint");
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
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.PairedAddress is not ulong address) return;
        info.Text = UiText.Get("WaitingForHid");
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
                info.Text = UiText.Get("PairingSuccess");
                return;
            }
            if (Environment.TickCount64 >= deadline) break;
            await Task.Delay(300);
        }
        if (!closing)
            info.Text = UiText.Get("PairingConfirmed");
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
        if (closing || suspended) return;
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
        if (width <= 0) return;

        int xboxWidth = Math.Min(LogicalToDeviceUnits(66), width / 4);
        int outputWidth = Math.Min(LogicalToDeviceUnits(152), width / 3);
        int remaining = width - xboxWidth - outputWidth;
        int nameWidth = remaining / 3;
        int statusWidth = remaining * 5 / 12;
        devices.Columns[0].Width = nameWidth;
        devices.Columns[1].Width = xboxWidth;
        devices.Columns[2].Width = statusWidth;
        devices.Columns[3].Width = remaining - nameWidth - statusWidth;
        devices.Columns[OutputColumn].Width = outputWidth;
    }

    private bool HasCapacity(SessionMode mode) =>
        mode == SessionMode.Buttons ||
        (mode == SessionMode.Mouse || XboxReady && sessions.Values.Count(s => s.UsesXbox && !s.Completion.IsCompleted) < 4) &&
        (mode == SessionMode.Xbox || !MouseInUse());

    private bool MouseInUse() => sessions.Values.Any(session => session.UsesMouse && !session.Completion.IsCompleted);

    private async Task RecoverSessions()
    {
        if (closing || suspended || finding || recovering) return;
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
        if (SelectedId is not { } id)
        {
            info.Text = UiText.Get("SelectProfileController");
            return;
        }
        using var dialog = new ProfileDialog(GetProfile(id));
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is not { } updated) return;
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
            var result = dialog.ShowDialog(this);
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
