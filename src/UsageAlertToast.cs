using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace CodexCreditMonitor;

internal sealed class UsageAlertToast : Form
{
    private readonly System.Windows.Forms.Timer _dismissTimer = new() { Interval = 12_000 };

    public UsageAlertToast(double percent)
    {
        var urgent = percent >= 90;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(23, 36, 53);
        ClientSize = new Size(388, 174);
        ControlBox = false;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Region = new Region(RoundedPath(new Rectangle(0, 0, Width, Height), 16));
        Paint += (_, eventArgs) => DrawCard(eventArgs.Graphics, urgent);

        Location = LocationNearSystemTray();

        var icon = NewLabel("!", 17, FontStyle.Bold, urgent ? Color.FromArgb(255, 174, 174) : Color.FromArgb(255, 210, 112));
        icon.Location = new Point(23, 18);
        icon.Size = new Size(26, 28);
        icon.TextAlign = ContentAlignment.MiddleCenter;
        var title = NewLabel(Ui.T("VERBRUIKSWAARSCHUWING", "USAGE WARNING"), 9, FontStyle.Bold, Color.FromArgb(177, 198, 223));
        title.Location = new Point(60, 17);
        var close = NewLabel("×", 17, FontStyle.Regular, Color.FromArgb(171, 190, 212));
        close.Location = new Point(348, 13);
        close.Size = new Size(22, 25);
        close.TextAlign = ContentAlignment.MiddleCenter;
        close.Cursor = Cursors.Hand;
        close.Click += (_, _) => Close();

        var percentage = NewLabel($"{percent:0}%", 29, FontStyle.Bold, Color.White);
        percentage.Location = new Point(21, 54);
        var label = NewLabel(Ui.T("HUIDIG 5-UURSVENSTER", "CURRENT 5-HOUR WINDOW"), 9, FontStyle.Regular, Color.FromArgb(177, 198, 223));
        label.Location = new Point(150, 58);
        label.AutoSize = false;
        label.Size = new Size(200, 18);
        var state = NewLabel(Ui.T($"{percent:0}% verbruikt", $"{percent:0}% used"), 12, FontStyle.Bold, urgent ? Color.FromArgb(255, 174, 174) : Color.FromArgb(255, 210, 112));
        state.Location = new Point(150, 79);
        var detail = NewLabel(
            urgent ? Ui.T("Bijna op. Pauzeer intensieve taken om onderbreking te voorkomen.", "Nearly exhausted. Pause intensive tasks to avoid interruption.") : Ui.T("Je nadert de limiet. Houd je resterende capaciteit in de gaten.", "You are approaching the limit. Keep an eye on your remaining capacity."),
            9,
            FontStyle.Regular,
            Color.FromArgb(170, 193, 219));
        detail.Location = new Point(23, 132);
        detail.AutoSize = false;
        detail.Size = new Size(340, 24);
        Controls.AddRange([icon, title, close, percentage, label, state, detail]);

        _dismissTimer.Tick += (_, _) => Close();
        Shown += (_, _) => _dismissTimer.Start();
        FormClosed += (_, _) => _dismissTimer.Dispose();
        Click += (_, _) => Close();
    }

    private static Label NewLabel(string text, float size, FontStyle style, Color color) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", size, style),
        ForeColor = color,
        BackColor = Color.Transparent,
        AutoSize = true
    };

    private void DrawCard(Graphics graphics, bool urgent)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), 16);
        using var fill = new LinearGradientBrush(ClientRectangle, Color.FromArgb(28, 45, 65), Color.FromArgb(17, 28, 42), 90);
        using var border = new Pen(Color.FromArgb(55, 82, 112));
        using var accent = new SolidBrush(urgent ? Color.FromArgb(239, 99, 110) : Color.FromArgb(243, 181, 76));
        using var iconBackground = new SolidBrush(urgent ? Color.FromArgb(74, 47, 60) : Color.FromArgb(73, 60, 38));
        graphics.FillPath(fill, path);
        graphics.DrawPath(border, path);
        graphics.FillRectangle(accent, new Rectangle(0, 18, 5, Height - 36));
        graphics.FillEllipse(iconBackground, new Rectangle(20, 17, 31, 31));
    }

    private static GraphicsPath RoundedPath(Rectangle rectangle, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private Point LocationNearSystemTray()
    {
        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar != IntPtr.Zero && GetWindowRect(taskbar, out var bounds))
        {
            var width = bounds.Right - bounds.Left;
            var height = bounds.Bottom - bounds.Top;
            if (width >= height)
            {
                var y = bounds.Top - Height - 10;
                return new Point(bounds.Right - Width - 10, y >= 0 ? y : bounds.Bottom + 10);
            }
            var x = bounds.Left - Width - 10;
            return new Point(x >= 0 ? x : bounds.Right + 10, bounds.Bottom - Height - 10);
        }

        var screen = Screen.FromPoint(Cursor.Position).WorkingArea;
        return new Point(screen.Right - Width - 20, screen.Bottom - Height - 20);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string className, string? windowName);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr handle, out NativeRect rectangle);

    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
