using System.Runtime.InteropServices;

namespace CodexCreditMonitor;

internal sealed class RefreshToast : Form
{
    private readonly System.Windows.Forms.Timer _dismissTimer = new() { Interval = 3_500 };

    public RefreshToast(string message, bool succeeded, int dismissAfterMilliseconds = 3_500)
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = succeeded ? Color.FromArgb(35, 94, 76) : Color.FromArgb(120, 54, 59);
        ClientSize = new Size(268, 48);
        ConfigureWindow();

        var label = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 0, 12, 0),
            Text = message,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(236, 248, 242),
        };
        Controls.Add(label);
        BeginDismissTimer(dismissAfterMilliseconds);
    }

    public RefreshToast(UsageSummary usage)
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(25, 39, 57);
        ClientSize = new Size(320, 174);
        ConfigureWindow();

        var title = NewLabel("CODEX CREDIT MONITOR", 9, FontStyle.Regular, Color.FromArgb(164, 196, 235));
        title.Location = new Point(14, 12);
        var updated = NewLabel($"{Ui.T("Vernieuwd", "Updated")} · {DateTime.Now:HH:mm}", 9, FontStyle.Regular, Color.FromArgb(151, 174, 201));
        updated.AutoSize = false;
        updated.Location = new Point(192, 12);
        updated.Size = new Size(114, 18);
        updated.TextAlign = ContentAlignment.MiddleRight;
        var balanceCaption = NewLabel(Ui.T("BESCHIKBAAR", "AVAILABLE"), 9, FontStyle.Regular, Color.FromArgb(166, 185, 207));
        balanceCaption.Location = new Point(14, 42);
        var balance = NewLabel(usage.CreditBalance is decimal credits ? $"{credits:N1} credits" : Ui.T("Nog niet beschikbaar", "Not available yet"), 19, FontStyle.Bold, Color.White);
        balance.Location = new Point(13, 55);

        var line = new Panel { BackColor = Color.FromArgb(53, 73, 97), Location = new Point(14, 91), Size = new Size(292, 1) };
        var divider = new Panel { BackColor = Color.FromArgb(49, 68, 91), Location = new Point(159, 105), Size = new Size(1, 35) };
        var fiveCaption = FixedLabel(Ui.T("5-UURSVENSTER", "5-HOUR WINDOW"), 8, FontStyle.Regular, Color.FromArgb(151, 174, 201), new Point(14, 104), new Size(136, 16));
        var fiveHour = FixedLabel(Percent(usage.FiveHourPercent), 10, FontStyle.Bold, Color.FromArgb(228, 237, 248), new Point(14, 120), new Size(136, 19));
        var weekCaption = FixedLabel(Ui.T("WEEKVERBRUIK", "WEEKLY USAGE"), 8, FontStyle.Regular, Color.FromArgb(151, 174, 201), new Point(172, 104), new Size(134, 16));
        var week = FixedLabel(Percent(usage.WeekPercent), 10, FontStyle.Bold, Color.FromArgb(228, 237, 248), new Point(172, 120), new Size(134, 19));
        var today = FixedLabel($"Vandaag · {usage.TodayRequests:N0} momenten · {Tokens(usage.TodayTokens)}", 8.5f, FontStyle.Regular, Color.FromArgb(157, 181, 210), new Point(14, 149), new Size(292, 16));
        today.AutoEllipsis = true;

        Controls.AddRange([title, updated, balanceCaption, balance, line, divider, fiveCaption, fiveHour, weekCaption, week, today]);
        BeginDismissTimer(7_000);
    }

    private void ConfigureWindow()
    {
        ControlBox = false;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;

        Location = LocationNearSystemTray();
    }

    private void BeginDismissTimer(int interval = 3_500)
    {
        _dismissTimer.Interval = interval;
        _dismissTimer.Tick += (_, _) => Close();
        Shown += (_, _) => _dismissTimer.Start();
        FormClosed += (_, _) => _dismissTimer.Dispose();
    }

    private static Label NewLabel(string text, float size, FontStyle style, Color color) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI", size, style),
        ForeColor = color,
        BackColor = Color.Transparent
    };

    private static Label FixedLabel(string text, float size, FontStyle style, Color color, Point location, Size dimensions) => new()
    {
        Text = text,
        AutoSize = false,
        Location = location,
        Size = dimensions,
        Font = new Font("Segoe UI", size, style),
        ForeColor = color,
        BackColor = Color.Transparent
    };

    private static string Percent(double? value) => value is double percent ? $"{percent:0}% {Ui.T("verbruikt", "used")}" : "—";
    private static string Tokens(long value) => value >= 1_000_000 ? $"{value / 1_000_000d:0.0}M tokens" : value >= 1_000 ? $"{value / 1_000d:0.0}K tokens" : $"{value:N0} tokens";

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

        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        return new Point(area.Right - Width - 18, area.Bottom - Height - 18);
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
