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
    private readonly Bitmap nunchukArtwork;
    private readonly int[] counts = new int[WiiButtonState.BitCount];
    private int litPlayers;
    private bool showNunchuk;
    private Vector2 stickDirection;

    public WiiRemoteView()
    {
        artwork = LoadArtwork("Wii2Xbox.WiiRemoteImage");
        nunchukArtwork = LoadArtwork("Wii2Xbox.NunchukImage");
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
        bool hasNunchuk = false;
        Vector2 strongestStick = Vector2.Zero;
        long cutoff = Stopwatch.GetTimestamp() - PulseTicks;
        foreach (var session in sessions)
        {
            if (session.Completion.IsCompleted) continue;
            bool active = session.Buttons.Accumulate(cutoff, next, out bool attached, out var stick);
            hasNunchuk |= attached;
            if (stick.LengthSquared() > strongestStick.LengthSquared()) strongestStick = stick;
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

        if (lit == litPlayers && hasNunchuk == showNunchuk && strongestStick == stickDirection
            && next.SequenceEqual(counts)) return;
        next.CopyTo(counts);
        litPlayers = lit;
        showNunchuk = hasNunchuk;
        stickDirection = strongestStick;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        const float artworkWidth = 600;
        const float triggerSpace = 106;
        const float nunchukSpace = 178;
        float drawingWidth = artworkWidth + triggerSpace + (showNunchuk ? nunchukSpace : 0);
        float drawingHeight = artworkWidth * ArtworkCrop.Height / ArtworkCrop.Width;
        float scale = Math.Min(Width / drawingWidth, Height / drawingHeight);
        if (scale <= 0) return;

        var canvas = e.Graphics;
        canvas.TranslateTransform((Width - drawingWidth * scale) / 2, (Height - drawingHeight * scale) / 2);
        canvas.ScaleTransform(scale, scale);
        canvas.TranslateTransform(triggerSpace, 0);
        canvas.SmoothingMode = SmoothingMode.AntiAlias;
        canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
        float imageScale = artworkWidth / ArtworkCrop.Width;
        canvas.DrawImage(artwork, new RectangleF(0, 0, artworkWidth, drawingHeight),
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

        var trigger = new RectangleF(-triggerSpace + 3, (drawingHeight - 24) / 2, 92, 24);
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
        }

        if (showNunchuk)
        {
            float size = Math.Min(nunchukSpace - 12, drawingHeight);
            float left = artworkWidth + (nunchukSpace - size) / 2;
            float top = (drawingHeight - size) / 2;
            canvas.DrawImage(nunchukArtwork, left, top, size, size);
            using var transform = new Matrix(size / nunchukArtwork.Width, 0, 0, size / nunchukArtwork.Height, left, top);
            using var buttonText = new SolidBrush(Color.FromArgb(43, 49, 58));
            if (counts[WiiButtonState.StickIndex] > 0)
            {
                using var stick = new GraphicsPath();
                stick.AddEllipse(278, 138, 223, 74);
                HighlightNunchukControl(WiiButtonState.StickIndex, stick);
                if (stickDirection != Vector2.Zero) DrawStickArrow(stick.GetBounds());
            }
            if (counts[WiiButtonState.CIndex] > 0)
            {
                using var button = new GraphicsPath();
                button.AddBeziers([
                    new Point(89, 481), new Point(106, 446), new Point(145, 453), new Point(164, 478),
                    new Point(181, 500), new Point(185, 542), new Point(162, 561),
                    new Point(139, 578), new Point(105, 566), new Point(91, 543),
                    new Point(80, 525), new Point(80, 501), new Point(89, 481)
                ]);
                button.CloseFigure();
                HighlightNunchukControl(WiiButtonState.CIndex, button);
            }
            if (counts[WiiButtonState.ZIndex] > 0)
            {
                using var triggerSurface = new GraphicsPath();
                triggerSurface.AddBeziers([
                    new Point(113, 585), new Point(140, 603), new Point(192, 603), new Point(226, 594),
                    new Point(240, 590), new Point(244, 602), new Point(249, 615),
                    new Point(256, 634), new Point(265, 655), new Point(270, 673),
                    new Point(274, 688), new Point(249, 695), new Point(231, 701),
                    new Point(202, 710), new Point(168, 709), new Point(151, 697),
                    new Point(134, 685), new Point(116, 633), new Point(108, 613),
                    new Point(103, 600), new Point(106, 589), new Point(113, 585)
                ]);
                triggerSurface.CloseFigure();
                HighlightNunchukControl(WiiButtonState.ZIndex, triggerSurface);
            }

            DrawNunchukLabel("C", WiiButtonState.CIndex, 116, 518);
            DrawNunchukLabel("Z", WiiButtonState.ZIndex, 180, 659);

            void DrawNunchukLabel(string text, int index, float x, float y)
            {
                var position = new PointF(left + x * size / nunchukArtwork.Width, top + y * size / nunchukArtwork.Height);
                canvas.DrawString(text, font, counts[index] != 0 ? litLabel : buttonText, position, align);
            }

            void HighlightNunchukControl(int index, GraphicsPath shape)
            {
                shape.Transform(transform);
                canvas.FillPath(wash, shape);
                canvas.DrawPath(edge, shape);
                DrawBadge(counts[index], shape.GetBounds());
            }

            void DrawStickArrow(RectangleF cap)
            {
                var center = new Vector2(cap.X + cap.Width / 2, cap.Y + cap.Height / 2);
                var forward = Vector2.Normalize(new Vector2(stickDirection.X, -stickDirection.Y));
                var side = new Vector2(-forward.Y, forward.X);
                PointF[] arrow =
                [
                    ArrowPoint(9, 0), ArrowPoint(1, 5), ArrowPoint(1, 2), ArrowPoint(-7, 2),
                    ArrowPoint(-7, -2), ArrowPoint(1, -2), ArrowPoint(1, -5)
                ];
                using var outline = new Pen(highContrast ? SystemColors.Highlight : Color.White, 1.2f);
                outline.LineJoin = LineJoin.Round;
                canvas.DrawPolygon(outline, arrow);
                canvas.FillPolygon(litLabel, arrow);

                PointF ArrowPoint(float along, float across)
                {
                    var point = center + forward * along + side * across;
                    return new PointF(point.X, point.Y);
                }
            }
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

    private static Bitmap LoadArtwork(string resourceName)
    {
        using var resource = typeof(WiiRemoteView).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(UiText.Get("MissingViewerImage"));
        using var image = Image.FromStream(resource);
        return new Bitmap(image);
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
        if (disposing)
        {
            artwork.Dispose();
            nunchukArtwork.Dispose();
        }
        base.Dispose(disposing);
    }
}
