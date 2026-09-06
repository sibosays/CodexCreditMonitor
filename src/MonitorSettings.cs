using System.Text.Json;

namespace CodexCreditMonitor;

internal sealed class MonitorSettings
{
    public bool AlertsEnabled { get; set; } = true;
    public bool AlertSoundEnabled { get; set; } = true;
    public int RefreshIntervalMinutes { get; set; } = 2;
    public int AutoRechargeThreshold { get; set; } = 125;
    public int AutoRechargeTarget { get; set; } = 250;
    public string Language { get; set; } = "auto";
    public bool ReopenDashboardAfterLanguageChange { get; set; }
    public decimal? LastValidCreditsPerHour { get; set; }
    public DateTimeOffset? LastCreditPaceMeasuredAt { get; set; }

    public static bool IsPortableMode => !string.IsNullOrWhiteSpace(
        Environment.GetEnvironmentVariable("CODEX_CREDIT_MONITOR_DATA_DIR"));

    private static string SettingsDirectory
    {
        get
        {
            var portableDirectory = Environment.GetEnvironmentVariable("CODEX_CREDIT_MONITOR_DATA_DIR");
            return string.IsNullOrWhiteSpace(portableDirectory)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Codex Credit Monitor")
                : portableDirectory;
        }
    }

    private static string FilePath => Path.Combine(SettingsDirectory, "settings.json");

    public static MonitorSettings Load()
    {
        foreach (var path in new[] { FilePath }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (File.Exists(path))
                    return JsonSerializer.Deserialize<MonitorSettings>(File.ReadAllText(path)) ?? new MonitorSettings();
            }
            catch (Exception)
            {
                // Try the next known location. Existing settings remain optional.
            }
        }
        return new MonitorSettings();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch (Exception)
        {
            // Monitoring must remain available when Windows temporarily blocks the settings file.
            // The current in-memory settings continue to apply for this session.
        }
    }
}
