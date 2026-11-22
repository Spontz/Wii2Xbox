using System.Text.Json;

namespace Wii2Xbox;

[Flags]
public enum WiiButtons
{
    None=0, Left=1, Right=2, Down=4, Up=8, Plus=16,
    Two=256, One=512, B=1024, A=2048, Minus=4096, Home=32768
}

public readonly record struct WiiInput(WiiButtons Buttons, byte StickX=128, byte StickY=128, bool C=false, bool Z=false, bool HasNunchuk=false);

public sealed class Profile
{
    public int SchemaVersion { get; set; } = 1;
    public string Name { get; set; } = "Horizontal";
    public bool Horizontal { get; set; } = true;
    public int Deadzone { get; set; } = 12;
    public InfraredCalibration? InfraredCalibration { get; set; }
    public Dictionary<string,string> Bindings { get; set; } = new()
    {
        ["Two"]="A", ["One"]="B", ["A"]="X", ["B"]="Y", ["Plus"]="Start",
        ["Minus"]="Back", ["Home"]="Guide", ["C"]="LeftShoulder", ["Z"]="LeftTrigger"
    };
    public static IReadOnlyDictionary<string, ushort> XboxButtons { get; } =
        new System.Collections.ObjectModel.ReadOnlyDictionary<string, ushort>(new Dictionary<string, ushort>
    {
        ["Up"]=1,["Down"]=2,["Left"]=4,["Right"]=8,["Start"]=16,["Back"]=32,
        ["LeftThumb"]=64,["RightThumb"]=128,["LeftShoulder"]=256,["RightShoulder"]=512,
        ["Guide"]=1024,["A"]=4096,["B"]=8192,["X"]=16384,["Y"]=32768
    });
    internal static readonly string[] Sources =
        Enum.GetNames<WiiButtons>().Where(name => name != nameof(WiiButtons.None)).Concat(["C", "Z"]).ToArray();

    public void Validate()
    {
        if (SchemaVersion != 1)
            throw new InvalidDataException(UiText.Format("UnsupportedProfileVersion", SchemaVersion));
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 80 || Deadzone < 0 || Deadzone > 60 || Bindings is null)
            throw new InvalidDataException(UiText.Get("InvalidProfileNameOrZone"));
        if (InfraredCalibration is { } calibration && !calibration.IsValid)
            throw new InvalidDataException(UiText.Get("InvalidIrCalibration"));
        foreach (var (source,target) in Bindings)
            if (!Sources.Contains(source) ||
                !(target is not null && (XboxButtons.ContainsKey(target) || target is "LeftTrigger" or "RightTrigger" or "None")))
                throw new InvalidDataException(UiText.Format("InvalidBinding", source, target));
    }
    public static Profile Load(string path)
    {
        var p=JsonSerializer.Deserialize<Profile>(File.ReadAllText(path)) ?? throw new InvalidDataException(UiText.Get("EmptyProfile"));
        p.Validate(); return p;
    }
    public void Save(string path) => ProfileStore.Write(path, this);
}

public static class InputMapper
{
    public static bool TryParseCore(ReadOnlySpan<byte> report, out WiiInput input)
    {
        input = default;
        if (report.Length < 3 || report[0] != 0x30)
            return false;
        input = Parse(report);
        return true;
    }

    public static WiiInput Parse(ReadOnlySpan<byte> r)
    {
        if(r.Length<3) throw new InvalidDataException(UiText.Get("IncompleteWiiReport"));
        // Only button bits: accelerometer bits may occupy the other positions.
        var buttons = ParseButtons(r).Buttons;
        if(r[0]==0x32 && r.Length>=11)
        {
            var e=r.Slice(3,8);
            return new(buttons,e[0],e[1],(e[5]&2)==0,(e[5]&1)==0,true);
        }
        return new(buttons);
    }
    internal static WiiInput ParseButtons(ReadOnlySpan<byte> report) =>
        new((WiiButtons)((report[1] & 0x1f) | ((report[2] & 0x9f) << 8)));
    public static XboxInput Map(WiiInput input, Profile p)
    {
        ushort bits=0; byte lt=0,rt=0;
        // Physical D-pad is rotated for a Wii Remote held horizontally.
        if(input.Buttons.HasFlag(WiiButtons.Left) && !p.Bindings.ContainsKey("Left")) bits|=(ushort)(p.Horizontal?2:4);
        if(input.Buttons.HasFlag(WiiButtons.Right) && !p.Bindings.ContainsKey("Right")) bits|=(ushort)(p.Horizontal?1:8);
        if(input.Buttons.HasFlag(WiiButtons.Up) && !p.Bindings.ContainsKey("Up")) bits|=(ushort)(p.Horizontal?4:1);
        if(input.Buttons.HasFlag(WiiButtons.Down) && !p.Bindings.ContainsKey("Down")) bits|=(ushort)(p.Horizontal?8:2);
        foreach(var (source,target) in p.Bindings)
        {
            bool pressed=source switch { "C"=>input.HasNunchuk&&input.C,"Z"=>input.HasNunchuk&&input.Z,
                _=>Enum.TryParse<WiiButtons>(source,out var b)&&input.Buttons.HasFlag(b) };
            if(!pressed) continue;
            if(target=="LeftTrigger") lt=255;
            else if(target=="RightTrigger") rt=255;
            else if(Profile.XboxButtons.TryGetValue(target,out var v)) bits|=v;
        }
        if ((bits & 3) == 3) bits &= 0xfffc;
        if ((bits & 12) == 12) bits &= 0xfff3;
        return new(bits,input.HasNunchuk?Axis(input.StickX,p.Deadzone):(short)0,input.HasNunchuk?Axis(input.StickY,p.Deadzone):(short)0,lt,rt);
    }
    public static short Axis(byte value,int deadzone)
    {
        int d=value-128;
        if(Math.Abs(d)<=deadzone) return 0;
        return (short)Math.Clamp(Math.Sign(d)*(Math.Abs(d)-deadzone)*32767/(127-deadzone),short.MinValue,short.MaxValue);
    }
}

internal sealed class CoreInputMap
{
    // The core report has 11 button bits. Build the mapping before opening the
    // device so each incoming report needs only a lookup, with no string parsing.
    private readonly XboxInput[] states = new XboxInput[1 << 11];
    private readonly XboxInput[] extensionStates = new XboxInput[1 << 13];
    private readonly int deadzone;
    public string Name { get; }
    public bool Horizontal { get; }
    public InfraredCalibration InfraredCalibration { get; }

    public CoreInputMap(Profile profile)
    {
        profile.Validate();
        Name = profile.Name;
        Horizontal = profile.Horizontal;
        InfraredCalibration = profile.InfraredCalibration ?? Wii2Xbox.InfraredCalibration.Default;
        deadzone = profile.Deadzone;
        for (int index = 0; index < states.Length; index++)
        {
            var buttons = (WiiButtons)((index & 0x1f) | ((index & 0x3e0) << 3) | ((index & 0x400) << 5));
            states[index] = InputMapper.Map(new WiiInput(buttons), profile);
            for (int extension = 0; extension < 4; extension++)
                extensionStates[index + (extension << 11)] = InputMapper.Map(
                    new WiiInput(buttons, C: (extension & 1) != 0, Z: (extension & 2) != 0, HasNunchuk: true), profile);
        }
    }

    public XboxInput Map(WiiButtons input)
    {
        return states[Index(input)];
    }

    public XboxInput Map(WiiInput input, StickCalibration calibration)
    {
        if (!input.HasNunchuk) return Map(input.Buttons);
        int extension = (input.C ? 1 : 0) | (input.Z ? 2 : 0);
        return extensionStates[Index(input.Buttons) + (extension << 11)] with
        {
            X = calibration.X.Scale(input.StickX, deadzone),
            Y = calibration.Y.Scale(input.StickY, deadzone)
        };
    }

    private static int Index(WiiButtons input)
    {
        int bits = (int)input;
        return (bits & 0x1f) | ((bits >> 3) & 0x3e0) | ((bits >> 5) & 0x400);
    }
}
