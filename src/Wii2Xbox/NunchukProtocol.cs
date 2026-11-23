using System.Diagnostics;

namespace Wii2Xbox;

internal readonly record struct StickAxis(byte Minimum, byte Center, byte Maximum)
{
    public bool Valid => Center - Minimum >= 16 && Maximum - Center >= 16;

    public short Scale(byte value, int deadzone)
    {
        int delta = value - Center;
        int range = delta < 0 ? Center - Minimum : Maximum - Center;
        int threshold = Math.Min(deadzone, range - 1);
        if (Math.Abs(delta) <= threshold) return 0;
        int limit = delta < 0 ? 32768 : 32767;
        int result = Math.Min(limit, (Math.Abs(delta) - threshold) * limit / (range - threshold));
        return (short)(delta < 0 ? -result : result);
    }
}

internal readonly record struct StickCalibration(StickAxis X, StickAxis Y)
{
    public static StickCalibration Default { get; } = new(new(32, 128, 224), new(32, 128, 224));

    public static bool TryParse(ReadOnlySpan<byte> bytes, out StickCalibration calibration)
    {
        calibration = Default;
        if (bytes.Length < 16) return false;
        var x = new StickAxis(bytes[9], bytes[10], bytes[8]);
        var y = new StickAxis(bytes[12], bytes[13], bytes[11]);
        if (!x.Valid || !y.Valid) return false;
        calibration = new(x, y);
        return true;
    }
}

internal enum WiiCommand
{
    None, Status, CoreMode, ExtensionMode, Init, DisableEncryption, Identify, ReadCalibration,
    Rumble, PlayerLed1, PlayerLed2, PlayerLed3, PlayerLed4
}

internal sealed class NunchukProtocol(bool enableExtension = true)
{
    private enum Stage { Absent, ResumeCore, Init, Encryption, Identity, Calibration, Active, Unsupported }
    private Stage stage;
    private long deadline;
    public StickCalibration Calibration { get; private set; } = StickCalibration.Default;
    public bool UsingFallbackCalibration { get; private set; }
    private readonly string fallbackCalibration = UiText.Get("NunchukFallback");
    private readonly string unsupportedAccessory = UiText.Get("UnsupportedAccessory");
    private readonly string detectingAccessory = UiText.Get("DetectingAccessory");
    public string Accessory => stage switch
    {
        Stage.Active when UsingFallbackCalibration => fallbackCalibration,
        Stage.Active => "Nunchuk",
        Stage.Unsupported => unsupportedAccessory,
        Stage.Absent => "",
        _ => detectingAccessory
    };

    public bool Process(ReadOnlySpan<byte> report, long now, out WiiInput input, out WiiCommand command)
    {
        input = default;
        command = WiiCommand.None;
        if (report.Length < 3) return false;

        if (report[0] == 0x20 && report.Length >= 7)
        {
            if (!enableExtension || (report[3] & 2) == 0)
            {
                stage = Stage.Absent;
                command = WiiCommand.CoreMode;
            }
            else
            {
                if (stage == Stage.Absent) Advance(Stage.ResumeCore, now);
                command = stage == Stage.Active ? WiiCommand.ExtensionMode : WiiCommand.CoreMode;
            }
            // Status includes current core buttons. Releasing extension state
            // here prevents a held C/Z or stick from remaining after unplug.
            if (stage == Stage.Active) return false;
            input = InputMapper.ParseButtons(report);
            return true;
        }

        if (stage is >= Stage.ResumeCore and <= Stage.Calibration && now >= deadline)
        {
            if (stage == Stage.Calibration) Activate(StickCalibration.Default, fallback: true, out command);
            else Reject(out command);
        }
        else if (report[0] == 0x22 && report.Length >= 5 && report[3] == 0x16)
        {
            if (stage is Stage.Init or Stage.Encryption && report[4] != 0) Reject(out command);
            else if (stage == Stage.Init)
            {
                Advance(Stage.Encryption, now);
                command = WiiCommand.DisableEncryption;
            }
            else if (stage == Stage.Encryption)
            {
                Advance(Stage.Identity, now);
                command = WiiCommand.Identify;
            }
        }
        else if (report[0] == 0x21 && report.Length >= 6 && report[4] == 0)
        {
            if (stage == Stage.Identity && report[5] == 0xfa)
            {
                ReadOnlySpan<byte> nunchuk = [0, 0, 0xa4, 0x20, 0, 0];
                if (!TryRead(report, 6, out var identity) || !identity.SequenceEqual(nunchuk))
                    Reject(out command);
                else
                {
                    Advance(Stage.Calibration, now);
                    command = WiiCommand.ReadCalibration;
                }
            }
            else if (stage == Stage.Calibration && report[5] == 0x20)
            {
                var calibration = StickCalibration.Default;
                bool valid = TryRead(report, 16, out var bytes) && StickCalibration.TryParse(bytes, out calibration);
                Activate(calibration, !valid, out command);
            }
        }

        int extensionOffset = report[0] switch
        {
            0x30 => 0,
            0x32 when report.Length >= 11 => 3,
            0x36 when report.Length >= 22 => 13,
            _ => -1
        };
        if (extensionOffset < 0) return false;
        if (stage == Stage.ResumeCore)
        {
            Advance(Stage.Init, now);
            command = WiiCommand.Init;
        }
        input = InputMapper.ParseButtons(report);
        ReadOnlySpan<byte> noAccessory = [255, 255, 255, 255, 255, 255];
        if (extensionOffset != 0 && stage == Stage.Active)
        {
            var data = report.Slice(extensionOffset, 6);
            if (!data.SequenceEqual(noAccessory))
                input = new(input.Buttons, data[0], data[1], (data[5] & 2) == 0, (data[5] & 1) == 0, true);
        }
        return true;
    }

    private void Advance(Stage next, long now)
    {
        stage = next;
        deadline = now + Stopwatch.Frequency / 2;
    }

    private void Activate(StickCalibration calibration, bool fallback, out WiiCommand command)
    {
        Calibration = calibration;
        UsingFallbackCalibration = fallback;
        stage = Stage.Active;
        command = WiiCommand.ExtensionMode;
    }

    private void Reject(out WiiCommand command)
    {
        stage = Stage.Unsupported;
        command = WiiCommand.CoreMode;
    }

    private static bool TryRead(ReadOnlySpan<byte> report, int length, out ReadOnlySpan<byte> data)
    {
        data = default;
        if ((report[3] & 0x0f) != 0 || (report[3] >> 4) + 1 < length || report.Length < 6 + length)
            return false;
        data = report.Slice(6, length);
        return true;
    }
}
