using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CodexCreditMonitor;

internal static class Program
{
    private const string AppTitle = "Codex Credit Monitor";
    internal static readonly Icon AppIcon = CreateAppIcon();
    private static string StartupShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
        "Codex Credit Monitor.lnk");

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var commandLine = Environment.GetCommandLineArgs();
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
            settingsForm.ShowDialog();
        }

        void ShowInfo()
        {
            var source = Path.Combine(AppContext.BaseDirectory, "WhatsNew.md");
            var message = File.Exists(source)
                ? File.ReadAllText(source)
                : "Codex Credit Monitor\n\nInformatie is niet beschikbaar.";
            var version = typeof(Program).Assembly.GetName().Version?.ToString(2) ?? "—";
            using var info = new InfoForm(version, message);
            info.ShowDialog();
        }

        menu.Items.Add("Dashboard openen", null, (_, _) => ShowDashboard());
        menu.Items.Add("Nu vernieuwen", null, (_, _) =>
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
        menu.Items.Add("Instellingen…", null, (_, _) => ShowSettings());
        menu.Items.Add("Info…", null, (_, _) => ShowInfo());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Afsluiten", null, (_, _) =>
        {
            allowClose = true;
            dashboard.Close();
            applicationContext.ExitThread();
        });

        trayIcon.MouseClick += (_, eventArgs) =>
        {
            if (eventArgs.Button == MouseButtons.Left) ShowDashboard();
        };
        dashboard.WarningRaised += percent =>
        {
            if (!settings.AlertsEnabled) return;
            if (settings.AlertSoundEnabled) System.Media.SystemSounds.Exclamation.Play();
            new UsageAlertToast(percent).Show();
        };
        dashboard.SettingsRequested += ShowSettings;
        dashboard.RefreshCompleted += (usage, error) =>
        {
            activeRefreshToast?.Close();
            activeRefreshToast = usage is not null
                ? new RefreshToast(usage)
                : new RefreshToast(error ?? "Vernieuwen is niet gelukt.", false);
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
