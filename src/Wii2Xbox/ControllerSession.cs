using System.Diagnostics;

namespace Wii2Xbox;

internal enum SessionMode { Xbox, Mouse, XboxAndMouse, Buttons }

internal sealed class ControllerSession
{
    private readonly Func<IControllerTransport> openTransport;
    private readonly Func<IXboxOutput> openXbox;
    private readonly Func<InfraredMouse>? openMouse;
    private readonly SessionMode mode;
    private CoreInputMap activeMap;
    private CoreInputMap? pendingMap;
    private readonly object lifecycle = new();
    private bool stopping;
    private IControllerTransport? stream;
    private string status = "Abriendo mando…";
    private int reports;
    private int truncatedReports;
    private int unknownReports;
    private int profileChanges;
    private int player;
    private int infraredReady;
    private LatencyMetrics? activeMetrics;
    private LatencyMetrics? latestMetrics;
    private InfraredCalibrationCapture? calibrationCapture;
    public bool InfraredReady => Volatile.Read(ref infraredReady) != 0;
    public InfraredCalibrationCapture StartInfraredCalibration()
    {
        var capture = new InfraredCalibrationCapture();
        if (Interlocked.CompareExchange(ref calibrationCapture, capture, null) is not null)
            throw new InvalidOperationException("Ya hay una calibración abierta para este mando.");
        return capture;
    }
    public void EndInfraredCalibration(InfraredCalibrationCapture capture) =>
        Interlocked.CompareExchange(ref calibrationCapture, null, capture);
    public bool ReconnectRecommended { get; private set; }
    public bool XboxOutputUnavailable { get; private set; }
    public WiiButtonState Buttons { get; } = new();
    public LatencyMetrics? Metrics => Volatile.Read(ref latestMetrics);

    public void SetLatencyCapture(bool enabled)
    {
        if (!UsesXbox) return;
        LatencyMetrics? capture = null;
        if (enabled)
        {
            capture = new LatencyMetrics();
            Volatile.Write(ref latestMetrics, capture);
        }
        Volatile.Write(ref activeMetrics, capture);
    }
    public string Model { get; }
    public bool UsesMouse => mode is SessionMode.Mouse or SessionMode.XboxAndMouse;
    public bool UsesXbox => mode is SessionMode.Xbox or SessionMode.XboxAndMouse;
    public bool Horizontal => Volatile.Read(ref activeMap).Horizontal;
    public string ProfileName => Volatile.Read(ref activeMap).Name;
    public int ProfileChanges => Volatile.Read(ref profileChanges);
    public int? Player
    {
        get
        {
            int index = Volatile.Read(ref player);
            return index > 0 ? index : null;
        }
    }
    public void ApplyProfile(CoreInputMap map) => Volatile.Write(ref pendingMap, map);
    public Task Completion { get; }
    public string Status => Volatile.Read(ref status);
    public int Reports => Volatile.Read(ref reports);
    public int TruncatedReports => Volatile.Read(ref truncatedReports);
    public int UnknownReports => Volatile.Read(ref unknownReports);

    public ControllerSession(WiiDevice device, CoreInputMap map, bool collectLatency = false, SessionMode mode = SessionMode.Xbox)
        : this(device.Model, device.Open, static () => new XboxOutput(), map, collectLatency, mode)
    {
    }

    internal ControllerSession(string model, Func<IControllerTransport> openTransport, Func<IXboxOutput> openXbox,
        CoreInputMap map, bool collectLatency = false, SessionMode mode = SessionMode.Xbox, Func<InfraredMouse>? openMouse = null)
    {
        Model = model;
        this.openTransport = openTransport;
        this.openXbox = openXbox;
        this.openMouse = openMouse;
        this.mode = mode;
        activeMap = map;
        SetLatencyCapture(collectLatency);
        Completion = Task.Factory.StartNew(Run, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void Stop()
    {
        IControllerTransport? current;
        lock (lifecycle)
        {
            if (stopping) return;
            Volatile.Write(ref stopping, true);
            current = stream;
        }
        current?.Dispose(); // Unblocks the outstanding HID read.
    }

    private void Run()
    {
        IXboxOutput? output = null;
        IControllerTransport? input = null;
        InfraredMouse? mouse = null;
        bool transportOperation = true;
        try
        {
            if (UsesMouse) mouse = openMouse is null ? new InfraredMouse() : openMouse();
            var map = activeMap;
            input = openTransport();
            lock (lifecycle)
            {
                if (stopping) return;
                stream = input;
            }

            var buffer = new byte[input.InputLength];
            var protocol = new NunchukProtocol(enableExtension: UsesXbox);
            input.Send(WiiCommand.CoreMode);
            var infrared = UsesMouse ? new InfraredSetup(Stopwatch.GetTimestamp()) : null;
            if (infrared is not null) input.Send(InfraredCommand.Clock);
            else input.Send(WiiCommand.Status);
            var lastValidReport = Stopwatch.GetTimestamp();
            string lastAccessory = "";
            string lastMouseStatus = "";
            int indicatedPlayer = 0;
            bool checkDeadline = false;
            while (!Volatile.Read(ref stopping))
            {
                transportOperation = true;
                int timeout = checkDeadline
                    ? InputDeadline.RemainingMs(Stopwatch.GetElapsedTime(lastValidReport))
                    : InputDeadline.LimitMs;
                if (input.ReadTimeout != timeout) input.ReadTimeout = timeout;
                int count = input.Read(buffer);
                long received = Stopwatch.GetTimestamp();
                if (count == 0) throw new IOException("El mando cerró la conexión.");
                if (Volatile.Read(ref stopping)) break;
                transportOperation = false;
                var frame = buffer.AsSpan(0, count);
                bool configuringMouse = infrared is { Pending: true };
                var infraredCommand = configuringMouse ? infrared!.Process(frame, received) : null;
                var state = default(WiiInput);
                var command = WiiCommand.None;
                bool hasState = configuringMouse
                    ? InputMapper.TryParseCore(frame, out state)
                    : protocol.Process(frame, received, out state, out command);
                if (hasState)
                {
                    lastValidReport = received;
                    if (Volatile.Read(ref pendingMap) is not null &&
                        Interlocked.Exchange(ref pendingMap, null) is { } replacement)
                    {
                        output?.Submit(default);
                        map = replacement;
                        Volatile.Write(ref activeMap, replacement);
                        Interlocked.Increment(ref profileChanges);
                    }
                    bool initial = reports == 0;
                    if (UsesXbox)
                    {
                        output ??= openXbox();
                        var xboxState = map.Map(state, protocol.Calibration);
                        output.Submit(xboxState);
                        if (!initial && Volatile.Read(ref activeMetrics) is { } metrics)
                        {
                            long submitted = Stopwatch.GetTimestamp();
                            metrics.Record(received, submitted, (xboxState.Buttons & 0x1000) != 0);
                        }
                    }
                    if (infrared is { Enabled: true })
                        mouse!.Update(frame, state.Buttons, map.InfraredCalibration, Volatile.Read(ref calibrationCapture));
                    Buttons.Publish(state.Buttons, received);
                    int reportNumber = Interlocked.Increment(ref reports);
                    bool retryPlayer = output is not null && (player == 0 || player != indicatedPlayer) &&
                        (reportNumber is 32 or 128 || (reportNumber & 511) == 0);
                    if (initial || retryPlayer)
                    {
                        int slot = output?.Player ?? 0;
                        Volatile.Write(ref player, slot is >= 1 and <= 4 ? slot : 0);
                        if (Player is int led && led != indicatedPlayer && !Volatile.Read(ref stopping))
                        {
                            var indicate = led switch
                            {
                                1 => WiiCommand.PlayerLed1,
                                2 => WiiCommand.PlayerLed2,
                                3 => WiiCommand.PlayerLed3,
                                _ => WiiCommand.PlayerLed4
                            };
                            try
                            {
                                input.Send(indicate);
                                indicatedPlayer = led;
                            }
                            catch (Exception error) when (error is IOException or TimeoutException or ObjectDisposedException)
                            {
                                Debug.WriteLine(error);
                            }
                        }
                    }
                    var mouseStatus = infrared switch
                    {
                        null => "",
                        { Pending: true } => " · iniciando IR",
                        { Enabled: false } => " · IR no disponible",
                        _ => mouse!.Tracking ? " · ratón IR" : " · apunta a la barra IR"
                    };
                    if (initial || retryPlayer || lastAccessory != protocol.Accessory || lastMouseStatus != mouseStatus)
                    {
                        lastAccessory = protocol.Accessory;
                        lastMouseStatus = mouseStatus;
                        Volatile.Write(ref status, lastAccessory.Length == 0
                            ? "Activo" + mouseStatus : $"Activo · {lastAccessory}{mouseStatus}");
                    }
                }
                else
                {
                    switch (WiiReportValidation.ClassifyIgnored(frame))
                    {
                        case ReportRejection.Truncated:
                            Interlocked.Increment(ref truncatedReports);
                            break;
                        case ReportRejection.UnknownReport:
                            Interlocked.Increment(ref unknownReports);
                            break;
                    }
                }
                if (Volatile.Read(ref stopping)) break;
                checkDeadline = !hasState || infraredCommand.HasValue || configuringMouse || command != WiiCommand.None;
                transportOperation = true;
                if (infraredCommand is { } next)
                    input.Send(next);
                else if (configuringMouse && infrared is { Pending: false })
                {
                    if (!infrared.Enabled && mode == SessionMode.Mouse)
                        throw new IOException("El mando no confirmó la activación de la cámara IR.");
                    if (infrared.Enabled)
                    {
                        input.Send(WiiCommand.CoreMode, infrared: true);
                        Volatile.Write(ref infraredReady, 1);
                    }
                    input.Send(WiiCommand.Status);
                }
                else if (command != WiiCommand.None)
                    input.Send(command, infrared?.Enabled == true);
            }
        }
        catch (Exception error)
        {
            if (!Volatile.Read(ref stopping))
            {
                ReconnectRecommended = transportOperation && error is TimeoutException or IOException;
                XboxOutputUnavailable = !transportOperation && error is
                    Nefarius.ViGEm.Client.Exceptions.VigemBusNotFoundException or
                    Nefarius.ViGEm.Client.Exceptions.VigemBusAccessFailedException or
                    Nefarius.ViGEm.Client.Exceptions.VigemBusVersionMismatchException or
                    Nefarius.ViGEm.Client.Exceptions.VigemNoFreeSlotException;
                var message = error switch
                {
                    TimeoutException => "Se ha perdido la comunicación con el mando.",
                    Nefarius.ViGEm.Client.Exceptions.VigemBusNotFoundException or
                    Nefarius.ViGEm.Client.Exceptions.VigemBusAccessFailedException or
                    Nefarius.ViGEm.Client.Exceptions.VigemBusVersionMismatchException or
                    Nefarius.ViGEm.Client.Exceptions.VigemNoFreeSlotException => XboxOutput.DescribeDriverFailure(error),
                    _ => error.Message
                };
                Volatile.Write(ref status, message);
            }
        }
        finally
        {
            Volatile.Write(ref infraredReady, 0);
            lock (lifecycle) stream = null;
            try { mouse?.Dispose(); }
            catch (Exception error) { Debug.WriteLine(error); }
            if (output is not null)
            {
                try { output.Submit(default); }
                catch (Exception error) { Debug.WriteLine(error); }
                try { output.Dispose(); }
                catch (Exception error) { Debug.WriteLine(error); }
            }
            try { input?.Dispose(); }
            catch (Exception error) { Debug.WriteLine(error); }
            if (Volatile.Read(ref stopping)) Volatile.Write(ref status, "Detenido");
        }
    }
}
