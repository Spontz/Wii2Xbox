namespace Wii2Xbox;

internal sealed class PairingDialog : Form
{
    private const int RecentDeviceSeconds = 15;
    private readonly ListBox found = new() { Dock = DockStyle.Fill, AccessibleName = UiText.Get("FoundBluetoothDevices") };
    private readonly Button pair = new() { Text = UiText.Get("PairSelected"), AutoSize = true };
    private readonly DeviceScanSwitch displayAll = new() { Text = UiText.Get("DisplayAllDevices"), AutoSize = true };
    private readonly Label status = new() { Text = UiText.Get("SearchingBluetooth"), AutoSize = true };
    private readonly Dictionary<ulong, BluetoothService.Candidate> discovered = new();
    private readonly Func<ulong, Task<bool?>> waitForDevice;
    private CancellationTokenSource? scanCancellation;
    private Task scanTask = Task.CompletedTask;
    private bool pairing;
    private bool closing;
    private string? pairingFailure;

    public PairingDialog(Func<ulong, Task<bool?>> waitForDevice)
    {
        this.waitForDevice = waitForDevice;
        Text = UiText.Get("AddControllerTitle");
        ScrollableLayout.ConfigureForm(this, new Size(620, 510), new Size(520, 415), expandOnLargeDisplays: true);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        var layout = new TableLayoutPanel { Padding = new Padding(14), ColumnCount = 1, RowCount = 6 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var pictureRow = new RowStyle(SizeType.Absolute, 112);
        layout.RowStyles.Add(pictureRow);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var instructions = new Label { Text = UiText.Get("PairingInstructions"), AutoSize = true };
        layout.Controls.Add(instructions, 0, 0);
        using var illustration = typeof(PairingDialog).Assembly.GetManifestResourceStream("Wii2Xbox.SyncButtonImage")
            ?? throw new InvalidOperationException(UiText.Get("MissingViewerImage"));
        using var source = Image.FromStream(illustration);
        var picture = new PictureBox
        {
            Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom,
            Image = new Bitmap(source), AccessibleName = UiText.Get("PairingInstructions")
        };
        picture.Disposed += (_, _) => picture.Image?.Dispose();
        layout.Controls.Add(picture, 0, 1);
        layout.Controls.Add(displayAll, 0, 2);
        layout.Controls.Add(found, 0, 3);
        layout.Controls.Add(pair, 0, 4);
        layout.Controls.Add(status, 0, 5);
        var viewport = new ScrollableLayout(layout, 3, 120, instructions, status);
        Controls.Add(viewport);
        viewport.ClientSizeChanged += (_, _) => FitPicture();
        picture.DpiChangedAfterParent += (_, _) => FitPicture();
        Load += (_, _) => FitPicture();
        Shown += (_, _) => StartScanning();
        displayAll.CheckedChanged += (_, _) =>
        {
            pairingFailure = null;
            discovered.Clear();
            found.Items.Clear();
            UpdateButtons();
            status.Text = UiText.Get(displayAll.Checked ? "SearchingAllBluetooth" : "SearchingBluetooth");
        };
        pair.Click += async (_, _) => await Pair();
        found.SelectedIndexChanged += (_, _) => UpdateButtons();
        FormClosing += StopBeforeClosing;
        UpdateButtons();

        void FitPicture()
        {
            if (picture.IsDisposed || viewport.ClientSize.Width <= 0 || viewport.ClientSize.Height <= 0) return;
            int width = Math.Max(1, viewport.ClientSize.Width - layout.Padding.Horizontal - picture.Margin.Horizontal);
            int aspectHeight = (int)Math.Ceiling(width * (double)picture.Image.Height / picture.Image.Width);
            int limit = Math.Max(LogicalToDeviceUnits(112), viewport.ClientSize.Height / 2);
            int height = Math.Min(aspectHeight + picture.Margin.Vertical, limit);
            if (pictureRow.Height != height) pictureRow.Height = height;
        }
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData != Keys.Escape) return base.ProcessDialogKey(keyData);
        Close();
        return true;
    }

    private void StartScanning()
    {
        var operation = new CancellationTokenSource();
        scanCancellation = operation;
        scanTask = Scan(operation);
    }

    private async Task Scan(CancellationTokenSource operation)
    {
        var cancellation = operation.Token;
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                int delay = 400;
                bool showAll = displayAll.Checked;
                try
                {
                    var batch = await Task.Run(() => BluetoothService.Find(cancellation, showAll), cancellation);
                    cancellation.ThrowIfCancellationRequested();
                    if (showAll != displayAll.Checked) continue;
                    UpdateResults(batch, showAll);
                    status.Text = pairingFailure ?? (showAll
                        ? UiText.Get(discovered.Count == 0 ? "SearchingAllBluetooth" : "SearchingChooseDevice")
                        : discovered.Count == 0 ? UiText.Get("SearchingBluetooth")
                        : discovered.Values.Any(item => item.CanPair)
                            ? UiText.Get("SearchingChooseController")
                            : UiText.Get("SearchingSupportedModels"));
                }
                catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (showAll != displayAll.Checked) continue;
                    status.Text = pairingFailure ?? error.Message;
                    delay = 1500;
                }
                await Task.Delay(delay, cancellation);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        finally
        {
            scanCancellation = null;
            operation.Dispose();
        }
    }

    private void UpdateResults(BluetoothService.Candidate[] batch, bool showAll)
    {
        bool listChanged = false;
        if (showAll)
        {
            var present = batch.Select(item => item.Device.Address).ToHashSet();
            var cutoff = DateTime.UtcNow.AddSeconds(-RecentDeviceSeconds);
            foreach (ulong address in discovered.Where(entry => !present.Contains(entry.Key) &&
                !entry.Value.Device.LastSeen.IsAtOrAfter(cutoff)).Select(entry => entry.Key).ToArray())
            {
                discovered.Remove(address);
                listChanged = true;
            }
        }
        foreach (var item in batch)
        {
            ulong address = item.Device.Address;
            if (!discovered.TryGetValue(address, out var previous) || previous.Device.Name != item.Device.Name ||
                previous.CanTryPair != item.CanTryPair)
                listChanged = true;
            discovered[address] = item;
        }
        if (!listChanged) return;

        var selectedAddress = (found.SelectedItem as BluetoothService.Candidate)?.Device.Address;
        var devices = discovered.Values.OrderByDescending(item => item.CanPair).ToArray();
        int selected = Array.FindIndex(devices, item => item.Device.Address == selectedAddress);
        found.BeginUpdate();
        try
        {
            found.Items.Clear();
            found.Items.AddRange(devices);
            found.SelectedIndex = selected >= 0 || showAll || devices.Length == 0 ? selected : 0;
        }
        finally { found.EndUpdate(); }
        UpdateButtons();
    }

    private async void StopBeforeClosing(object? sender, FormClosingEventArgs args)
    {
        if (args.CloseReason == CloseReason.WindowsShutDown)
        {
            scanCancellation?.Cancel();
            return;
        }
        if (!pairing && scanTask.IsCompleted) return;
        args.Cancel = true;
        if (pairing || closing) return;

        closing = true;
        UpdateButtons();
        status.Text = UiText.Get("StoppingBluetoothSearch");
        scanCancellation?.Cancel();
        await scanTask;
        if (!IsDisposed) Close();
    }

    private async Task Pair()
    {
        if (found.SelectedItem is not BluetoothService.Candidate device || !displayAll.Checked && !device.CanPair) return;
        if (!device.CanTryPair)
        {
            status.Text = pairingFailure = UiText.Get("NotAWiiRemote");
            return;
        }
        pairing = true;
        bool tryUnknown = !device.CanPair;
        pairingFailure = null;
        var result = DialogResult.None;
        UpdateButtons();
        status.Text = UiText.Get("PairingInProgress");
        try
        {
            scanCancellation?.Cancel();
            await scanTask;
            var current = discovered[device.Device.Address];
            bool alreadyKnown = await Task.Run(() => BluetoothService.Pair(current, tryUnknown: tryUnknown));
            bool? ready = IsDisposed ? null : await WaitForInput();
            if (tryUnknown && !alreadyKnown && ready is not true)
                await Task.Run(() => BluetoothService.ForgetPairing(current.Device.Address));
            if (IsDisposed) return;
            if (ready is false && !tryUnknown && alreadyKnown && await Task.Run(() => BluetoothService.RemoveDisconnectedPairing(current)))
            {
                if (IsDisposed) return;
                discovered.Remove(device.Device.Address);
                found.Items.Remove(device);
                status.Text = UiText.Get("PairingAgain");
                await Task.Run(() => BluetoothService.Pair(current, afterRemovingPairing: true));
                if (IsDisposed) return;
                ready = await WaitForInput();
            }
            if (ready is true) result = DialogResult.OK;
            else if (ready is null) result = DialogResult.Cancel;
            else pairingFailure = UiText.Get(tryUnknown ? "NotAWiiRemote" : "PairingInputUnavailable");
        }
        catch (Exception error)
        {
            pairingFailure = UiText.Format(tryUnknown ? "UnknownControllerPairingFailed" : "PairingRetry", error.Message);
        }
        finally
        {
            pairing = false;
            if (!IsDisposed) UpdateButtons();
        }
        if (IsDisposed) return;
        if (result != DialogResult.None)
        {
            DialogResult = result;
            return;
        }
        status.Text = pairingFailure;
        StartScanning();

        Task<bool?> WaitForInput()
        {
            status.Text = UiText.Get("WaitingForPairedInput");
            return waitForDevice(device.Device.Address);
        }
    }

    private void UpdateButtons()
    {
        bool canSelect = !pairing && !closing;
        found.Enabled = canSelect;
        displayAll.Enabled = canSelect;
        pair.Enabled = canSelect && found.SelectedItem is BluetoothService.Candidate candidate &&
            (displayAll.Checked || candidate.CanPair);
    }

    private sealed class DeviceScanSwitch : CheckBox
    {
        public DeviceScanSwitch() => SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        public override Size GetPreferredSize(Size proposedSize)
        {
            var text = TextRenderer.MeasureText(Text, Font);
            return new Size(text.Width + LogicalToDeviceUnits(44), Math.Max(text.Height, LogicalToDeviceUnits(24)));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var canvas = e.Graphics;
            canvas.Clear(BackColor);
            int height = LogicalToDeviceUnits(18);
            int width = LogicalToDeviceUnits(34);
            int y = (Height - height) / 2;
            canvas.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var track = new SolidBrush(Checked ? (SystemInformation.HighContrast ? SystemColors.Highlight : Color.SeaGreen)
                : SystemColors.ControlDark);
            canvas.FillEllipse(track, 0, y, height, height);
            canvas.FillRectangle(track, height / 2, y, width - height, height);
            canvas.FillEllipse(track, width - height, y, height, height);
            int inset = LogicalToDeviceUnits(3);
            using var thumb = new SolidBrush(SystemInformation.HighContrast ? SystemColors.HighlightText : Color.White);
            canvas.FillEllipse(thumb, (Checked ? width - height : 0) + inset, y + inset, height - 2 * inset, height - 2 * inset);
            var label = new Rectangle(width + LogicalToDeviceUnits(7), 0, Width - width - LogicalToDeviceUnits(7), Height);
            TextRenderer.DrawText(canvas, Text, Font, label, Enabled ? ForeColor : SystemColors.GrayText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(canvas, label);
        }
    }
}
