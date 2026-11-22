namespace Wii2Xbox;

internal sealed class PairingDialog : Form
{
    private readonly ListBox found = new() { Dock = DockStyle.Fill, AccessibleName = UiText.Get("FoundWiiControllers") };
    private readonly Button pair = new() { Text = UiText.Get("PairSelected"), AutoSize = true };
    private readonly Button close = new() { Text = UiText.Get("Close"), AutoSize = true, DialogResult = DialogResult.Cancel };
    private readonly ComboBox method = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 85, AccessibleName = UiText.Get("PairingButtons") };
    private readonly Label status = new() { Text = UiText.Get("SearchingBluetooth"), AutoSize = true, MaximumSize = new Size(535, 0) };
    private readonly Dictionary<ulong, BluetoothService.Candidate> discovered = new();
    private CancellationTokenSource? scanCancellation;
    private Task scanTask = Task.CompletedTask;
    private bool pairing;
    private bool closing;
    public ulong? PairedAddress { get; private set; }

    public PairingDialog()
    {
        Text = UiText.Get("AddControllerTitle");
        ClientSize = new Size(575, 310);
        MinimumSize = new Size(590, 300);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = UiText.Get("PairingInstructions"), AutoSize = true, MaximumSize = new Size(535, 0) });
        layout.Controls.Add(found);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        method.Items.AddRange(new object[] { "SYNC", "1+2" });
        method.SelectedIndex = 0;
        buttons.Controls.AddRange([method, pair, close]);
        layout.Controls.Add(buttons);
        layout.Controls.Add(status);
        Controls.Add(layout);
        CancelButton = close;
        Shown += (_, _) => StartScanning();
        pair.Click += async (_, _) => await Pair();
        found.SelectedIndexChanged += (_, _) => UpdateButtons();
        FormClosing += StopBeforeClosing;
        UpdateButtons();
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
                    status.Text = discovered.Count == 0 ? UiText.Get("SearchingPressSync")
                        : discovered.Values.Any(item => item.CanPair)
                            ? UiText.Get("SearchingChooseController")
                            : UiText.Get("SearchingSupportedModels");
                }
                catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
                {
                    cancellation.ThrowIfCancellationRequested();
                    status.Text = error.Message;
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
        bool sync = method.SelectedIndex == 0;
        pairing = true;
        UpdateButtons();
        status.Text = UiText.Get("PairingInProgress");
        try
        {
            scanCancellation?.Cancel();
            await scanTask;
            var current = discovered[device.Device.Address];
            await Task.Run(() => BluetoothService.Pair(current, sync));
            PairedAddress = device.Device.Address;
        }
        catch (Exception error)
        {
            status.Text = UiText.Format("PairingRetry", error.Message);
        }
        finally
        {
            pairing = false;
            UpdateButtons();
        }
        if (PairedAddress.HasValue) DialogResult = DialogResult.OK;
        else StartScanning();
    }

    private void UpdateButtons()
    {
        bool canSelect = !pairing && !closing;
        method.Enabled = canSelect && found.SelectedItem is BluetoothService.Candidate { CanPair: true };
        found.Enabled = canSelect;
        pair.Enabled = method.Enabled;
        close.Enabled = !pairing && !closing;
    }
}
