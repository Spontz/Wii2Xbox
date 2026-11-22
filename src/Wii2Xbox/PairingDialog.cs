namespace Wii2Xbox;

internal sealed class PairingDialog : Form
{
    private readonly ListBox found = new() { Dock = DockStyle.Fill, AccessibleName = UiText.Get("FoundWiiControllers") };
    private readonly Button pair = new() { Text = UiText.Get("PairSelected"), AutoSize = true };
    private readonly Button close = new() { Text = UiText.Get("Close"), AutoSize = true, DialogResult = DialogResult.Cancel };
    private readonly ComboBox method = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 85, AccessibleName = UiText.Get("PairingButtons") };
    private readonly Label status = new() { Text = UiText.Get("SearchingBluetooth"), AutoSize = true };
    private readonly Dictionary<ulong, BluetoothService.Candidate> discovered = new();
    private CancellationTokenSource? scanCancellation;
    private Task scanTask = Task.CompletedTask;
    private bool pairing;
    private bool closing;
    public ulong? PairedAddress { get; private set; }

    public PairingDialog()
    {
        Text = UiText.Get("AddControllerTitle");
        ScrollableLayout.ConfigureForm(this, new Size(575, 375), new Size(520, 340));
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        var layout = new TableLayoutPanel { Padding = new Padding(14), ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var instructions = new Label { Text = UiText.Get("PairingInstructions"), AutoSize = true };
        layout.Controls.Add(instructions, 0, 0);
        layout.Controls.Add(found, 0, 1);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        method.Items.AddRange(new object[] { "SYNC", "1+2" });
        method.SelectedIndex = 0;
        ScrollableLayout.FitChoices(method, 85);
        buttons.Controls.AddRange([pair, close]);
        layout.Controls.Add(method, 0, 2);
        layout.Controls.Add(buttons, 0, 3);
        layout.Controls.Add(status, 0, 4);
        Controls.Add(new ScrollableLayout(layout, 1, 120, instructions, status));
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
