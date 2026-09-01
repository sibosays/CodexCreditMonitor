using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace CodexCreditMonitor;

internal sealed class SettingsForm : Form
{
    private readonly MonitorSettings _settings;
    private readonly Func<bool> _startupEnabled;
    private readonly Func<bool, bool> _setStartupEnabled;
    private readonly Action<int> _setRefreshInterval;
    private readonly bool _allowStartup;
    private readonly ToggleSwitch _startupToggle;
    private readonly ToggleSwitch _alertsToggle;
    private readonly ToggleSwitch _soundToggle;
    private readonly List<ChoiceChip> _intervalChoices = [];
    private ComboBox? _languageChoice;
    private bool _languageChanged;

    public SettingsForm(MonitorSettings settings, Func<bool> startupEnabled, Func<bool, bool> setStartupEnabled, Action<int> setRefreshInterval, bool allowStartup)
    {
        _settings = settings;
        _startupEnabled = startupEnabled;
        _setStartupEnabled = setStartupEnabled;
        _setRefreshInterval = setRefreshInterval;
        _allowStartup = allowStartup;

        Text = $"{Ui.T("Instellingen", "Settings")} · Codex Credit Monitor";
        Icon = Program.AppIcon;
        ClientSize = new Size(462, 492);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(15, 22, 35);
        Font = new Font("Segoe UI", 10);

        var title = Label(Ui.T("Instellingen", "Settings"), 20, FontStyle.Bold, Color.White);
        title.Location = new Point(24, 21);
        var subtitle = Label(Ui.T("Kies hoe rustig en opvallend de monitor werkt.", "Choose how quietly and visibly the monitor works."), 10, FontStyle.Regular, Color.FromArgb(158, 177, 201));
        subtitle.Location = new Point(25, 51);

        _startupToggle = new ToggleSwitch { Checked = _startupEnabled(), Location = new Point(365, 24), Enabled = _allowStartup };
        var startup = Card(
            Ui.T("Starten met Windows", "Start with Windows"),
            _allowStartup ? Ui.T("Start onzichtbaar in het systeemvak na aanmelden.", "Starts hidden in the system tray after sign-in.") : Ui.T("Niet beschikbaar in de draagbare editie.", "Unavailable in the portable edition."),
            84,
            _startupToggle);
        _startupToggle.CheckedChanged += (_, _) =>
        {
            if (!_allowStartup || _startupToggle.Checked == _startupEnabled()) return;
            if (!_setStartupEnabled(_startupToggle.Checked)) _startupToggle.Checked = _startupEnabled();
        };

        _alertsToggle = new ToggleSwitch { Checked = _settings.AlertsEnabled, Location = new Point(365, 24) };
        var alerts = Card(Ui.T("Waarschuwingen bij hoog verbruik", "High-usage warnings"), Ui.T("Toon een duidelijke melding bij 75% en 90% verbruik.", "Show a clear alert at 75% and 90% usage."), 164, _alertsToggle);
        _soundToggle = new ToggleSwitch { Checked = _settings.AlertSoundEnabled, Location = new Point(365, 24), Enabled = _settings.AlertsEnabled };
        var sound = Card(Ui.T("Waarschuwingsgeluid", "Alert sound"), Ui.T("Speel een kort Windows-signaal bij een verbruikswaarschuwing.", "Play a short Windows sound with a usage alert."), 244, _soundToggle);
        _soundToggle.CheckedChanged += (_, _) =>
        {
            _settings.AlertSoundEnabled = _soundToggle.Checked;
            _settings.Save();
        };
        _alertsToggle.CheckedChanged += (_, _) =>
        {
            _settings.AlertsEnabled = _alertsToggle.Checked;
            _soundToggle.Enabled = _alertsToggle.Checked;
            _settings.Save();
        };

        var interval = new SettingsCard { Location = new Point(23, 324), Size = new Size(416, 72) };
        var intervalTitle = Label(Ui.T("Verversinterval", "Refresh interval"), 11, FontStyle.Bold, Color.FromArgb(229, 238, 249));
        intervalTitle.Location = new Point(16, 14);
        var intervalDescription = Label(Ui.T("Lees lokale sessies op de achtergrond.", "Read local sessions in the background."), 9, FontStyle.Regular, Color.FromArgb(150, 172, 197));
        intervalDescription.Location = new Point(16, 37);
        interval.Controls.AddRange([intervalTitle, intervalDescription]);
        foreach (var minutes in new[] { 1, 2, 5 })
        {
            var chip = new ChoiceChip($"{minutes} min.") { Location = new Point(250 + (minutes == 1 ? 0 : minutes == 2 ? 53 : 106), 20), Selected = _settings.RefreshIntervalMinutes == minutes };
            chip.Click += (_, _) => SetInterval(minutes);
            _intervalChoices.Add(chip);
            interval.Controls.Add(chip);
        }

        var apply = new Button { Text = Ui.T("Toepassen", "Apply"), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(45, 87, 136), ForeColor = Color.White, Location = new Point(16, 42), Size = new Size(104, 24), Enabled = false };
        apply.FlatAppearance.BorderSize = 0;
        apply.Click += (_, _) => ApplyLanguage();
        var language = new SettingsCard { Location = new Point(23, 404), Size = new Size(416, 72) };
        var languageTitle = Label(Ui.T("Taal", "Language"), 11, FontStyle.Bold, Color.FromArgb(229, 238, 249));
        languageTitle.Location = new Point(16, 13);
        var languageDescription = Label(Ui.T("Start de app opnieuw in de gekozen taal.", "Restarts the app in the selected language."), 8.5f, FontStyle.Regular, Color.FromArgb(150, 172, 197));
        languageDescription.Location = new Point(130, 46);
        _languageChoice = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(215, 21), Size = new Size(184, 25), Font = new Font("Segoe UI", 9) };
        _languageChoice.Items.AddRange(["Automatisch (Windows)", "Nederlands", "English"]);
        _languageChoice.SelectedIndex = _settings.Language switch { "nl" => 1, "en" => 2, _ => 0 };
        _languageChoice.SelectedIndexChanged += (_, _) =>
        {
            _languageChanged = SelectedLanguage() != _settings.Language;
            apply.Enabled = _languageChanged;
        };
        language.Controls.AddRange([languageTitle, languageDescription, _languageChoice, apply]);

        Controls.AddRange([title, subtitle, startup, alerts, sound, interval, language]);
    }

    private SettingsCard Card(string titleText, string descriptionText, int y, ToggleSwitch toggle)
    {
        var card = new SettingsCard { Location = new Point(23, y), Size = new Size(416, 72) };
        var title = Label(titleText, 11, FontStyle.Bold, Color.FromArgb(229, 238, 249));
        title.Location = new Point(16, 13);
        var description = Label(descriptionText, 9, FontStyle.Regular, Color.FromArgb(150, 172, 197));
        description.Location = new Point(16, 38);
        description.Size = new Size(320, 18);
        card.Controls.AddRange([title, description, toggle]);
        return card;
    }

    private void SetInterval(int minutes)
    {
        _setRefreshInterval(minutes);
        foreach (var chip in _intervalChoices) chip.Selected = chip.Text == $"{minutes} min.";
    }

    private string SelectedLanguage() => _languageChoice?.SelectedIndex switch { 1 => "nl", 2 => "en", _ => "auto" };

    private void ApplyLanguage()
    {
        if (!_languageChanged) return;
        var language = SelectedLanguage();
        _settings.Language = language;
        _settings.Save();
        Ui.SetLanguage(language);
        Program.RestartForLanguageChange(Owner is DashboardForm { Visible: true });
    }

    private static Label Label(string text, float size, FontStyle style, Color color) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI", size, style),
        ForeColor = color,
        BackColor = Color.Transparent
    };
}

internal sealed class SettingsCard : Panel
{
    public SettingsCard()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), 12);
        using var fill = new SolidBrush(Color.FromArgb(27, 40, 58));
        using var border = new Pen(Color.FromArgb(46, 67, 92));
        eventArgs.Graphics.FillPath(fill, path);
        eventArgs.Graphics.DrawPath(border, path);
        base.OnPaint(eventArgs);
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
}

internal sealed class ToggleSwitch : Control
{
    private bool _checked;
    public event EventHandler? CheckedChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public ToggleSwitch()
    {
        Size = new Size(42, 24);
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
    }

    protected override void OnClick(EventArgs eventArgs)
    {
        if (Enabled) Checked = !Checked;
        base.OnClick(eventArgs);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var active = Enabled && Checked;
        using var track = new SolidBrush(active ? Color.FromArgb(68, 152, 216) : Color.FromArgb(71, 88, 109));
        using var thumb = new SolidBrush(Enabled ? Color.White : Color.FromArgb(149, 161, 176));
        eventArgs.Graphics.FillEllipse(track, new Rectangle(0, 2, 42, 20));
        eventArgs.Graphics.FillEllipse(thumb, new Rectangle(Checked ? 22 : 3, 4, 16, 16));
    }
}

internal sealed class ChoiceChip : Button
{
    private bool _selected;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set { _selected = value; Invalidate(); }
    }

    public ChoiceChip(string text)
    {
        Text = text;
        Size = new Size(49, 28);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 8, FontStyle.Bold);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = new GraphicsPath();
        path.AddArc(0, 0, 12, 12, 180, 90);
        path.AddArc(Width - 12, 0, 12, 12, 270, 90);
        path.AddArc(Width - 12, Height - 12, 12, 12, 0, 90);
        path.AddArc(0, Height - 12, 12, 12, 90, 90);
        path.CloseFigure();
        using var background = new SolidBrush(Selected ? Color.FromArgb(48, 103, 165) : Color.FromArgb(48, 65, 86));
        using var foreground = new SolidBrush(Selected ? Color.White : Color.FromArgb(181, 202, 226));
        eventArgs.Graphics.FillPath(background, path);
        var size = eventArgs.Graphics.MeasureString(Text, Font);
        eventArgs.Graphics.DrawString(Text, Font, foreground, (Width - size.Width) / 2, (Height - size.Height) / 2);
    }
}
