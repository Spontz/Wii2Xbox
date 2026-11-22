using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Numerics;

namespace Wii2Xbox;

internal sealed class WiiRemoteView : Control
{
    private static readonly long PulseTicks = Stopwatch.Frequency * 120 / 1000;
    private static readonly RectangleF ArtworkCrop = new(0, 90, 2172, 560);
    private static readonly (WiiButtons Button, Rectangle Pixels, bool Round)[] Hotspots =
    [
        (WiiButtons.Up, new(224, 324, 92, 80), false),
        (WiiButtons.Down, new(406, 324, 94, 80), false),
        (WiiButtons.Right, new(321, 226, 83, 92), false),
        (WiiButtons.Left, new(321, 413, 83, 89), false),
        (WiiButtons.A, new(619, 271, 184, 189), true),
        (WiiButtons.Plus, new(1013, 172, 105, 105), true),
        (WiiButtons.Home, new(1013, 310, 105, 105), true),
        (WiiButtons.Minus, new(1014, 450, 105, 107), true),
        (WiiButtons.One, new(1542, 305, 128, 128), true),
        (WiiButtons.Two, new(1756, 305, 128, 128), true)
    ];

    private readonly Bitmap artwork;
    private readonly int[] counts = new int[WiiButtonState.BitCount];
    private int litPlayers;

    public WiiRemoteView()
    {
        using var resource = typeof(WiiRemoteView).Assembly.GetManifestResourceStream("Wii2Xbox.WiiRemoteImage")
            ?? throw new InvalidOperationException(UiText.Get("MissingViewerImage"));
        using var image = Image.FromStream(resource);
        artwork = new Bitmap(image);
        AccessibleName = UiText.Get("WiiRemoteButtons");
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Margin = new Padding(0, 3, 0, 0);
    }

    public void UpdateButtons(IEnumerable<ControllerSession> sessions)
    {
        Span<int> next = stackalloc int[WiiButtonState.BitCount];
        next.Clear();
        Span<bool> withoutSlot = stackalloc bool[4];
        int withoutSlotCount = 0;
        int assigned = 0;
        int lit = 0;
        long cutoff = Stopwatch.GetTimestamp() - PulseTicks;
        foreach (var session in sessions)
        {
            if (session.Completion.IsCompleted) continue;
            bool active = session.Buttons.Accumulate(cutoff, next);
            if (session.Player is int player)
            {
                int slot = 1 << (player - 1);
                assigned |= slot;
                if (active) lit |= slot;
            }
            else if (session.Reports > 0 && withoutSlotCount < withoutSlot.Length)
            {
                withoutSlot[withoutSlotCount++] = active;
            }
        }

        int available = ~assigned & 0xF;
        for (int index = 0; index < withoutSlotCount && available != 0; index++)
        {
            int slot = available & -available;
            if (withoutSlot[index]) lit |= slot;
            available &= ~slot;
        }

        if (lit == litPlayers && next.SequenceEqual(counts)) return;
        next.CopyTo(counts);
        litPlayers = lit;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        const float drawingWidth = 600;
        const float drawingHeight = 184;
        float scale = Math.Min(Width / drawingWidth, Height / drawingHeight);
        if (scale <= 0) return;

        var canvas = e.Graphics;
        canvas.TranslateTransform((Width - drawingWidth * scale) / 2, (Height - drawingHeight * scale) / 2);
        canvas.ScaleTransform(scale, scale);
        canvas.SmoothingMode = SmoothingMode.AntiAlias;
        canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
        float imageScale = drawingWidth / ArtworkCrop.Width;
        canvas.DrawImage(artwork, new RectangleF(0, 0, drawingWidth, ArtworkCrop.Height * imageScale),
            ArtworkCrop, GraphicsUnit.Pixel);

        bool highContrast = SystemInformation.HighContrast;
        Color accent = highContrast ? SystemColors.Highlight : Color.FromArgb(10, 243, 116);
        using var wash = new SolidBrush(Color.FromArgb(115, accent));
        using var edge = new Pen(highContrast ? SystemColors.HighlightText : Color.FromArgb(28, 124, 73), 2.5f);
        using var label = new SolidBrush(ForeColor);
        using var litLabel = new SolidBrush(highContrast ? SystemColors.HighlightText : Color.FromArgb(10, 51, 30));
        using var badge = new SolidBrush(highContrast ? SystemColors.Highlight : Color.FromArgb(21, 92, 55));
        using var badgeText = new SolidBrush(highContrast ? SystemColors.HighlightText : Color.White);
        using var font = new Font("Segoe UI", 12, FontStyle.Bold, GraphicsUnit.Pixel);
        using var ledFont = new Font("Segoe UI", 9, FontStyle.Bold, GraphicsUnit.Pixel);
        using var align = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        foreach (var (button, pixels, round) in Hotspots)
        {
            int total = Count(button);
            if (total == 0) continue;
            var area = new RectangleF(pixels.X * imageScale, (pixels.Y - ArtworkCrop.Y) * imageScale,
                pixels.Width * imageScale, pixels.Height * imageScale);
            if (round)
            {
                canvas.FillEllipse(wash, area);
                canvas.DrawEllipse(edge, area);
            }
            else
            {
                using var pad = RoundedRectangle(area, 4);
                canvas.FillPath(wash, pad);
                canvas.DrawPath(edge, pad);
            }
            DrawBadge(total, area);
        }

        var trigger = new RectangleF(147, 157, 100, 24);
        int triggerCount = Count(WiiButtons.B);
        using var triggerShape = RoundedRectangle(trigger, 12);
        using var triggerEdge = new Pen(ForeColor);
        if (triggerCount > 0)
        {
            using var triggerFill = new SolidBrush(accent);
            canvas.FillPath(triggerFill, triggerShape);
        }
        canvas.DrawPath(triggerCount > 0 ? edge : triggerEdge, triggerShape);
        canvas.DrawString(UiText.Get("RearButton"), font, triggerCount > 0 ? litLabel : label, trigger, align);
        DrawBadge(triggerCount, trigger);

        using var led = new SolidBrush(highContrast ? SystemColors.Highlight : Color.FromArgb(36, 201, 255));
        using var ledGlow = new SolidBrush(Color.FromArgb(90, led.Color));
        using var ledLabel = new SolidBrush(Color.FromArgb(37, 52, 70));
        for (int player = 0; player < 4; player++)
        {
            float x = 1988 * imageScale;
            float y = (463 - player * 73 - ArtworkCrop.Y) * imageScale;
            var diode = new RectangleF(x, y, 32 * imageScale, 24 * imageScale);
            if ((litPlayers & (1 << player)) != 0)
            {
                var halo = RectangleF.Inflate(diode, 3, 3);
                canvas.FillEllipse(ledGlow, halo);
                using var indicator = RoundedRectangle(diode, 1.5f);
                canvas.FillPath(led, indicator);
            }
            var number = new RectangleF(diode.Right + 2, diode.Y - 2, 12, diode.Height + 4);
            canvas.DrawString((player + 1).ToString(), ledFont, ledLabel, number, align);
        }

        int Count(WiiButtons button) => counts[BitOperations.TrailingZeroCount((uint)button)];

        void DrawBadge(int total, RectangleF area)
        {
            if (total < 2) return;
            var marker = new RectangleF(area.Right - 8, area.Top - 5, 17, 17);
            canvas.FillEllipse(badge, marker);
            canvas.DrawString(total.ToString(), font, badgeText, marker, align);
        }
    }

    private static GraphicsPath RoundedRectangle(RectangleF area, float radius)
    {
        float diameter = radius * 2;
        var shape = new GraphicsPath();
        shape.AddArc(area.Left, area.Top, diameter, diameter, 180, 90);
        shape.AddArc(area.Right - diameter, area.Top, diameter, diameter, 270, 90);
        shape.AddArc(area.Right - diameter, area.Bottom - diameter, diameter, diameter, 0, 90);
        shape.AddArc(area.Left, area.Bottom - diameter, diameter, diameter, 90, 90);
        shape.CloseFigure();
        return shape;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) artwork.Dispose();
        base.Dispose(disposing);
    }
}
