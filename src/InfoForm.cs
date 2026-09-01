namespace CodexCreditMonitor;

internal sealed class InfoForm : Form
{
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
        var author = Header("Concept & Realisatie: C.S.K. (Simon) Bouwens", 10, FontStyle.Bold, Color.FromArgb(193, 214, 241));
        var copyright = Header("© 2026 C.S.K. (Simon) Bouwens · Gebouwd met AI-ondersteuning", 9, FontStyle.Italic, Color.FromArgb(154, 177, 207));
        var line = new Panel { Dock = DockStyle.Fill, Height = 1, BackColor = Color.FromArgb(59, 79, 104), Margin = Padding.Empty };
        var content = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            BackColor = BackColor,
            ForeColor = Color.FromArgb(221, 232, 246),
            Font = new Font("Segoe UI", 10),
            Text = ToDisplayText(markdown),
            DetectUrls = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Margin = new Padding(0, 16, 0, 0),
            TabStop = false
        };

        root.Controls.Add(product, 0, 0);
        root.Controls.Add(author, 0, 1);
        root.Controls.Add(copyright, 0, 2);
        root.Controls.Add(line, 0, 4);
        root.Controls.Add(content, 0, 5);
        Controls.Add(root);
    }

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
