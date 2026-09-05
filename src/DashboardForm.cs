using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace CodexCreditMonitor;

internal sealed class DashboardForm : Form
{
    private const decimal AddCreditsThreshold = 50m;
    private readonly Label _balance = NewLabel(27, FontStyle.Bold, Color.White);
    private readonly Panel _autoRechargeBadge = new() { BackColor = Color.Transparent, Visible = false };
    private readonly Label _balanceNote = NewLabel(9, FontStyle.Bold, Color.FromArgb(191, 231, 245));
    private readonly Panel _creditPaceBadge = new() { BackColor = Color.Transparent, Visible = false };
    private readonly Label _creditPaceNote = NewLabel(9, FontStyle.Bold, Color.FromArgb(235, 204, 136));
    private readonly Label _updated = NewLabel(10, FontStyle.Regular, Color.FromArgb(155, 172, 194));
    private readonly UsageBar _fiveHour = new(Ui.S("Dashboard.CurrentWindow"));
    private readonly UsageBar _week = new(Ui.S("Dashboard.WeeklyUsage"));
    private readonly Label _todayRequests = NewLabel(17, FontStyle.Bold, Color.White);
    private readonly Label _todayTokens = NewLabel(17, FontStyle.Bold, Color.White);
    private readonly FlowLayoutPanel _sessions = new() { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = false, BackColor = Color.Transparent };
    // Keep the monitor unobtrusive when it stays open: refresh only every two minutes.
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 120_000 };
    // A burst of Codex log activity yields at most one additional low-priority read per minute.
    private readonly System.Windows.Forms.Timer _fileChangeDebounce = new() { Interval = 60_000 };
    private readonly System.Windows.Forms.Timer _bannerTimer = new() { Interval = 5_000 };
    private readonly Panel _refreshBanner = new() { Height = 24, BackColor = Color.Transparent, Visible = false };
    private readonly Label _refreshBannerText = NewLabel(9, FontStyle.Bold, Color.FromArgb(220, 238, 255));
    private readonly ToolTip _headerToolTip = new()
    {
        InitialDelay = 250,
        ReshowDelay = 100,
        AutoPopDelay = 10_000,
        ShowAlways = true
    };
    private readonly HeaderIconButton _addCredits;
    private readonly HeaderIconButton _useCredits;
    private double? _lastWarnedPercent;
    private decimal? _lastWarnedCreditThreshold;
    private CreditSpendAlertLevel _lastCreditSpendAlertLevel;
    private bool _isRefreshing;
    private bool _notifyWhenCurrentRefreshCompletes;
    private int _autoRechargeThreshold;
    private int _autoRechargeTarget;
    private FileSystemWatcher? _sessionWatcher;
    private bool _pulseAddCreditsWhenShown;
    private bool _pulseUseCreditsWhenShown;

    public event Action<double>? WarningRaised;
    public event Action<decimal>? LowCreditWarningRaised;
    public event Action<CreditSpendRate>? RapidCreditSpendWarningRaised;
    public event Action<UsageSummary?, string?>? RefreshCompleted;
    public event Action? SettingsRequested;
    public event Action? InfoRequested;
    public event Action? AddCreditsRequested;
    public event Action? UseCreditsRequested;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal UsageSummary? LatestUsage { get; private set; }

    public DashboardForm()
    {
        Text = "Codex Credit Monitor";
        Icon = Program.AppIcon;
        ClientSize = new Size(450, 730);
        MinimumSize = new Size(420, 730);
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(15, 22, 35);
        Font = new Font("Segoe UI", 10);

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22, 20, 22, 18), BackColor = BackColor };
        root.Paint += DrawBackground;
        Controls.Add(root);

        // FlowLayoutPanel does not honor Dock sizing for child controls; make the header
        // explicitly as wide as the cards so the controls on its right stay visible.
        var header = new Panel { Width = 402, Height = 101, Margin = Padding.Empty, BackColor = Color.Transparent };
        // Five compact header actions leave 190 logical pixels for the product name.
        // Keep the full title visible instead of letting it run beneath an action.
        var title = NewLabel(13, FontStyle.Bold, Color.White);
        title.Text = "Codex Credit Monitor";
        title.Location = new Point(0, 0);
        title.AutoSize = true;
        _updated.Text = Ui.S("Dashboard.LocalTracking");
        _updated.Location = new Point(1, 36);
        _updated.AutoSize = true;
        var refresh = new HeaderIconButton(HeaderIcon.Refresh) { Location = new Point(366, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right, AccessibleName = Ui.T("Vernieuwen", "Refresh") };
        refresh.Click += (_, _) => RefreshUsage();
        var settings = new HeaderIconButton(HeaderIcon.Settings) { Location = new Point(322, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right, AccessibleName = Ui.T("Instellingen", "Settings") };
        settings.Click += (_, _) => SettingsRequested?.Invoke();
        var info = new HeaderIconButton(HeaderIcon.Info) { Location = new Point(278, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right, AccessibleName = Ui.T("Info", "About") };
        info.Click += (_, _) => InfoRequested?.Invoke();
        _useCredits = new HeaderIconButton(HeaderIcon.UseCredits) { Location = new Point(234, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right, Visible = false };
        _useCredits.Click += (_, _) => UseCreditsRequested?.Invoke();
        _addCredits = new HeaderIconButton(HeaderIcon.AddCredits) { Location = new Point(190, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right, Visible = false };
        _addCredits.Click += (_, _) => AddCreditsRequested?.Invoke();
        UpdateCreditActionText();
        VisibleChanged += (_, _) =>
        {
            if (Visible) PulseVisibleCreditActions();
        };
        _headerToolTip.SetToolTip(info, Ui.T("Info", "About"));
        _headerToolTip.SetToolTip(settings, Ui.T("Instellingen", "Settings"));
        _headerToolTip.SetToolTip(refresh, Ui.T("Nu vernieuwen", "Refresh now"));
        _refreshBanner.Location = new Point(0, 59);
        _refreshBanner.Width = 262;
        _refreshBanner.Padding = new Padding(9, 3, 8, 2);
        _refreshBannerText.Dock = DockStyle.Fill;
        _refreshBannerText.TextAlign = ContentAlignment.MiddleLeft;
        _refreshBanner.Controls.Add(_refreshBannerText);
        header.Controls.AddRange([title, _updated, _addCredits, _useCredits, info, settings, refresh, _refreshBanner]);

        var balanceCard = Card(154);
        var balanceCaption = NewLabel(11, FontStyle.Regular, Color.FromArgb(168, 185, 205));
        balanceCaption.Name = "balanceCaption";
        balanceCaption.Text = Ui.S("Dashboard.AvailableBalance");
        balanceCaption.Location = new Point(17, 16);
        balanceCaption.AutoSize = true;
        _balance.Location = new Point(16, 36);
        _balance.Text = "—";
        _balance.AutoSize = true;
        _autoRechargeBadge.Location = new Point(16, 85);
        _autoRechargeBadge.Size = new Size(370, 23);
        _autoRechargeBadge.Padding = new Padding(9, 3, 8, 2);
        _balanceNote.Dock = DockStyle.Fill;
        _balanceNote.TextAlign = ContentAlignment.MiddleLeft;
        _autoRechargeBadge.Controls.Add(_balanceNote);
        _creditPaceBadge.Location = new Point(16, 113);
        _creditPaceBadge.Size = new Size(370, 23);
        _creditPaceBadge.Padding = new Padding(9, 3, 8, 2);
        _creditPaceNote.Dock = DockStyle.Fill;
        _creditPaceNote.TextAlign = ContentAlignment.MiddleLeft;
        _creditPaceBadge.Controls.Add(_creditPaceNote);
        balanceCard.Controls.AddRange([balanceCaption, _balance, _autoRechargeBadge, _creditPaceBadge]);

        var bars = Card(142);
        _fiveHour.Location = new Point(17, 14);
        _fiveHour.Width = 350;
        _week.Location = new Point(17, 77);
        _week.Width = 350;
        bars.Controls.AddRange([_fiveHour, _week]);

        var metrics = new Panel { Width = 402, Height = 88, Margin = Padding.Empty, BackColor = Color.Transparent };
        var requestCard = SmallCard(Ui.S("Dashboard.Today"), Ui.S("Dashboard.ModelEvents"), _todayRequests, new Point(0, 0));
        var tokenCard = SmallCard(Ui.S("Dashboard.Today"), Ui.S("Dashboard.ProcessedTokens"), _todayTokens, new Point(204, 0));
        requestCard.Name = "requestCard";
        tokenCard.Name = "tokenCard";
        metrics.Controls.AddRange([requestCard, tokenCard]);

        var sessionsCard = Card(170);
        var sessionsHeader = NewLabel(11, FontStyle.Regular, Color.FromArgb(168, 185, 205));
        sessionsHeader.Name = "sessionsHeader";
        sessionsHeader.Text = Ui.S("Dashboard.RecentSessions");
        sessionsHeader.Location = new Point(17, 14);
        sessionsHeader.AutoSize = true;
        _sessions.Location = new Point(12, 38);
        _sessions.Size = new Size(382, 116);
        _sessions.AutoScroll = false;
        sessionsCard.Controls.AddRange([sessionsHeader, _sessions]);

        var stack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = false, BackColor = Color.Transparent, Padding = new Padding(0) };
        stack.Controls.AddRange([header, Spacer(6), balanceCard, Spacer(12), bars, Spacer(12), metrics, Spacer(6), sessionsCard]);
        root.Controls.Add(stack);

        _refreshTimer.Tick += (_, _) => RefreshUsage();
        _fileChangeDebounce.Tick += (_, _) =>
        {
            _fileChangeDebounce.Stop();
            RefreshUsage();
        };
        _bannerTimer.Tick += (_, _) => _refreshBanner.Visible = false;
        _refreshTimer.Start();
        Ui.LanguageChanged += OnLanguageChanged;
    }

    public async void RefreshUsage(bool notifyWhenComplete = false)
    {
        if (IsDisposed) return;
        if (_isRefreshing)
        {
            // A tray refresh during the initial/background read must still receive its mini-widget result.
            _notifyWhenCurrentRefreshCompletes |= notifyWhenComplete;
            return;
        }
        _isRefreshing = true;
        _notifyWhenCurrentRefreshCompletes = notifyWhenComplete;
        _updated.Text = Ui.S("Dashboard.RefreshingLocal");
        ShowRefreshBanner(Ui.S("Dashboard.Refreshing"), Color.Transparent, Color.FromArgb(171, 202, 242), hideAfter: false);
        try
        {
            var usage = await Task.Run(UsageReader.Read);
            if (IsDisposed) return;
            ApplyUsage(usage);
            ShowRefreshBanner($"{Ui.S("Dashboard.Refreshed")} · {DateTime.Now:HH:mm:ss}", Color.Transparent, Color.FromArgb(130, 208, 174), hideAfter: true);
            if (_notifyWhenCurrentRefreshCompletes) RefreshCompleted?.Invoke(usage, null);
        }
        catch (Exception)
        {
            if (!IsDisposed)
            {
                _updated.Text = Ui.S("Dashboard.RefreshFailed");
                ShowRefreshBanner(Ui.S("Dashboard.RefreshFailed"), Color.Transparent, Color.FromArgb(244, 155, 159), hideAfter: true);
                if (_notifyWhenCurrentRefreshCompletes) RefreshCompleted?.Invoke(null, Ui.S("Dashboard.RefreshFailed"));
            }
        }
        finally
        {
            _notifyWhenCurrentRefreshCompletes = false;
            _isRefreshing = false;
        }
    }

    public void SetRefreshInterval(int minutes)
    {
        _refreshTimer.Interval = Math.Clamp(minutes, 1, 5) * 60_000;
    }

    internal void ShowPreviewUsage(UsageSummary usage)
    {
        _refreshTimer.Stop();
        ApplyUsage(usage);
    }

    internal void PulseVisibleCreditActions()
    {
        if (_addCredits.Visible)
        {
            _pulseAddCreditsWhenShown = false;
            _addCredits.PulseAttention();
        }
        if (_useCredits.Visible)
        {
            _pulseUseCreditsWhenShown = false;
            _useCredits.PulseAttention();
        }
    }

    private void OnLanguageChanged()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke((Action)OnLanguageChanged); return; }
        var balanceCaption = Controls.Find("balanceCaption", true).OfType<Label>().FirstOrDefault();
        if (balanceCaption is not null) balanceCaption.Text = Ui.S("Dashboard.AvailableBalance");
        var sessionsHeader = Controls.Find("sessionsHeader", true).OfType<Label>().FirstOrDefault();
        if (sessionsHeader is not null) sessionsHeader.Text = Ui.S("Dashboard.RecentSessions");
        UpdateMetricCard("requestCard", Ui.S("Dashboard.Today"), Ui.S("Dashboard.ModelEvents"));
        UpdateMetricCard("tokenCard", Ui.S("Dashboard.Today"), Ui.S("Dashboard.ProcessedTokens"));
        _fiveHour.SetLabel(Ui.S("Dashboard.CurrentWindow"));
        _week.SetLabel(Ui.S("Dashboard.WeeklyUsage"));
        UpdateCreditActionText();
        // This label is not derived from a usage read, so translate it explicitly
        // instead of waiting for the next dashboard refresh.
        UpdateBalanceNote();
        if (_isRefreshing)
        {
            _updated.Text = Ui.S("Dashboard.RefreshingLocal");
            _refreshBannerText.Text = Ui.S("Dashboard.Refreshing");
        }
        else if (LatestUsage is { } usage)
            ApplyUsage(usage);
        else
            _updated.Text = Ui.S("Dashboard.LocalTracking");
    }

    private void UpdateMetricCard(string name, string eyebrow, string caption)
    {
        var card = Controls.Find(name, true).OfType<Panel>().FirstOrDefault();
        if (card is null) return;
        if (card.Controls.Count > 0) card.Controls[0].Text = eyebrow;
        if (card.Controls.Count > 2) card.Controls[2].Text = caption;
    }

    private void ShowRefreshBanner(string message, Color background, Color foreground, bool hideAfter)
    {
        _bannerTimer.Stop();
        _refreshBanner.BackColor = background;
        _refreshBannerText.ForeColor = foreground;
        _refreshBannerText.Text = message;
        _refreshBanner.Visible = true;
        if (hideAfter) _bannerTimer.Start();
    }

    public void SetAutoRecharge(int threshold, int target)
    {
        _autoRechargeThreshold = threshold;
        _autoRechargeTarget = target;
        UpdateBalanceNote();
    }

    public void StartRealtimeMonitoring()
    {
        if (_sessionWatcher is not null) return;
        var sessionsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "sessions");
        if (!Directory.Exists(sessionsRoot)) return;

        _sessionWatcher = new FileSystemWatcher(sessionsRoot, "*.jsonl")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _sessionWatcher.Changed += QueueRealtimeRefresh;
        _sessionWatcher.Created += QueueRealtimeRefresh;
        _sessionWatcher.Renamed += QueueRealtimeRefresh;
    }

    private void QueueRealtimeRefresh(object? sender, FileSystemEventArgs eventArgs)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try
        {
            BeginInvoke((Action)(() =>
            {
                // At most one read per activity burst: responsive without polling pressure.
                if (!_fileChangeDebounce.Enabled) _fileChangeDebounce.Start();
            }));
        }
        catch (InvalidOperationException)
        {
            // The monitor is shutting down.
        }
    }

    private void ApplyUsage(UsageSummary usage)
    {
        LatestUsage = usage;
        _balance.Text = usage.CreditBalance is decimal balance ? $"{balance:N1} credits" : Ui.S("Dashboard.NotAvailable");
        UpdateBalanceNote();
        _updated.Text = usage.LastUpdate is DateTimeOffset updated
            ? string.Format(Ui.S("Dashboard.UpdatedAt"), updated.LocalDateTime.ToString("HH:mm"))
            : usage.Error ?? Ui.S("Dashboard.NoData");
        _fiveHour.SetValue(usage.FiveHourPercent);
        _week.SetValue(usage.WeekPercent);
        _todayRequests.Text = usage.TodayRequests.ToString("N0");
        _todayTokens.Text = FormatTokens(usage.TodayTokens);
        UpdateCreditActionVisibility(usage);
        UpdateSessions(usage.RecentSessions);
        ShowWarningIfNeeded(usage);
        ShowLowCreditWarningIfNeeded(usage);
        var creditSpendRate = CreditSpendRateDetector.Analyze(usage.CreditBalanceHistory);
        UpdateCreditPace(usage, creditSpendRate);
        ShowRapidCreditSpendWarningIfNeeded(creditSpendRate);
    }

    private void UpdateBalanceNote()
    {
        _autoRechargeBadge.Visible = true;
        _balanceNote.Text = $"{Ui.S("Dashboard.AutoRecharge")} · {_autoRechargeThreshold} → {_autoRechargeTarget} CREDITS";
    }

    private void UpdateCreditActionText()
    {
        _addCredits.AccessibleName = Ui.T("Credits toevoegen", "Add credits");
        _useCredits.AccessibleName = Ui.T("Credits gebruiken", "Use credits");
        _headerToolTip.SetToolTip(_addCredits, Ui.T(
            "Credits toevoegen\nOpen Usage & Billing in ChatGPT om je tegoed aan te vullen.",
            "Add credits\nOpen Usage & Billing in ChatGPT to top up your balance."));
        _headerToolTip.SetToolTip(_useCredits, Ui.T(
            "Credits gebruiken\nJe inbegrepen bundel is opgebruikt. Open Usage & Billing in ChatGPT voor extra Codex-gebruik.",
            "Use credits\nYour included allowance is exhausted. Open Usage & Billing in ChatGPT for additional Codex usage."));
    }

    private void UpdateCreditActionVisibility(UsageSummary usage)
    {
        var addCreditsWasVisible = _addCredits.Visible;
        var useCreditsWasVisible = _useCredits.Visible;
        _addCredits.Visible = usage.CreditBalance is decimal balance && balance <= AddCreditsThreshold;
        // The portal itself is not inspected. A fully used included allowance plus a positive
        // local balance is the reliable local signal that credit-backed usage is relevant.
        _useCredits.Visible = usage.CreditBalance is decimal available && available > 0m && usage.WeekPercent is >= 100d;

        // Keep a single conditional action connected to the fixed header actions.
        // When both actions are present, they use the two reserved slots in order.
        _addCredits.Location = new Point(_useCredits.Visible ? 190 : 234, 0);
        _useCredits.Location = new Point(234, 0);

        if (_addCredits.Visible && !addCreditsWasVisible) _pulseAddCreditsWhenShown = true;
        if (_useCredits.Visible && !useCreditsWasVisible) _pulseUseCreditsWhenShown = true;

        if (Visible) PulsePendingCreditActions();
    }

    private void PulsePendingCreditActions()
    {
        if (_pulseAddCreditsWhenShown)
        {
            _pulseAddCreditsWhenShown = false;
            _addCredits.PulseAttention();
        }
        if (_pulseUseCreditsWhenShown)
        {
            _pulseUseCreditsWhenShown = false;
            _useCredits.PulseAttention();
        }
    }

    private void UpdateCreditPace(UsageSummary usage, CreditSpendRate? rate)
    {
        _creditPaceBadge.Visible = true;
        if (usage.WeekPercent is >= 100)
        {
            _creditPaceBadge.BackColor = Color.FromArgb(74, 47, 60);
            _creditPaceNote.ForeColor = Color.FromArgb(255, 181, 184);
            _creditPaceNote.Text = Ui.T("BUNDEL OPGEBRUIKT · credits worden nu gebruikt", "INCLUDED ALLOWANCE EXHAUSTED · credits are now in use");
            return;
        }

        _creditPaceBadge.BackColor = Color.Transparent;
        _creditPaceNote.ForeColor = rate?.AlertLevel == CreditSpendAlertLevel.Critical
            ? Color.FromArgb(255, 181, 184)
            : Color.FromArgb(235, 204, 136);
        _creditPaceNote.Text = rate is null
            ? Ui.T("VERBRUIKSTEMPO · lokale metingen worden verzameld", "USAGE PACE · collecting local measurements")
            : Ui.T($"VERBRUIKSTEMPO · {rate.CreditsPerHour:N1} credits/uur", $"USAGE PACE · {rate.CreditsPerHour:N1} credits/hour");
    }

    private void UpdateSessions(IReadOnlyList<SessionUsage> sessions)
    {
        _sessions.SuspendLayout();
        _sessions.Controls.Clear();
        if (sessions.Count == 0)
        {
            var empty = NewLabel(10, FontStyle.Regular, Color.FromArgb(150, 169, 191));
            empty.Text = Ui.S("Dashboard.NoSessions");
            empty.Margin = new Padding(5, 5, 0, 0);
            empty.AutoSize = true;
            _sessions.Controls.Add(empty);
        }
        foreach (var session in sessions.Take(4))
        {
            var row = new Panel { Width = 360, Height = 26, Margin = new Padding(4, 0, 0, 3), BackColor = Color.FromArgb(33, 48, 68) };
            var left = NewLabel(9, FontStyle.Regular, Color.FromArgb(216, 227, 241));
            left.Text = $"{session.Model.Replace("gpt-", "").Replace("-", " ")} · {session.LastUpdate.LocalDateTime:HH:mm}";
            left.Location = new Point(9, 5);
            left.Size = new Size(195, 17);
            left.AutoEllipsis = true;
            var right = NewLabel(9, FontStyle.Regular, Color.FromArgb(152, 195, 255));
            right.Text = $"{session.Requests} · {FormatTokens(session.Tokens)}";
            right.Location = new Point(209, 5);
            right.Size = new Size(140, 17);
            right.TextAlign = ContentAlignment.MiddleRight;
            row.Controls.AddRange([left, right]);
            _sessions.Controls.Add(row);
        }
        _sessions.ResumeLayout();
    }

    private void ShowWarningIfNeeded(UsageSummary usage)
    {
        if (usage.FiveHourPercent is not double percent) return;
        var threshold = percent >= 90 ? 90 : percent >= 75 ? 75 : 0;
        if (threshold == 0)
        {
            _lastWarnedPercent = null;
            return;
        }
        if (_lastWarnedPercent == threshold) return;
        _lastWarnedPercent = threshold;
        // A threshold is announced once, and becomes eligible again after usage returns below 75%.
        NotifyUser(percent);
    }

    private void NotifyUser(double percent)
    {
        WarningRaised?.Invoke(percent);
    }

    private void ShowLowCreditWarningIfNeeded(UsageSummary usage)
    {
        if (usage.CreditBalance is not decimal credits) return;

        var threshold = credits <= 10m ? 10m : credits <= 25m ? 25m : 0m;
        if (threshold == 0m)
        {
            _lastWarnedCreditThreshold = null;
            return;
        }

        if (_lastWarnedCreditThreshold == threshold) return;
        _lastWarnedCreditThreshold = threshold;
        // Mirrors the usage-window warning behaviour: one alert per threshold, re-armed above 25 credits.
        LowCreditWarningRaised?.Invoke(credits);
    }

    private void ShowRapidCreditSpendWarningIfNeeded(CreditSpendRate? rate)
    {
        if (rate is null || rate.AlertLevel == CreditSpendAlertLevel.None)
        {
            _lastCreditSpendAlertLevel = CreditSpendAlertLevel.None;
            return;
        }

        if (rate.AlertLevel <= _lastCreditSpendAlertLevel) return;
        _lastCreditSpendAlertLevel = rate.AlertLevel;
        RapidCreditSpendWarningRaised?.Invoke(rate);
    }

    private Panel SmallCard(string eyebrow, string caption, Label value, Point location)
    {
        var card = Card(82);
        card.Location = location;
        card.Size = new Size(198, 82);
        var label = NewLabel(9, FontStyle.Regular, Color.FromArgb(154, 173, 195));
        label.Text = eyebrow;
        label.Location = new Point(14, 11);
        label.AutoSize = true;
        value.Location = new Point(13, 27);
        value.AutoSize = true;
        var detail = NewLabel(9, FontStyle.Regular, Color.FromArgb(144, 162, 184));
        detail.Text = caption;
        detail.Location = new Point(14, 57);
        detail.AutoSize = true;
        card.Controls.AddRange([label, value, detail]);
        return card;
    }

    private static Panel Card(int height) => new() { Width = 402, Height = height, Margin = Padding.Empty, BackColor = Color.FromArgb(27, 39, 57) };
    private static Panel Spacer(int height) => new() { Height = height, Width = 402, Margin = Padding.Empty, BackColor = Color.Transparent };
    private static Label NewLabel(float size, FontStyle style, Color color) => new() { Font = new Font("Segoe UI", size, style), ForeColor = color, BackColor = Color.Transparent };
    private static string FormatTokens(long value) => value >= 1_000_000 ? $"{value / 1_000_000d:0.0}M" : value >= 1_000 ? $"{value / 1_000d:0.0}K" : value.ToString("N0");

    private void DrawBackground(object? sender, PaintEventArgs e)
    {
        using var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(17, 28, 45), Color.FromArgb(11, 16, 26), 90);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Ui.LanguageChanged -= OnLanguageChanged;
            _sessionWatcher?.Dispose();
            _fileChangeDebounce.Dispose();
        }
        base.Dispose(disposing);
    }

}

internal enum HeaderIcon
{
    AddCredits,
    UseCredits,
    Info,
    Settings,
    Refresh
}

internal sealed class HeaderIconButton : Control
{
    private readonly HeaderIcon _icon;
    private bool _hovered;
    private int _attentionFrame = -1;
    private int _attentionSequence;

    public HeaderIconButton(HeaderIcon icon)
    {
        _icon = icon;
        Size = new Size(36, 32);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        TabStop = true;
    }

    public async void PulseAttention()
    {
        var sequence = ++_attentionSequence;
        // Start after the window has actually settled on screen, then pulse for five seconds.
        await Task.Delay(600);
        for (var frame = 0; frame < 24; frame++)
        {
            if (sequence != _attentionSequence || IsDisposed) return;
            _attentionFrame = frame;
            Invalidate();
            await Task.Delay(200);
        }
        if (sequence != _attentionSequence || IsDisposed) return;
        _attentionFrame = -1;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs eventArgs)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(eventArgs);
    }

    protected override void OnMouseLeave(EventArgs eventArgs)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(eventArgs);
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (_hovered)
        {
            using var hover = new SolidBrush(Color.FromArgb(38, 64, 95));
            eventArgs.Graphics.FillEllipse(hover, 2, 0, 32, 32);
        }
        if (_attentionFrame >= 0)
        {
            var phase = _attentionFrame % 6;
            var alpha = phase is 0 or 1 ? 210 : phase is 2 or 3 ? 120 : 55;
            var inset = phase is 0 or 1 ? 0 : phase is 2 or 3 ? 2 : 4;
            using var attentionFill = new SolidBrush(Color.FromArgb(alpha / 3, 255, 196, 93));
            using var attentionRing = new Pen(Color.FromArgb(alpha, 255, 215, 133), 2.4f);
            eventArgs.Graphics.FillEllipse(attentionFill, inset, inset, Width - inset * 2, Height - inset * 2);
            eventArgs.Graphics.DrawEllipse(attentionRing, inset + 1, inset + 1, Width - inset * 2 - 2, Height - inset * 2 - 2);
        }

        using var pen = new Pen(Color.FromArgb(197, 220, 252), 2.1f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        if (_icon == HeaderIcon.Refresh)
        {
            eventArgs.Graphics.DrawArc(pen, 8, 6, 20, 20, -58, 286);
            using var fill = new SolidBrush(Color.FromArgb(197, 220, 252));
            eventArgs.Graphics.FillPolygon(fill, [new Point(29, 8), new Point(23, 8), new Point(28, 14)]);
            return;
        }

        if (_icon == HeaderIcon.AddCredits)
        {
            eventArgs.Graphics.DrawEllipse(pen, 8, 6, 20, 20);
            eventArgs.Graphics.DrawLine(pen, 18, 11, 18, 21);
            eventArgs.Graphics.DrawLine(pen, 13, 16, 23, 16);
            return;
        }

        if (_icon == HeaderIcon.UseCredits)
        {
            eventArgs.Graphics.DrawRectangle(pen, 8, 7, 20, 17);
            eventArgs.Graphics.DrawLine(pen, 9, 12, 27, 12);
            using var fill = new SolidBrush(Color.FromArgb(197, 220, 252));
            eventArgs.Graphics.FillPolygon(fill, [new Point(19, 13), new Point(14, 19), new Point(18, 19), new Point(16, 24), new Point(23, 16), new Point(19, 16)]);
            return;
        }

        if (_icon == HeaderIcon.Info)
        {
            eventArgs.Graphics.DrawEllipse(pen, 9, 7, 18, 18);
            using var infoFont = new Font("Segoe UI", 12, FontStyle.Bold);
            using var infoBrush = new SolidBrush(Color.FromArgb(197, 220, 252));
            var infoSize = eventArgs.Graphics.MeasureString("i", infoFont);
            eventArgs.Graphics.DrawString("i", infoFont, infoBrush, (Width - infoSize.Width) / 2, 7);
            return;
        }

        var center = new PointF(18, 16);
        for (var step = 0; step < 8; step++)
        {
            var angle = step * MathF.PI / 4;
            var inner = new PointF(center.X + MathF.Cos(angle) * 7, center.Y + MathF.Sin(angle) * 7);
            var outer = new PointF(center.X + MathF.Cos(angle) * 10, center.Y + MathF.Sin(angle) * 10);
            eventArgs.Graphics.DrawLine(pen, inner, outer);
        }
        eventArgs.Graphics.DrawEllipse(pen, 10, 8, 16, 16);
        eventArgs.Graphics.DrawEllipse(pen, 15, 13, 6, 6);
    }
}

internal sealed class UsageBar : Control
{
    private string _label;
    private double? _value;

    public UsageBar(string label)
    {
        _label = label;
        Height = 52;
        DoubleBuffered = true;
    }

    public void SetValue(double? value)
    {
        _value = value;
        Invalidate();
    }

    public void SetLabel(string label)
    {
        _label = label;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var labelFont = new Font("Segoe UI", 10, FontStyle.Regular);
        using var valueFont = new Font("Segoe UI", 10, FontStyle.Bold);
        using var labelBrush = new SolidBrush(Color.FromArgb(214, 226, 241));
        using var mutedBrush = new SolidBrush(Color.FromArgb(151, 170, 193));
        e.Graphics.DrawString(_label, labelFont, labelBrush, 0, 0);
        var text = _value is double value ? $"{value:0}% {Ui.S("Dashboard.Used")}" : Ui.S("Dashboard.NotAvailable");
        var size = e.Graphics.MeasureString(text, valueFont);
        e.Graphics.DrawString(text, valueFont, mutedBrush, Width - size.Width, 0);
        var bar = new RectangleF(0, 28, Width, 10);
        using var track = new SolidBrush(Color.FromArgb(50, 70, 94));
        e.Graphics.FillRoundedRectangle(track, bar, 5);
        if (_value is not double percent) return;
        var color = percent >= 90 ? Color.FromArgb(244, 112, 112) : percent >= 75 ? Color.FromArgb(245, 182, 80) : Color.FromArgb(83, 184, 144);
        using var fill = new SolidBrush(color);
        var filled = new RectangleF(bar.X, bar.Y, Math.Max(0, bar.Width * (float)Math.Clamp(percent, 0, 100) / 100), bar.Height);
        e.Graphics.FillRoundedRectangle(fill, filled, 5);
    }
}

internal static class DrawingExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, RectangleF rectangle, float radius)
    {
        using var path = new GraphicsPath();
        path.AddArc(rectangle.X, rectangle.Y, radius * 2, radius * 2, 180, 90);
        path.AddArc(rectangle.Right - radius * 2, rectangle.Y, radius * 2, radius * 2, 270, 90);
        path.AddArc(rectangle.Right - radius * 2, rectangle.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
        path.AddArc(rectangle.X, rectangle.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}
