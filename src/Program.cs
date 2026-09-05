using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CodexCreditMonitor;

internal static class Program
{
    private const string AppTitle = "Codex Credit Monitor";
    // ChatGPT owns purchasing and flexible-credit settings; this monitor only provides a shortcut.
    private const string CreditSettingsUrl = "https://chatgpt.com/#settings/usage";
    internal static readonly Icon AppIcon = CreateAppIcon();

    private static MonitorSettings? _settingsForRestart;

    internal static void RestartForLanguageChange(bool reopenDashboard)
    {
        _settingsForRestart!.ReopenDashboardAfterLanguageChange = reopenDashboard;
        _settingsForRestart.Save();
        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable)) return;
        var restartThread = new Thread(() =>
        {
            Thread.Sleep(500);
            Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
        }) { IsBackground = false };
        restartThread.Start();
        Application.Exit();
    }
    private static string StartupShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
        "Codex Credit Monitor.lnk");

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var commandLine = Environment.GetCommandLineArgs();
        if (commandLine.Contains("--preview-low-credits", StringComparer.OrdinalIgnoreCase))
        {
            RunDashboardPreview(50m, 68d);
            return;
        }
        if (commandLine.Contains("--preview-use-credits", StringComparer.OrdinalIgnoreCase))
        {
            RunDashboardPreview(80m, 100d);
            return;
        }
        if (commandLine.Contains("--preview-credit-actions", StringComparer.OrdinalIgnoreCase))
        {
            RunDashboardPreview(34m, 100d);
            return;
        }
        if (commandLine.Contains("--render-credit-actions", StringComparer.OrdinalIgnoreCase))
        {
            RenderDashboardPreview(34m, 100d);
            return;
        }
        if (commandLine.Contains("--preview-info", StringComparer.OrdinalIgnoreCase))
        {
            using var preview = new RefreshToast("Saldo bijgewerkt · 245 credits", true, 8_000);
            Application.Run(preview);
            return;
        }
        if (commandLine.Contains("--preview-alert", StringComparer.OrdinalIgnoreCase))
        {
            using var preview = new UsageAlertToast(82);
            Application.Run(preview);
            return;
        }
        if (commandLine.Contains("--preview-critical", StringComparer.OrdinalIgnoreCase))
        {
            using var preview = new UsageAlertToast(94);
            Application.Run(preview);
            return;
        }
        if (commandLine.Contains("--preview-low-credit", StringComparer.OrdinalIgnoreCase))
        {
            using var preview = new CreditBalanceAlertToast(22m);
            Application.Run(preview);
            return;
        }
        if (commandLine.Contains("--preview-critical-credit", StringComparer.OrdinalIgnoreCase))
        {
            using var preview = new CreditBalanceAlertToast(8m);
            Application.Run(preview);
            return;
        }
        if (commandLine.Contains("--preview-rapid-credit", StringComparer.OrdinalIgnoreCase))
        {
            using var preview = new CreditSpendRateAlertToast(new CreditSpendRate(39m, 31m, TimeSpan.FromMinutes(55), 33.8m, CreditSpendAlertLevel.Critical));
            Application.Run(preview);
            return;
        }
        if (commandLine.Contains("--verify-credit-pace", StringComparer.OrdinalIgnoreCase))
        {
            VerifyCreditPaceDetection();
            return;
        }
        try
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch (Exception)
        {
            // Some Windows policies do not permit priority changes; monitoring still remains background work.
        }

        using var instanceMutex = new Mutex(initiallyOwned: true, "Local\\CodexCreditMonitor", out var isFirstInstance);
        if (!isFirstInstance)
        {
            try
            {
                using var existingDashboardSignal = EventWaitHandle.OpenExisting("Local\\CodexCreditMonitor.ShowDashboard");
                existingDashboardSignal.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // A previous version may still be exiting; the new instance can safely end here.
            }
            return;
        }

        using var dashboard = new DashboardForm();
        using var applicationContext = new ApplicationContext();
        using var showDashboardSignal = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\CodexCreditMonitor.ShowDashboard");
        var allowClose = false;
        var settings = MonitorSettings.Load();
        _settingsForRestart = settings;
        Ui.SetLanguage(settings.Language);
        if (settings.ReopenDashboardAfterLanguageChange)
        {
            settings.ReopenDashboardAfterLanguageChange = false;
            settings.Save();
            dashboard.Show();
        }
        var menu = new ContextMenuStrip();
        using var trayIcon = new NotifyIcon
        {
            Icon = AppIcon,
            Text = AppTitle,
            ContextMenuStrip = menu,
        };
        // Register first, then set the tooltip: this makes Explorer refresh it reliably.
        trayIcon.Visible = true;
        trayIcon.Text = AppTitle;
        RefreshToast? activeRefreshToast = null;
        Form? activeAlertToast = null;

        void ShowAlert(Form alert)
        {
            activeAlertToast?.Close();
            activeAlertToast = alert;
            alert.FormClosed += (_, _) =>
            {
                if (ReferenceEquals(activeAlertToast, alert)) activeAlertToast = null;
            };
            alert.Show();
        }

        void ShowDashboard()
        {
            dashboard.Show();
            dashboard.WindowState = FormWindowState.Normal;
            dashboard.BringToFront();
            dashboard.Activate();
        }

        var showDashboardRegistration = ThreadPool.RegisterWaitForSingleObject(
            showDashboardSignal,
            (_, _) =>
            {
                if (!dashboard.IsDisposed) dashboard.BeginInvoke((Action)ShowDashboard);
            },
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);

        bool SetStartupEnabled(bool enabled)
        {
            if (MonitorSettings.IsPortableMode) return false;
            try
            {
                if (enabled && !File.Exists(StartupShortcutPath))
                {
                    var executable = Environment.ProcessPath;
                    if (string.IsNullOrWhiteSpace(executable)) return false;
                    CreateStartupShortcut(executable);
                }
                else if (!enabled && File.Exists(StartupShortcutPath))
                {
                    File.Delete(StartupShortcutPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                trayIcon.ShowBalloonTip(4_000, AppTitle, $"Opstartinstelling kon niet worden opgeslagen: {ex.Message}", ToolTipIcon.Error);
                return false;
            }
        }

        void SetRefreshInterval(int minutes)
        {
            settings.RefreshIntervalMinutes = minutes;
            dashboard.SetRefreshInterval(minutes);
            settings.Save();
        }

        void ShowSettings()
        {
            using var settingsForm = new SettingsForm(
                settings,
                () => !MonitorSettings.IsPortableMode && File.Exists(StartupShortcutPath),
                SetStartupEnabled,
                SetRefreshInterval,
                allowStartup: !MonitorSettings.IsPortableMode);
            settingsForm.ShowDialog(dashboard.Visible ? dashboard : null);
        }

        void ShowInfo()
        {
            var source = Path.Combine(AppContext.BaseDirectory, "WhatsNew.md");
            var message = File.Exists(source)
                ? Ui.SelectInfo(File.ReadAllText(source))
                : Ui.T("Codex Credit Monitor\n\nInformatie is niet beschikbaar.", "Codex Credit Monitor\n\nInformation is unavailable.");
            var version = typeof(Program).Assembly.GetName().Version?.ToString(2) ?? "—";
            using var info = new InfoForm(version, message);
            info.ShowDialog();
        }

        void OpenCreditSettings()
        {
            Process.Start(new ProcessStartInfo(CreditSettingsUrl) { UseShellExecute = true });
        }

        void BuildTrayMenu()
        {
        menu.Items.Clear();
        menu.Items.Add(Ui.T("Dashboard openen", "Open dashboard"), null, (_, _) => ShowDashboard());
        menu.Items.Add(Ui.T("Nu vernieuwen", "Refresh now"), null, (_, _) =>
        {
            var notifyWhenComplete = !dashboard.Visible;
            if (notifyWhenComplete && dashboard.LatestUsage is { } latestUsage)
            {
                activeRefreshToast?.Close();
                activeRefreshToast = new RefreshToast(latestUsage);
                activeRefreshToast.Show();
            }
            dashboard.RefreshUsage(notifyWhenComplete);
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Ui.T("Instellingen…", "Settings…"), null, (_, _) => ShowSettings());
        menu.Items.Add(Ui.T("Info…", "About…"), null, (_, _) => ShowInfo());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Ui.T("Afsluiten", "Exit"), null, (_, _) =>
        {
            allowClose = true;
            dashboard.Close();
            applicationContext.ExitThread();
        });
        }
        BuildTrayMenu();
        Ui.LanguageChanged += () => BuildTrayMenu();

        trayIcon.MouseClick += (_, eventArgs) =>
        {
            if (eventArgs.Button == MouseButtons.Left) ShowDashboard();
        };
        dashboard.WarningRaised += percent =>
        {
            if (!settings.AlertsEnabled) return;
            if (settings.AlertSoundEnabled) System.Media.SystemSounds.Exclamation.Play();
            ShowAlert(new UsageAlertToast(percent));
        };
        dashboard.LowCreditWarningRaised += credits =>
        {
            if (!settings.AlertsEnabled) return;
            if (settings.AlertSoundEnabled) System.Media.SystemSounds.Exclamation.Play();
            ShowAlert(new CreditBalanceAlertToast(credits));
        };
        dashboard.RapidCreditSpendWarningRaised += rate =>
        {
            if (!settings.AlertsEnabled) return;
            if (settings.AlertSoundEnabled) System.Media.SystemSounds.Exclamation.Play();
            ShowAlert(new CreditSpendRateAlertToast(rate));
        };
        dashboard.SettingsRequested += ShowSettings;
        dashboard.InfoRequested += ShowInfo;
        dashboard.AddCreditsRequested += OpenCreditSettings;
        dashboard.UseCreditsRequested += OpenCreditSettings;
        dashboard.RefreshCompleted += (usage, error) =>
        {
            activeRefreshToast?.Close();
            activeRefreshToast = usage is not null
                ? new RefreshToast(usage)
                : new RefreshToast(error ?? Ui.S("Dashboard.RefreshFailed"), false);
            activeRefreshToast.Show();
        };
        dashboard.FormClosing += (_, eventArgs) =>
        {
            if (allowClose || eventArgs.CloseReason is CloseReason.ApplicationExitCall or CloseReason.WindowsShutDown) return;
            eventArgs.Cancel = true;
            dashboard.Hide();
        };

        SetRefreshInterval(Math.Clamp(settings.RefreshIntervalMinutes, 1, 5));
        dashboard.SetAutoRecharge(settings.AutoRechargeThreshold, settings.AutoRechargeTarget);
        dashboard.CreateControl();
        dashboard.StartRealtimeMonitoring();
        dashboard.RefreshUsage();
        Application.Run(applicationContext);
        showDashboardRegistration.Unregister(null);
    }

    private static void CreateStartupShortcut(string executable)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows kan geen opstartsnelkoppeling maken.");
        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Windows kan geen opstartsnelkoppeling maken.");
        dynamic shortcut = shell.CreateShortcut(StartupShortcutPath);
        shortcut.TargetPath = executable;
        shortcut.WorkingDirectory = Path.GetDirectoryName(executable);
        shortcut.Description = AppTitle;
        shortcut.IconLocation = $"{executable},0";
        shortcut.Save();
    }

    private static void VerifyCreditPaceDetection()
    {
        var now = DateTimeOffset.UtcNow;
        var rapid = CreditSpendRateDetector.Analyze(
        [
            new CreditBalanceSample(now.AddMinutes(-55), 70m),
            new CreditBalanceSample(now, 39m)
        ]);
        var calm = CreditSpendRateDetector.Analyze(
        [
            new CreditBalanceSample(now.AddMinutes(-55), 70m),
            new CreditBalanceSample(now, 66m)
        ]);
        var afterTopUp = CreditSpendRateDetector.Analyze(
        [
            new CreditBalanceSample(now.AddMinutes(-55), 22m),
            new CreditBalanceSample(now.AddMinutes(-50), 250m),
            new CreditBalanceSample(now, 215m)
        ]);

        if (rapid is not { AlertLevel: CreditSpendAlertLevel.Rapid } ||
            calm?.AlertLevel != CreditSpendAlertLevel.None ||
            afterTopUp?.AlertLevel != CreditSpendAlertLevel.Rapid)
            throw new InvalidOperationException("Credit pace detection verification failed.");
    }

    private static void RunDashboardPreview(decimal balance, double weekPercent)
    {
        var now = DateTimeOffset.Now;
        using var preview = new DashboardForm();
        preview.SetAutoRecharge(125, 250);
        preview.ShowPreviewUsage(new UsageSummary(
            true,
            balance,
            42d,
            weekPercent,
            now,
            128,
            1_248_730,
            [new SessionUsage("preview", "gpt-5.6-sol", now.AddMinutes(-8), 6, 82_400)],
            [new CreditBalanceSample(now.AddMinutes(-30), balance + 4m), new CreditBalanceSample(now, balance)],
            null));
        Application.Run(preview);
    }

    private static void RenderDashboardPreview(decimal balance, double weekPercent)
    {
        using var preview = new DashboardForm();
        preview.SetAutoRecharge(125, 250);
        preview.ShowPreviewUsage(CreatePreviewUsage(balance, weekPercent));
        preview.Show();
        Application.DoEvents();
        using var image = new Bitmap(preview.Width, preview.Height);
        preview.DrawToBitmap(image, new Rectangle(Point.Empty, preview.Size));
        image.Save(Path.Combine(AppContext.BaseDirectory, "dashboard-preview.png"));
    }

    private static UsageSummary CreatePreviewUsage(decimal balance, double weekPercent)
    {
        var now = DateTimeOffset.Now;
        return new UsageSummary(
            true,
            balance,
            42d,
            weekPercent,
            now,
            128,
            1_248_730,
            [new SessionUsage("preview", "gpt-5.6-sol", now.AddMinutes(-8), 6, 82_400)],
            [new CreditBalanceSample(now.AddMinutes(-30), balance + 4m), new CreditBalanceSample(now, balance)],
            null);
    }

    private static Icon CreateAppIcon()
    {
        using var bitmap = new Bitmap(64, 64);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var background = RoundedPath(new RectangleF(2, 2, 60, 60), 15);
        using var backgroundBrush = new SolidBrush(Color.FromArgb(14, 27, 45));
        graphics.FillPath(backgroundBrush, background);

        using var gaugePen = new Pen(Color.FromArgb(87, 184, 240), 6) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        graphics.DrawArc(gaugePen, 11, 11, 42, 42, 130, 245);
        using var accentBrush = new SolidBrush(Color.FromArgb(90, 211, 160));
        graphics.FillEllipse(accentBrush, 43, 13, 9, 9);

        using var trendPen = new Pen(Color.White, 5) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        graphics.DrawLines(trendPen, [new Point(18, 40), new Point(28, 31), new Point(35, 36), new Point(46, 23)]);
        graphics.FillEllipse(Brushes.White, 43, 20, 7, 7);

        var handle = bitmap.GetHicon();
        try
        {
            using var temporaryIcon = Icon.FromHandle(handle);
            return (Icon)temporaryIcon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static GraphicsPath RoundedPath(RectangleF rectangle, float radius)
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

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr iconHandle);
}
