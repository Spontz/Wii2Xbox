namespace Wii2Xbox;

public sealed class MainForm : Form
{
    private const int OutputColumn = 4;
    private static readonly string[] OutputLabels = ["Xbox", "Ratón IR", "Xbox y ratón IR"];
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
    private readonly Button installXbox = new() { Text = "Instalar controlador Xbox", AutoSize = true, Visible = false };
    private readonly CheckBox startup = new()
    {
        Text = "Arrancar al iniciar Windows", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 0, 0)
    };
    private readonly ContextMenuStrip outputMenu = new()
    {
        AccessibleName = "Salida del mando", ShowImageMargin = false, ShowCheckMargin = true
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
            ?? throw new InvalidOperationException("No se encontró el icono de Wii2Xbox."))
            appIcon = new Icon(icon);
        Text = "Wii2Xbox";
        Icon = appIcon;
        ClientSize = new Size(850, 520);
        MinimumSize = new Size(690, 420);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 7 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 165));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        AddFullWidth(new Label { Text = "Mandos Wii", Font = new Font("Segoe UI", 22, FontStyle.Bold), AutoSize = true }, 0);
        var actions = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill };
        actions.Controls.Add(Action("Añadir mando…", () => _ = PairDevice()));
        actions.Controls.Add(Action("Perfil…", EditProfile));
        actions.Controls.Add(Action("&Calibrar IR…", CalibrateInfrared));
        actions.Controls.Add(installXbox);
        layout.Controls.Add(actions, 0, 1);
        var support = Action("Invítame a un café", OpenSupportPage);
        support.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        layout.Controls.Add(support, 1, 1);
        startup.Click += (_, _) => UpdateStartup();
        installXbox.Click += async (_, _) => await InstallXbox();
        devices.Columns.Add("Mando");
        devices.Columns.Add("Xbox");
        devices.Columns.Add("Estado");
        devices.Columns.Add("Perfil");
        devices.Columns.Add("Salida");
        for (int index = 0; index < OutputLabels.Length; index++)
        {
            var mode = (SessionMode)index;
            outputMenu.Items.Add(OutputLabels[index], null, (_, _) =>
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
        layout.SizeChanged += (_, _) =>
        {
            var limit = new Size(Math.Max(1, layout.ClientSize.Width - layout.Padding.Horizontal - 8), 0);
            info.MaximumSize = xboxStatus.MaximumSize = limit;
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

    internal void StartResident(bool background)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir Wii2Xbox", null, (_, _) => RestoreWindow());
        menu.Items.Add("Salir", null, (_, _) =>
        {
            if (installingXbox)
            {
                RestoreWindow();
                xboxStatus.Text = "Espera a que termine la instalación de ViGEmBus antes de salir.";
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
            startupError = "No se pudo leer la configuración de inicio de Windows.";
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
            info.Text = $"No se pudo cambiar el inicio con Windows. {error.Message}";
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
            ? $"{status.Message} Puedes instalar el controlador desde Wii2Xbox."
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
        xboxStatus.Text = "Instalando ViGEmBus. Autoriza la instalación cuando Windows lo solicite.";
        string result;
        bool restartRequired = false;
        try
        {
            int code = await Task.Run(VigemBusSetup.Install);
            restartRequired = code is 1641 or 3010;
            result = code switch
            {
                0 => "El instalador de ViGEmBus ha terminado.",
                1641 or 3010 => "Reinicia Windows para completar la instalación de ViGEmBus.",
                1602 => "Instalación cancelada.",
                _ => $"El instalador de ViGEmBus devolvió el código {code}."
            };
        }
        catch (System.ComponentModel.Win32Exception error) when (error.NativeErrorCode == 1223)
        {
            result = "Instalación cancelada.";
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            result = $"No se pudo iniciar la instalación. {error.Message}";
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
            info.Text = "No se pudo abrir Ko-fi. Comprueba que haya un navegador predeterminado.";
        }
    }

    private async Task FindDevices(bool announce = true)
    {
        if (finding || suspended || closing) return;
        finding = true;
        if (announce) info.Text = "Buscando mandos Wii asociados en Windows…";
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
                    ? "Pulsa Añadir mando… para conectar un Wii Remote."
                    : "Los mandos disponibles se activan automáticamente. Puedes cambiar la salida de cada mando.";
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
        info.Text = "Bluetooth asociado. Esperando a que Windows publique el mando HID…";
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
                info.Text = "Mando asociado. La salida se activa automáticamente cuando está disponible.";
                return;
            }
            if (Environment.TickCount64 >= deadline) break;
            await Task.Delay(300);
        }
        if (!closing)
            info.Text = "Windows confirmó la asociación. Si el mando aparece en la lista, selecciónalo allí.";
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
        row.SubItems[OutputColumn].Text = $"{OutputLabels[(int)mode]}  ▾";
        recovery.Remove(id);
        unavailableXboxOutputs.Remove(id);
        if (sessions.TryGetValue(id, out var session) && !session.Completion.IsCompleted)
            _ = Task.Run(session.Stop);
        StartAvailableSessions();
        if (mode is SessionMode.Mouse or SessionMode.XboxAndMouse)
            info.Text = "Enciende la barra, apunta con el mando en vertical y usa A para clic izquierdo y B para derecho.";
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
        var row = new ListViewItem(new[] { name, "—", "Disponible", "", $"{OutputLabels[(int)GetOutputMode(id)]}  ▾" })
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
            info.Text = $"No se pudo cargar el perfil. Se usará el predeterminado. {error.Message}";
            return profiles[id] = new Profile();
        }
    }

    private void EditProfile()
    {
        if (SelectedId is not { } id)
        {
            info.Text = "Selecciona el mando cuyo perfil quieres editar.";
            return;
        }
        using var dialog = new ProfileDialog(GetProfile(id));
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is not { } updated) return;
        SaveProfile(id, updated, "Perfil guardado para este mando.");
    }

    private void CalibrateInfrared()
    {
        if (SelectedId is not { } id || !sessions.TryGetValue(id, out var session) ||
            session.Completion.IsCompleted || !session.InfraredReady)
        {
            info.Text = "Activa el ratón IR y espera a que el mando confirme la cámara antes de calibrar.";
            return;
        }
        var capture = session.StartInfraredCalibration();
        try
        {
            using var dialog = new InfraredCalibrationDialog(session, capture);
            var result = dialog.ShowDialog(this);
            if (result == DialogResult.Abort)
            {
                info.Text = "Se perdió la sesión durante la calibración.";
                return;
            }
            if (result != DialogResult.OK) return;
            var previous = GetProfile(id);
            var updated = new Profile
            {
                Name = previous.Name, Horizontal = previous.Horizontal, Deadzone = previous.Deadzone,
                Bindings = previous.Bindings, InfraredCalibration = dialog.Result
            };
            SaveProfile(id, updated, dialog.Result is null ? "Calibración IR restablecida." : "Calibración IR guardada.");
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
        catch (Exception error) { info.Text = $"No se pudo guardar el perfil. {error.Message}"; }
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
                    _ when !discovered.ContainsKey(id) => "Desconectado",
                    SessionMode.Xbox or SessionMode.XboxAndMouse when !XboxReady => "Esperando al controlador Xbox",
                    SessionMode.Mouse or SessionMode.XboxAndMouse when MouseInUse() => "Esperando al ratón IR",
                    _ => "Esperando una ranura Xbox"
                };
                if (session is null) continue;
            }
            else
            {
                bool waiting = !suspended && session.Completion.IsCompleted &&
                    recovery.TryGetValue(id, out var plan) &&
                    plan.HasRetry(Environment.TickCount64, session.ReconnectRecommended);
                row.SubItems[1].Text = !session.Completion.IsCompleted && session.Player is int player ? player.ToString() : "—";
                row.SubItems[2].Text = waiting ? $"{session.Status} · reintentando" : session.Status;
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
