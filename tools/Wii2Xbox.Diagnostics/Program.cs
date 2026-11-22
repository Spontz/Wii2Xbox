using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Wii2Xbox;

return XInputDiagnostics.Run(args);

internal static class XInputDiagnostics
{
    private const uint NotConnected = 1167;

    public static int Run(string[] args)
    {
        try
        {
            if (args.Length == 0 || args is ["list"])
            {
                for (uint slot = 0; slot < 4; slot++)
                {
                    var state = Read(slot);
                    Console.WriteLine(state is { } s
                        ? $"Xbox {slot + 1}: botones=0x{s.Buttons:X4}, LT={s.LeftTrigger}, RT={s.RightTrigger}, stick=({s.X},{s.Y})"
                        : $"Xbox {slot + 1}: sin conexión");
                }
                return 0;
            }
            if (args is ["capture", var player, var duration, var path] &&
                uint.TryParse(player, out uint slotNumber) && slotNumber is >= 1 and <= 4 &&
                int.TryParse(duration, out int seconds) && seconds is >= 1 and <= 120)
            {
                Capture(slotNumber, seconds, path);
                return 0;
            }
            if (args is ["self-test", var quantity] && int.TryParse(quantity, out int count) && count is >= 1 and <= 4)
            {
                SelfTest(count);
                return 0;
            }
            if (args is ["bluetooth", var time] && int.TryParse(time, out int scanSeconds) && scanSeconds is >= 3 and <= 120)
                return ObserveBluetooth(scanSeconds);
            if (args is ["driver"])
            {
                var status = XboxOutput.CheckDriver();
                Console.WriteLine(status.Message);
                return status.Ready ? 0 : 3;
            }
            Console.Error.WriteLine("Uso: Wii2Xbox.Diagnostics list | driver | capture <Xbox 1–4> <segundos 1–120> <archivo.json> | self-test <1–4> | bluetooth <segundos 3–120>");
            return 2;
        }
        catch (Nefarius.ViGEm.Client.Exceptions.VigemBusNotFoundException)
        {
            Console.Error.WriteLine("No se encontró ViGEmBus activo. Instálalo desde la publicación oficial y repite la autoprueba.");
            return 3;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }

    private static int ObserveBluetooth(int seconds)
    {
        using var scan = new CancellationTokenSource();
        bool interrupted = false;
        void Cancel(object? _, ConsoleCancelEventArgs eventArgs)
        {
            eventArgs.Cancel = true;
            Volatile.Write(ref interrupted, true);
            scan.Cancel();
        }

        var seen = new Dictionary<ulong, string>();
        var supported = new HashSet<ulong>();
        int passes = 0;
        int hidCount = -1;
        long deadline = Stopwatch.GetTimestamp() + seconds * Stopwatch.Frequency;
        Console.WriteLine("Pulsa SYNC en el mando. El diagnóstico solo observa Bluetooth y HID.");
        Console.CancelKeyPress += Cancel;
        try
        {
            do
            {
                var candidates = BluetoothService.Find(scan.Token);
                var interfaces = BluetoothHidIdentity.NintendoInterfaces().ToArray();
                var matched = interfaces.Select(device => BluetoothHidIdentity.FromInterface(device.DevicePath)).ToHashSet();
                foreach (var candidate in candidates)
                {
                    ulong address = candidate.Device.Address;
                    if (candidate.CanPair) supported.Add(address);
                    string stage = !candidate.CanPair ? "modelo Wii no compatible"
                        : matched.Contains(address) ? "interfaz HID disponible"
                        : candidate.Device.Connected != 0 ? "conectado, pendiente de HID"
                        : candidate.Device.Authenticated != 0 ? "asociado, sin conexión activa"
                        : "visible para asociación";
                    if (seen.TryGetValue(address, out var prior) && prior == stage) continue;
                    seen[address] = stage;
                    Console.WriteLine($"{candidate.Device.Name} · {address & 0xffff:X4}: {stage}");
                }
                if (interfaces.Length != hidCount)
                {
                    hidCount = interfaces.Length;
                    Console.WriteLine($"Interfaces HID Nintendo Wii publicadas por Windows: {hidCount}.");
                }
                passes++;
            } while (!scan.IsCancellationRequested && Stopwatch.GetTimestamp() < deadline);
        }
        catch (OperationCanceledException) when (scan.IsCancellationRequested) { }
        finally { Console.CancelKeyPress -= Cancel; }

        if (Volatile.Read(ref interrupted)) return 130;
        Console.WriteLine($"Búsquedas completadas: {passes}. Wii Remote compatibles: {supported.Count}. Otros modelos Wii: {seen.Count - supported.Count}.");
        return supported.Count > 0 || hidCount > 0 ? 0 : 4;
    }

    private static void SelfTest(int count)
    {
        var occupied = Enumerable.Range(0, 4).Where(index => Read((uint)index).HasValue).ToHashSet();
        if (4 - occupied.Count < count)
            throw new InvalidOperationException("No hay suficientes ranuras XInput libres. Desconecta otros mandos y repite.");
        var outputs = new List<XboxOutput>(count);
        var assigned = new HashSet<int>();
        try
        {
            for (int index = 0; index < count; index++)
            {
                var output = new XboxOutput();
                outputs.Add(output);
                WaitUntil(() => output.Player is >= 1 and <= 4, $"asignación de la salida {index + 1}");
                int slot = output.Player!.Value - 1;
                if (occupied.Contains(slot) || !assigned.Add(slot))
                    throw new InvalidOperationException($"Ranura Xbox {slot + 1} no exclusiva.");
                // ViGEm can assign a slot before PnP has exposed it to XInput.
                WaitUntil(() => Read((uint)slot).HasValue, $"disponibilidad de Xbox {slot + 1}");
            }

            var states = new XboxInput[count];
            for (int i = 0; i < count; i++)
            {
                states[i] = new XboxInput((ushort)(0x1000 << i), (short)(6000 + 1000 * i), (short)(-4000 - 500 * i),
                    (byte)(52 + 31 * i), (byte)(203 - 19 * i));
                outputs[i].Submit(states[i]);
            }
            for (int i = 0; i < count; i++)
            {
                uint slot = (uint)(outputs[i].Player!.Value - 1);
                var expected = states[i];
                var target = outputs[i];
                try
                {
                    WaitUntil(() =>
                    {
                        if (Read(slot) is { } state && Matches(state, expected)) return true;
                        target.Submit(expected);
                        return false;
                    }, $"estado de Xbox {slot + 1}");
                }
                catch (TimeoutException error)
                {
                    var last = Read(slot);
                    throw new TimeoutException($"{error.Message} Esperado: {expected}. Observado: " +
                        (last is { } observed ? $"botones=0x{observed.Buttons:X4}, LT={observed.LeftTrigger}, RT={observed.RightTrigger}, stick=({observed.X},{observed.Y})."
                            : "ranura desconectada."), error);
                }
            }

            for (int i = 0; i < count; i++)
            {
                int slot = outputs[i].Player!.Value - 1;
                if (Read((uint)slot) is not { } actual || !Matches(actual, states[i]))
                    throw new InvalidOperationException($"Xbox {slot + 1} cambió de estado al actualizar otra salida.");
            }

            for (int i = 0; i < count; i++)
            {
                uint slot = (uint)(outputs[i].Player!.Value - 1);
                outputs[i].Submit(default);
                WaitUntil(() => Read(slot) is { } state && Matches(state, default), $"liberación de Xbox {slot + 1}");
            }
        }
        finally
        {
            foreach (var output in outputs)
            {
                try { output.Submit(default); }
                catch (Exception error) { Console.Error.WriteLine($"No se pudo enviar neutro: {error.Message}"); }
                try { output.Dispose(); }
                catch (Exception error) { Console.Error.WriteLine($"No se pudo cerrar una salida: {error.Message}"); }
            }
        }
        foreach (int slot in assigned)
            WaitUntil(() => Read((uint)slot) is null, $"desconexión de Xbox {slot + 1}");
        Console.WriteLine($"Autoprueba correcta con {count} salidas Xbox virtuales: asignación, entrada, liberación y desconexión.");
    }

    private static bool Matches(State state, XboxInput expected) =>
        state.Buttons == expected.Buttons && state.X == expected.X && state.Y == expected.Y &&
        state.LeftTrigger == expected.LeftTrigger && state.RightTrigger == expected.RightTrigger &&
        state.RightX == 0 && state.RightY == 0;

    private static void WaitUntil(Func<bool> condition, string operation)
    {
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.ElapsedMilliseconds >= 2000)
                throw new TimeoutException($"XInput no confirmó {operation}.");
            Thread.Sleep(10);
        }
    }

    private static void Capture(uint player, int seconds, string path)
    {
        long frequency = Stopwatch.Frequency;
        long interval = frequency / 1000;
        var gaps = new List<long>(seconds * 1000);
        var edges = new List<Observation>();
        Console.WriteLine($"Midiendo Xbox {player} durante {seconds} s. Pulsa y suelta A dejando pausas entre cambios.");
        long start = Stopwatch.GetTimestamp();
        var first = Read(player - 1) ?? throw new InvalidOperationException($"Xbox {player} está desconectado.");
        long previousStart = start;
        long next = start + interval;
        bool previousA = IsA(first);
        bool initialA = previousA;
        long end = start;
        while (next < start + seconds * frequency)
        {
            while (Stopwatch.GetTimestamp() < next) Thread.SpinWait(16);
            long pollStart = Stopwatch.GetTimestamp();
            var state = Read(player - 1) ?? throw new InvalidOperationException($"Xbox {player} se desconectó durante la captura.");
            end = Stopwatch.GetTimestamp();
            gaps.Add(pollStart - previousStart);
            bool pressed = IsA(state);
            if (pressed != previousA)
                edges.Add(new Observation(previousStart, end, pressed));
            previousA = pressed;
            previousStart = pollStart;
            next = pollStart + interval;
        }
        gaps.Sort();
        double ms = 1000.0 / frequency;
        var report = new
        {
            SchemaVersion = 1, Measurement = "xinput-observer-xbox-a", Player = player,
            ClockFrequencyHz = frequency, StartTick = start, EndTick = end,
            InitialPressed = initialA, Polls = gaps.Count, NominalPollHz = 1000,
            ActualPollHz = gaps.Count * (double)frequency / (end - start),
            PollGapP95Ms = gaps[(int)Math.Ceiling(gaps.Count * .95) - 1] * ms,
            MaxPollGapMs = gaps[^1] * ms, Transitions = edges
        };
        using var file = new FileStream(path, FileMode.CreateNew);
        JsonSerializer.Serialize(file, report, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine($"Registrados {edges.Count} cambios en {path}.");
    }

    private static bool IsA(State state) => (state.Buttons & 0x1000) != 0;

    private static State? Read(uint slot)
    {
        uint result = XInputGetState(slot, out var state);
        return result switch
        {
            0 => state,
            NotConnected => null,
            _ => throw new InvalidOperationException($"XInputGetState devolvió {result} para la ranura {slot + 1}.")
        };
    }

    private readonly record struct Observation(long EarliestTick, long ObservedTick, bool Pressed);

    [StructLayout(LayoutKind.Sequential)]
    private struct State
    {
        public uint Packet;
        public ushort Buttons;
        public byte LeftTrigger, RightTrigger;
        public short X, Y, RightX, RightY;
    }

    [DllImport("xinput1_4.dll", ExactSpelling = true)]
    private static extern uint XInputGetState(uint userIndex, out State state);
}
