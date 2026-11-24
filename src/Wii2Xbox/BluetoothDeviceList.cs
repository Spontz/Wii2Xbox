using System.Drawing.Drawing2D;
using DeviceKind = Wii2Xbox.BluetoothService.DeviceKind;

namespace Wii2Xbox;

internal sealed class BluetoothDeviceList : ListBox
{
    private const TextFormatFlags LabelFlags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding |
        TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;

    public BluetoothDeviceList()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        IntegralHeight = false;
        UpdateItemHeight();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        UpdateItemHeight();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        UpdateItemHeight();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        UpdateItemHeight();
    }

    private void UpdateItemHeight() =>
        ItemHeight = Math.Min(255, Math.Max(LogicalToDeviceUnits(48), 2 * Font.Height + LogicalToDeviceUnits(12)));

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        var canvas = e.Graphics;
        using var background = new SolidBrush(BackColor);
        canvas.FillRectangle(background, e.Bounds);
        if (e.Index < 0 || e.Index >= Items.Count || Items[e.Index] is not BluetoothService.Candidate device) return;

        var pill = Rectangle.Inflate(e.Bounds, -LogicalToDeviceUnits(5), -LogicalToDeviceUnits(3));
        int iconSize = LogicalToDeviceUnits(20);
        int textLeft = pill.Left + LogicalToDeviceUnits(19) + iconSize;
        int textWidth = pill.Right - LogicalToDeviceUnits(10) - textLeft;
        if (textWidth < 1 || pill.Height < 1) return;

        bool focused = (e.State & DrawItemState.Focus) != 0;
        bool selected = Enabled && (e.State & DrawItemState.Selected) != 0;
        bool highContrast = SystemInformation.HighContrast;
        bool dark = BackColor.GetBrightness() < 0.5f;
        Color surface = dark ? Color.FromArgb(58, 58, 58) : Color.FromArgb(238, 241, 245);
        Color ink = Enabled ? ForeColor : SystemColors.GrayText;
        if (highContrast) surface = SystemColors.Window;
        if (selected)
        {
            surface = highContrast ? SystemColors.Highlight : dark ? Color.FromArgb(38, 91, 154) : Color.FromArgb(218, 234, 252);
            ink = highContrast ? SystemColors.HighlightText : dark ? Color.White : Color.FromArgb(20, 57, 104);
        }
        var corners = new Size(pill.Height, pill.Height);
        canvas.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(surface);
        canvas.FillRoundedRectangle(fill, pill, corners);
        if (focused || highContrast && !selected)
        {
            using var outline = new Pen(ink);
            canvas.DrawRoundedRectangle(outline, pill, corners);
        }

        int middle = pill.Top + pill.Height / 2;
        DrawDeviceIcon(canvas, device.Kind,
            new Rectangle(pill.Left + LogicalToDeviceUnits(11), middle - iconSize / 2, iconSize, iconSize), ink);
        var name = new Rectangle(textLeft, middle - Font.Height, textWidth, Font.Height);
        var address = new Rectangle(textLeft, middle, textWidth, Font.Height);
        Color addressInk = highContrast || selected || !Enabled ? ink : dark ? Color.LightGray : Color.FromArgb(71, 85, 105);
        TextRenderer.DrawText(canvas, device.DisplayName, Font, name, ink, LabelFlags);
        TextRenderer.DrawText(canvas, device.Device.Address.ToString("X12"), Font, address, addressInk, LabelFlags);
    }

    private static void DrawDeviceIcon(Graphics canvas, DeviceKind kind, Rectangle bounds, Color color)
    {
        var state = canvas.Save();
        canvas.TranslateTransform(bounds.Left, bounds.Top);
        canvas.ScaleTransform(bounds.Width / 20f, bounds.Height / 20f);
        using var pen = new Pen(color, 1.5f)
        {
            StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round
        };
        switch (kind)
        {
            case DeviceKind.Phone:
                canvas.DrawRoundedRectangle(pen, new Rectangle(5, 2, 10, 16), new Size(4, 4));
                canvas.DrawLine(pen, 9, 15, 11, 15);
                break;
            case DeviceKind.Computer:
                canvas.DrawRectangle(pen, 4, 4, 12, 9);
                canvas.DrawLine(pen, 2, 16, 18, 16);
                break;
            case DeviceKind.Display:
                canvas.DrawRectangle(pen, 2, 3, 16, 11);
                canvas.DrawLine(pen, 10, 14, 10, 17);
                canvas.DrawLine(pen, 7, 17, 13, 17);
                break;
            case DeviceKind.Headphones:
                canvas.DrawArc(pen, 3, 3, 14, 14, 180, 180);
                canvas.DrawRectangle(pen, 3, 10, 3, 6);
                canvas.DrawRectangle(pen, 14, 10, 3, 6);
                break;
            case DeviceKind.Speaker:
                canvas.DrawPolygon(pen, [new Point(2, 8), new Point(5, 8), new Point(10, 4),
                    new Point(10, 16), new Point(5, 12), new Point(2, 12)]);
                canvas.DrawArc(pen, 7, 4, 10, 12, -55, 110);
                break;
            case DeviceKind.Gamepad:
                canvas.DrawRoundedRectangle(pen, new Rectangle(2, 5, 16, 10), new Size(7, 7));
                canvas.DrawLine(pen, 5, 10, 9, 10);
                canvas.DrawLine(pen, 7, 8, 7, 12);
                canvas.DrawEllipse(pen, 13, 9, 2, 2);
                break;
            case DeviceKind.Keyboard:
                canvas.DrawRoundedRectangle(pen, new Rectangle(2, 5, 16, 10), new Size(3, 3));
                canvas.DrawLine(pen, 5, 8, 15, 8);
                canvas.DrawLine(pen, 7, 12, 13, 12);
                break;
            case DeviceKind.Mouse:
                canvas.DrawRoundedRectangle(pen, new Rectangle(5, 2, 10, 16), new Size(8, 8));
                canvas.DrawLine(pen, 10, 5, 10, 8);
                break;
            default:
                canvas.DrawLines(pen, [new Point(6, 6), new Point(14, 14), new Point(10, 18),
                    new Point(10, 2), new Point(14, 6), new Point(6, 14)]);
                break;
        }
        canvas.Restore(state);
    }
}
