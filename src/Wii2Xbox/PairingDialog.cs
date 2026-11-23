namespace Wii2Xbox;

internal sealed class PairingDialog : Form
{
    private readonly ListBox found = new() { Dock = DockStyle.Fill, AccessibleName = UiText.Get("FoundWiiControllers") };
    private readonly Button pair = new() { Text = UiText.Get("PairSelected"), AutoSize = true };
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
        ScrollableLayout.ConfigureForm(this, new Size(575, 450), new Size(520, 415));
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        var layout = new TableLayoutPanel { Padding = new Padding(14), ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
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
        found.Dock = DockStyle.Fill;
        layout.Controls.Add(found, 0, 2);
        layout.Controls.Add(pair, 0, 3);
        layout.Controls.Add(status, 0, 4);
        Controls.Add(new ScrollableLayout(layout, 2, 120, instructions, status));
        Shown += (_, _) => StartScanning();
        pair.Click += async (_, _) => await Pair();
        found.SelectedIndexChanged += (_, _) => UpdateButtons();
        FormClosing += StopBeforeClosing;
        UpdateButtons();
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
                try
                {
                    var batch = await Task.Run(() => BluetoothService.Find(cancellation), cancellation);
                    cancellation.ThrowIfCancellationRequested();
                    UpdateResults(batch);
                    status.Text = pairingFailure ?? (discovered.Count == 0 ? UiText.Get("SearchingBluetooth")
                        : discovered.Values.Any(item => item.CanPair)
                            ? UiText.Get("SearchingChooseController")
                            : UiText.Get("SearchingSupportedModels"));
                }
                catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
                {
                    cancellation.ThrowIfCancellationRequested();
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

    private void UpdateResults(BluetoothService.Candidate[] batch)
    {
        bool listChanged = false;
        foreach (var item in batch)
        {
            ulong address = item.Device.Address;
            if (!discovered.TryGetValue(address, out var previous) || previous.Device.Name != item.Device.Name)
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
            found.SelectedIndex = Math.Max(0, selected);
        }
        finally { found.EndUpdate(); }
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
        if (found.SelectedItem is not BluetoothService.Candidate { CanPair: true } device) return;
        pairing = true;
        pairingFailure = null;
        var result = DialogResult.None;
        UpdateButtons();
        status.Text = UiText.Get("PairingInProgress");
        try
        {
            scanCancellation?.Cancel();
            await scanTask;
            var current = discovered[device.Device.Address];
            bool alreadyKnown = await Task.Run(() => BluetoothService.Pair(current));
            if (IsDisposed) return;
            bool? ready = await WaitForInput();
            if (IsDisposed) return;
            if (ready is false && alreadyKnown && await Task.Run(() => BluetoothService.RemoveDisconnectedPairing(current)))
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
            else pairingFailure = UiText.Get("PairingInputUnavailable");
        }
        catch (Exception error)
        {
            pairingFailure = UiText.Format("PairingRetry", error.Message);
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
        pair.Enabled = canSelect && found.SelectedItem is BluetoothService.Candidate { CanPair: true };
    }
}
