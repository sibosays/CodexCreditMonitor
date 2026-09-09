using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CodexCreditMonitor;

internal static class Program
{
    private const string AppTitle = "Codex Credit Monitor";
    // Give Explorer a stable identity before any window is created. Without
    // this, taskbar grouping can retain a generic icon for the native host.
    private const string AppUserModelId = "com.cskbouwens.CodexCreditMonitor";
    // ChatGPT owns purchasing and flexible-credit settings; this monitor only provides a shortcut.
    private const string CreditSettingsUrl = "https://chatgpt.com/#settings/Usage";
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
        var commandLine = Environment.GetCommandLineArgs();
        RestorePortableDataDirectory(commandLine);
        if (ShouldDetachFromParent(commandLine) && TryStartIndependent(commandLine)) return;

        ApplicationConfiguration.Initialize();
        SetTaskbarIdentity();
        var showDashboardOnLaunch = commandLine.Contains("--show-dashboard", StringComparer.OrdinalIgnoreCase);
        UsageSummary? previewUsage = null;
        if (commandLine.Contains("--preview-low-credits", StringComparer.OrdinalIgnoreCase))
        {
            previewUsage = CreatePreviewUsage(50m, 68d);
        }
        else if (commandLine.Contains("--preview-use-credits", StringComparer.OrdinalIgnoreCase))
        {
            previewUsage = CreatePreviewUsage(80m, 100d);
        }
        else if (commandLine.Contains("--preview-credit-actions", StringComparer.OrdinalIgnoreCase))
        {
            previewUsage = CreatePreviewUsage(34m, 100d);
        }
        if (previewUsage is not null)
        {
            showDashboardOnLaunch = true;
        }
        if (commandLine.Contains("--render-settings-en", StringComparer.OrdinalIgnoreCase) ||
            commandLine.Contains("--render-settings-nl", StringComparer.OrdinalIgnoreCase))
        {
            var language = commandLine.Contains("--render-settings-en", StringComparer.OrdinalIgnoreCase) ? "en" : "nl";
            Ui.SetLanguage(language);
            RenderSettingsPreview(language);
            return;
        }
        if (commandLine.Contains("--render-info-preview", StringComparer.OrdinalIgnoreCase))
        {
            Ui.SetLanguage("en");
            RenderInfoPreview();
            return;
        }
        if (commandLine.Contains("--render-credit-actions", StringComparer.OrdinalIgnoreCase) ||
            commandLine.Contains("--render-credit-actions-nl", StringComparer.OrdinalIgnoreCase))
        {
            // The public release image is deliberately English, independent of
            // the Windows display language or a user's saved app preference.
            var language = commandLine.Contains("--render-credit-actions-nl", StringComparer.OrdinalIgnoreCase) ? "nl" : "en";
            Ui.SetLanguage(language);
            RenderDashboardPreview(34m, 100d, language == "nl" ? "dashboard-preview-nl.png" : "dashboard-preview.png");
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
            using var preview = new CreditSpendRateAlertToast(new CreditSpendRate(31m, 39m, TimeSpan.FromMinutes(55), 33.8m, CreditSpendAlertLevel.Critical));
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

        var settings = MonitorSettings.Load();
        _settingsForRestart = settings;
        Ui.SetLanguage(settings.Language);
        using var dashboard = new DashboardForm(settings);
        // The tray activation listener can receive a signal before the dashboard is shown.
        // Force a handle now so its UI callback always has a safe target.
        _ = dashboard.Handle;
        using var applicationContext = new ApplicationContext();
        using var showDashboardSignal = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\CodexCreditMonitor.ShowDashboard");
        var allowClose = false;
        RefreshStartupShortcutIfEnabled();
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
                if (enabled)
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
            var source = Path.Combine(AppContext.BaseDirectory, "README.md");
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
        if (previewUsage is not null)
        {
            dashboard.ShowPreviewUsage(previewUsage);
        }
        else
        {
            dashboard.StartRealtimeMonitoring();
            dashboard.RefreshUsage();
        }
        if (showDashboardOnLaunch)
        {
            EventHandler? showAfterStartup = null;
            showAfterStartup = (_, _) =>
            {
                Application.Idle -= showAfterStartup;
                ShowDashboard();
                if (previewUsage is not null)
                {
                    _ = PulsePreviewActionsAfterDashboardIsVisibleAsync(dashboard);
                }
            };
            Application.Idle += showAfterStartup;
        }
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

    private static void RefreshStartupShortcutIfEnabled()
    {
        if (MonitorSettings.IsPortableMode || !File.Exists(StartupShortcutPath)) return;
        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable)) return;

        try
        {
            CreateStartupShortcut(executable);
        }
        catch
        {
            // Monitoring remains available when a corporate policy blocks shortcut writes.
        }
    }

    private static void SetTaskbarIdentity()
    {
        try
        {
            _ = SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
        }
        catch (EntryPointNotFoundException)
        {
            // Older Windows installations simply use the executable icon.
        }
    }

    private static bool ShouldDetachFromParent(string[] commandLine)
    {
        if (commandLine.Any(argument => argument.StartsWith("--preview-", StringComparison.OrdinalIgnoreCase) ||
                                        argument.StartsWith("--render-", StringComparison.OrdinalIgnoreCase) ||
                                        argument.StartsWith("--verify-", StringComparison.OrdinalIgnoreCase))) return false;
        return !IndependentLaunch.IsIndependent();
    }

    private static bool TryStartIndependent(string[] commandLine)
    {
        try
        {
            // A marker is not evidence of independence. Refuse a second hand-off
            // if Windows did not produce the verified shell-owned process.
            if (commandLine.Contains(IndependentLaunch.Marker, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Windows could not start an independent monitor process.");
            var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Application path unavailable.");
            var arguments = commandLine.Skip(1).ToList();
            arguments.Add(IndependentLaunch.Marker);
            var portableData = Environment.GetEnvironmentVariable("CODEX_CREDIT_MONITOR_DATA_DIR");
            if (!string.IsNullOrWhiteSpace(portableData))
                arguments.Add("--ccm-data=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(portableData)));
            IndependentLaunch.Start(executable, arguments, Path.GetDirectoryName(executable)!);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "Codex Credit Monitor could not start independently. Open the app directly from Windows Explorer.\n\n" + exception.Message,
                AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        // Never silently run inside the launcher's job after a failed hand-off.
        return true;
    }
    private static void RestorePortableDataDirectory(IEnumerable<string> commandLine)
    {
        const string prefix = "--ccm-data=";
        var encoded = commandLine.FirstOrDefault(argument => argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (encoded is null) return;
        try
        {
            var directory = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded[prefix.Length..]));
            if (!string.IsNullOrWhiteSpace(directory))
                Environment.SetEnvironmentVariable("CODEX_CREDIT_MONITOR_DATA_DIR", directory);
        }
        catch (FormatException)
        {
            // Ignore malformed internal hand-off arguments and use normal settings storage.
        }
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
        var invalidNegativeBalance = CreditSpendRateDetector.Analyze(
        [
            new CreditBalanceSample(now.AddMinutes(-20), 10m),
            new CreditBalanceSample(now, -20m)
        ]);
        var invalidShortSpike = CreditSpendRateDetector.Analyze(
        [
            new CreditBalanceSample(now.AddSeconds(-20), 100m),
            new CreditBalanceSample(now, 1m)
        ]);
        var recoveredHistoricalRate = CreditSpendRateDetector.AnalyzeLatestValid(
        [
            new CreditBalanceSample(now.AddHours(-4), 80m),
            new CreditBalanceSample(now.AddHours(-3).AddMinutes(-30), 76m),
            new CreditBalanceSample(now.AddMinutes(-30), 76m),
            new CreditBalanceSample(now, 76m)
        ]);
        var historicalRateAcrossTopUp = CreditSpendRateDetector.AnalyzeLatestValid(
        [
            new CreditBalanceSample(now.AddHours(-2), 20m),
            new CreditBalanceSample(now.AddHours(-1), 250m),
            new CreditBalanceSample(now, 250m)
        ]);
        var persisted = System.Text.Json.JsonSerializer.Deserialize<MonitorSettings>(
            System.Text.Json.JsonSerializer.Serialize(new MonitorSettings
            {
                LastValidCreditsPerHour = 12.5m,
                LastCreditPaceMeasuredAt = now
            }));

        if (rapid is not { AlertLevel: CreditSpendAlertLevel.Rapid } ||
            calm?.AlertLevel != CreditSpendAlertLevel.None ||
            afterTopUp?.AlertLevel != CreditSpendAlertLevel.Rapid ||
            invalidNegativeBalance is not null ||
            invalidShortSpike is not null ||
            recoveredHistoricalRate?.CreditsPerHour != 8m ||
            historicalRateAcrossTopUp is not null ||
            !CreditSpendRateDetector.HasRecentTopUp(
            [
                new CreditBalanceSample(now.AddHours(-1), 20m),
                new CreditBalanceSample(now, 250m)
            ]) ||
            persisted?.LastValidCreditsPerHour != 12.5m ||
            persisted.LastCreditPaceMeasuredAt != now)
            throw new InvalidOperationException("Credit pace detection verification failed.");
    }

    private static void RenderDashboardPreview(decimal balance, double weekPercent, string outputFileName = "dashboard-preview.png")
    {
        using var preview = new DashboardForm(new MonitorSettings());
        // A borderless surface makes this an exact dashboard capture rather
        // than a DPI-dependent mix of client area and Windows title bar.
        preview.FormBorderStyle = FormBorderStyle.None;
        preview.SetAutoRecharge(125, 250);
        preview.ShowPreviewUsage(CreatePreviewUsage(balance, weekPercent));
        preview.Show();
        Application.DoEvents();
        preview.PulseVisibleCreditActions();
        // The credit cues intentionally begin after the window settles. Wait
        // until their first visible frame before capturing the release image.
        Thread.Sleep(800);
        Application.DoEvents();
        // Capture the client area directly. This avoids non-client title-bar
        // clipping that DrawToBitmap can produce at scaled Windows DPI.
        using var image = new Bitmap(preview.ClientSize.Width, preview.ClientSize.Height);
        preview.DrawToBitmap(image, new Rectangle(Point.Empty, preview.ClientSize));
        image.Save(Path.Combine(AppContext.BaseDirectory, outputFileName));
    }

    private static void RenderSettingsPreview(string language)
    {
        var settings = new MonitorSettings { Language = language };
        using var preview = new SettingsForm(settings, () => true, _ => true, _ => { }, allowStartup: true);
        preview.Show();
        Application.DoEvents();
        using var image = new Bitmap(preview.Width, preview.Height);
        preview.DrawToBitmap(image, new Rectangle(Point.Empty, preview.Size));
        image.Save(Path.Combine(AppContext.BaseDirectory, $"settings-{language}-preview.png"));
    }

    private static void RenderInfoPreview()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "README.md");
        var message = File.Exists(source) ? Ui.SelectInfo(File.ReadAllText(source)) : "Product information unavailable.";
        using var preview = new InfoForm("2.2", message);
        preview.Show();
        PumpPreviewMessages(180);
        using var image = new Bitmap(preview.Width, preview.Height);
        preview.DrawToBitmap(image, new Rectangle(Point.Empty, preview.Size));
        image.Save(Path.Combine(AppContext.BaseDirectory, "info-preview.png"));
        PumpPreviewMessages(600);
        using var laterImage = new Bitmap(preview.Width, preview.Height);
        preview.DrawToBitmap(laterImage, new Rectangle(Point.Empty, preview.Size));
        laterImage.Save(Path.Combine(AppContext.BaseDirectory, "info-preview-later.png"));
    }

    private static void PumpPreviewMessages(int milliseconds)
    {
        var until = Environment.TickCount64 + milliseconds;
        while (Environment.TickCount64 < until)
        {
            Application.DoEvents();
            Thread.Sleep(15);
        }
        Application.DoEvents();
    }

    private static UsageSummary CreatePreviewUsage(decimal balance, double weekPercent)
    {
        var now = DateTimeOffset.Now;
        return new UsageSummary(
            true,
            balance,
            42d,
            now.AddHours(3).AddMinutes(48),
            weekPercent,
            now,
            128,
            1_248_730,
            [new SessionUsage("preview", "gpt-5.6-sol", now.AddMinutes(-8), 6, 82_400)],
            [new CreditBalanceSample(now.AddMinutes(-30), balance + 4m), new CreditBalanceSample(now, balance)],
            null);
    }

    private static async Task PulsePreviewActionsAfterDashboardIsVisibleAsync(DashboardForm dashboard)
    {
        await Task.Delay(5_000);
        if (!dashboard.IsDisposed) dashboard.PulseVisibleCreditActions();
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

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appID);


}
