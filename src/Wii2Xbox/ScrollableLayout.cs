namespace Wii2Xbox;

internal sealed class ScrollableLayout : Panel
{
    private readonly TableLayoutPanel content;
    private readonly int fillRow;
    private readonly int minimumFillHeight;
    private readonly Label[] wrappingLabels;
    private bool fitting;

    internal ScrollableLayout(TableLayoutPanel content, int fillRow, int minimumFillHeight, params Label[] wrappingLabels)
    {
        this.content = content;
        this.fillRow = fillRow;
        this.minimumFillHeight = minimumFillHeight;
        this.wrappingLabels = wrappingLabels;
        Dock = DockStyle.Fill;
        AutoScroll = true;
        content.AutoSize = true;
        content.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        content.Dock = DockStyle.Top;
        content.RowStyles[fillRow].SizeType = SizeType.Absolute;
        content.RowStyles[fillRow].Height = minimumFillHeight;
        Controls.Add(content);
        content.SizeChanged += (_, _) => FitContent();
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        FitContent();
    }

    private void FitContent()
    {
        if (fitting || content is null || ClientSize.Width <= 0) return;
        fitting = true;
        try
        {
            int wrapWidth = Math.Max(1, ClientSize.Width - content.Padding.Horizontal);
            foreach (var label in wrappingLabels)
                label.MaximumSize = new Size(Math.Max(1, wrapWidth - label.Margin.Horizontal), 0);

            var rows = content.GetRowHeights();
            if (rows.Length <= fillRow) return;
            int otherHeight = content.Height - rows[fillRow];
            int height = Math.Max(LogicalToDeviceUnits(minimumFillHeight), ClientSize.Height - otherHeight);
            if (content.RowStyles[fillRow].Height != height)
                content.RowStyles[fillRow].Height = height;
        }
        finally { fitting = false; }
    }

    internal static void ConfigureForm(Form form, Size initialClientSize, Size minimumWindowSize, bool expandOnLargeDisplays = false)
    {
        form.AutoScaleDimensions = new SizeF(96, 96);
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.ClientSize = initialClientSize;
        form.MinimumSize = minimumWindowSize;
        form.Load += (_, _) =>
        {
            if (expandOnLargeDisplays && form.WindowState == FormWindowState.Normal)
            {
                var area = Screen.FromControl(form.Owner ?? form).WorkingArea;
                var frame = form.Size - form.ClientSize;
                var expanded = PreferredClientSize(form.ClientSize, area.Size - frame);
                if (expanded != form.ClientSize)
                {
                    var previous = form.Bounds;
                    form.ClientSize = expanded;
                    if (form.StartPosition != FormStartPosition.Manual)
                        form.Location = new Point(
                            Math.Clamp(previous.Left - (form.Width - previous.Width) / 2, area.Left, Math.Max(area.Left, area.Right - form.Width)),
                            Math.Clamp(previous.Top - (form.Height - previous.Height) / 2, area.Top, Math.Max(area.Top, area.Bottom - form.Height)));
                }
            }
            FitWorkingArea();
        };
        form.DpiChanged += (_, _) => form.BeginInvoke(FitWorkingArea);

        void FitWorkingArea()
        {
            var area = Screen.FromControl(form).WorkingArea;
            var minimum = form.LogicalToDeviceUnits(minimumWindowSize);
            form.MinimumSize = new Size(Math.Min(minimum.Width, area.Width), Math.Min(minimum.Height, area.Height));
            if (form.WindowState == FormWindowState.Normal)
                form.Size = new Size(Math.Min(form.Width, area.Width), Math.Min(form.Height, area.Height));
        }
    }

    internal static Size PreferredClientSize(Size baseline, Size available)
    {
        int width = Math.Clamp(Math.Max(baseline.Width, available.Width * 28 / 100), 1, Math.Max(1, available.Width));
        int height = baseline.Height + Math.Max(0, width - baseline.Width) / 2;
        return new Size(width, Math.Clamp(height, 1, Math.Max(1, available.Height)));
    }

    internal static void FitChoices(ComboBox combo, int minimumWidth)
    {
        combo.FontChanged += (_, _) => Resize();
        combo.DpiChangedAfterParent += (_, _) => Resize();
        Resize();

        void Resize()
        {
            int textWidth = combo.Items.Cast<string>().Max(value => TextRenderer.MeasureText(value, combo.Font).Width);
            int width = Math.Max(combo.LogicalToDeviceUnits(minimumWidth), textWidth + combo.LogicalToDeviceUnits(24));
            combo.MinimumSize = new Size(0, combo.PreferredHeight);
            combo.Size = new Size(width, combo.PreferredHeight);
        }
    }
}
