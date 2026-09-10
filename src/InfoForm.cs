using System.Runtime.InteropServices;

namespace CodexCreditMonitor;

internal sealed class InfoForm : Form
{
    private const int EmGetScrollPos = 0x04DD;
    private const int EmSetScrollPos = 0x04DE;
    private readonly System.Windows.Forms.Timer _scrollTimer = new() { Interval = 30 };
    private readonly int _secondCopyStart;
    private int _loopHeight;
    public InfoForm(string version, string markdown)
    {
        Text = "Codex Credit Monitor · Info";
        Icon = Program.AppIcon;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(570, 510);
        MinimumSize = new Size(500, 420);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(15, 22, 35);
        Font = new Font("Segoe UI", 10);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 24, 26, 22),
            BackColor = BackColor,
            ColumnCount = 1,
            RowCount = 6
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var product = Header($"Codex Credit Monitor v{version}", 15, FontStyle.Bold, Color.White);
        var author = Header(Ui.S("Info.Concept"), 10, FontStyle.Bold, Color.FromArgb(193, 214, 241));
        var copyright = Header(Ui.S("Info.Ai"), 9, FontStyle.Italic, Color.FromArgb(154, 177, 207));
        var line = new Panel { Dock = DockStyle.Fill, Height = 1, BackColor = Color.FromArgb(59, 79, 104), Margin = Padding.Empty };
        var displayText = ToDisplayText(markdown);
        // A quiet divider separates the looping copies without introducing a
        // distracting label or a large blank block.
        var loopGap = string.Concat(Environment.NewLine, Environment.NewLine,
            "■", Environment.NewLine, Environment.NewLine);
        var restartHeading = Ui.T("PRODUCTINFORMATIE", "PRODUCT INFORMATION");
        var firstCopy = restartHeading + Environment.NewLine + Environment.NewLine + displayText;
        _secondCopyStart = firstCopy.Length + loopGap.Length;
        var secondCopy = restartHeading + Environment.NewLine + Environment.NewLine + displayText;
        var content = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            BackColor = BackColor,
            ForeColor = Color.FromArgb(221, 232, 246),
            Font = new Font("Segoe UI", 10),
            Text = firstCopy + loopGap + secondCopy,
            DetectUrls = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Margin = new Padding(0, 16, 0, 0),
            TabStop = false
        };
        content.SelectAll();
        content.SelectionFont = content.Font;
        content.Select(0, restartHeading.Length);
        content.SelectionFont = new Font(content.Font, FontStyle.Bold);
        var secondHeadingStart = content.Text.LastIndexOf(restartHeading, StringComparison.Ordinal);
        if (secondHeadingStart > 0)
        {
            content.Select(secondHeadingStart, restartHeading.Length);
            content.SelectionFont = new Font(content.Font, FontStyle.Bold);
        }
        content.Select(0, 0);

        root.Controls.Add(product, 0, 0);
        root.Controls.Add(author, 0, 1);
        root.Controls.Add(copyright, 0, 2);
        root.Controls.Add(line, 0, 4);
        root.Controls.Add(content, 0, 5);
        content.MouseEnter += (_, _) => _scrollTimer.Stop();
        content.MouseLeave += (_, _) => StartDescriptionScroll(content);
        Shown += (_, _) => BeginInvoke(() => StartDescriptionScroll(content));
        FormClosed += (_, _) => _scrollTimer.Dispose();
        _scrollTimer.Tick += (_, _) => ScrollDescription(content);

        Controls.Add(root);
    }

    private void StartDescriptionScroll(RichTextBox content)
    {
        if (_secondCopyStart <= 0 || content.IsDisposed) return;
        var firstCopyBottom = content.GetPositionFromCharIndex(Math.Max(0, _secondCopyStart - 7));
        if (firstCopyBottom.Y <= content.ClientSize.Height - 18) return;
        _loopHeight = Math.Max(1, content.GetPositionFromCharIndex(_secondCopyStart).Y);
        _scrollTimer.Start();
    }

    private void ScrollDescription(RichTextBox content)
    {
        if (_loopHeight <= 0 || content.IsDisposed) return;
        var position = new NativePoint();
        SendMessage(content.Handle, EmGetScrollPos, IntPtr.Zero, ref position);
        position.Y = position.Y + 1 >= _loopHeight ? position.Y + 1 - _loopHeight : position.Y + 1;
        SendMessage(content.Handle, EmSetScrollPos, IntPtr.Zero, ref position);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr handle, int message, IntPtr wParam, ref NativePoint lParam);

    private static Label Header(string text, float size, FontStyle style, Color color) => new()
    {
        AutoSize = true,
        Text = text,
        Font = new Font("Segoe UI", size, style),
        ForeColor = color,
        BackColor = Color.Transparent,
        UseMnemonic = false,
        Margin = new Padding(0, 0, 0, 5)
    };

    private static string ToDisplayText(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        return string.Join(Environment.NewLine, lines
            .Where(line => !line.StartsWith("# ", StringComparison.Ordinal))
            .Select(line => line.StartsWith("## ", StringComparison.Ordinal) ? line[3..].ToUpperInvariant() : line)
            .Select(line => line.Replace("**", string.Empty).Replace("*", string.Empty)))
            .Trim();
    }
}
