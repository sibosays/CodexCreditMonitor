using System.Globalization;
using System.Text.Json;

namespace CodexCreditMonitor;

internal sealed record UsageSummary(
    bool HasData,
    decimal? CreditBalance,
    double? FiveHourPercent,
    DateTimeOffset? FiveHourResetsAt,
    double? WeekPercent,
    DateTimeOffset? LastUpdate,
    int TodayRequests,
    long TodayTokens,
    IReadOnlyList<SessionUsage> RecentSessions,
    IReadOnlyList<CreditBalanceSample> CreditBalanceHistory,
    string? Error);

internal sealed record SessionUsage(string Name, string Model, DateTimeOffset LastUpdate, int Requests, long Tokens);
internal sealed record CreditBalanceSample(DateTimeOffset Timestamp, decimal Balance);
internal sealed record UsageLimitSnapshot(
    decimal? CreditBalance,
    double? FiveHourPercent,
    DateTimeOffset? FiveHourResetsAt,
    double? WeekPercent,
    DateTimeOffset UpdatedAt);

internal static class UsageReader
{
    public static UsageLimitSnapshot? ReadLatestLimits()
    {
        try
        {
            var sessionsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "sessions");
            if (!Directory.Exists(sessionsRoot)) return null;

            UsageLimitSnapshot? newest = null;
            var recentFiles = Directory.EnumerateFiles(sessionsRoot, "*.jsonl", SearchOption.AllDirectories)
                .Select(path => new { Path = path, Modified = File.GetLastWriteTimeUtc(path) })
                .Where(file => file.Modified >= DateTime.UtcNow.AddDays(-8))
                .OrderByDescending(file => file.Modified)
                .Take(8);

            foreach (var file in recentFiles)
            {
                using var stream = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    if (line.Length == 0) continue;
                    try
                    {
                        using var document = JsonDocument.Parse(line);
                        var root = document.RootElement;
                        if (!TryGetTimestamp(root, out var timestamp) ||
                            newest is not null && timestamp <= newest.UpdatedAt ||
                            !root.TryGetProperty("type", out var type) || type.GetString() != "event_msg" ||
                            !root.TryGetProperty("payload", out var payload) ||
                            !payload.TryGetProperty("type", out var payloadType) || payloadType.GetString() != "token_count" ||
                            !TryGetCodexLimits(payload, out var limits)) continue;

                        newest = new UsageLimitSnapshot(
                            GetDecimalPath(limits, "credits", "balance"),
                            GetDoublePath(limits, "primary", "used_percent"),
                            GetUnixTimePath(limits, "primary", "resets_at"),
                            GetDoublePath(limits, "secondary", "used_percent"),
                            timestamp);
                    }
                    catch (JsonException)
                    {
                        // The active log may end in a partial line; a later refresh retries it.
                    }
                }

                // File modification order normally locates the current limits immediately.
                if (newest is not null && file.Modified < newest.UpdatedAt.UtcDateTime) break;
            }

            return newest;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static UsageSummary Read()
    {
        try
        {
            var sessionsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "sessions");
            if (!Directory.Exists(sessionsRoot))
                return new UsageSummary(false, null, null, null, null, null, 0, 0, [], [], Ui.T("Geen lokale Codex-sessies gevonden.", "No local Codex sessions found."));

            var newest = new LatestState();
            var sessions = new List<SessionUsage>();
            var creditBalanceHistory = new List<CreditBalanceSample>();
            var today = DateTimeOffset.Now.Date;

            var earliestRelevantFile = DateTime.Now.AddDays(-8);
            foreach (var path in Directory.EnumerateFiles(sessionsRoot, "*.jsonl", SearchOption.AllDirectories)
                         .Where(path => File.GetLastWriteTime(path) >= earliestRelevantFile))
            {
                sessions.AddRange(ParseSession(path, today, newest, creditBalanceHistory));
            }

            var todaySessions = sessions.Where(s => s.LastUpdate.LocalDateTime.Date == today)
                .OrderByDescending(s => s.LastUpdate)
                .ToList();

            return new UsageSummary(
                newest.LastUpdate is not null,
                newest.CreditBalance,
                newest.FiveHourPercent,
                newest.FiveHourResetsAt,
                newest.WeekPercent,
                newest.LastUpdate,
                todaySessions.Sum(s => s.Requests),
                todaySessions.Sum(s => s.Tokens),
                todaySessions.Take(5).ToList(),
                creditBalanceHistory
                    // Keep enough local history to recover the newest valid pace after a
                    // quiet period or restart. The detector itself still measures at most
                    // a two-hour interval and refuses to cross a top-up boundary.
                    .Where(sample => sample.Timestamp >= DateTimeOffset.Now.AddDays(-8))
                    .OrderBy(sample => sample.Timestamp)
                    .ToList(),
                newest.LastUpdate is null ? "Nog geen gebruiksgegevens gevonden." : null);
        }
        catch (Exception ex)
        {
            return new UsageSummary(false, null, null, null, null, null, 0, 0, [], [], ex.Message);
        }
    }

    private static IReadOnlyList<SessionUsage> ParseSession(string path, DateTimeOffset today, LatestState newest, ICollection<CreditBalanceSample> creditBalanceHistory)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var model = "Onbekend model";
        var byModel = new Dictionary<string, ModelUsage>(StringComparer.OrdinalIgnoreCase);
        DateTimeOffset? last = null;

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            try
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;
                if (!root.TryGetProperty("type", out var typeProperty)) continue;
                var type = typeProperty.GetString();

                if (type == "session_meta" && root.TryGetProperty("payload", out var metaPayload))
                {
                    model = FindString(metaPayload, "model") ?? model;
                    continue;
                }

                if (type != "event_msg" || !root.TryGetProperty("payload", out var payload)) continue;
                if (!payload.TryGetProperty("type", out var payloadType)) continue;

                // Model changes are logged as events after a session has started. Keep the active
                // model in sync so each following token_count is allocated correctly.
                if (payloadType.GetString() == "thread_settings_applied")
                {
                    model = FindString(payload, "model") ?? model;
                    continue;
                }

                if (payloadType.GetString() != "token_count") continue;
                if (!TryGetTimestamp(root, out var timestamp)) continue;
                last = !last.HasValue || timestamp > last ? timestamp : last;

                if (timestamp.LocalDateTime.Date == today && payload.TryGetProperty("info", out var info) &&
                    info.ValueKind == JsonValueKind.Object && info.TryGetProperty("last_token_usage", out var usage) &&
                    usage.ValueKind == JsonValueKind.Object)
                {
                    if (!byModel.TryGetValue(model, out var modelUsage))
                    {
                        modelUsage = new ModelUsage();
                        byModel[model] = modelUsage;
                    }
                    modelUsage.Requests++;
                    modelUsage.Tokens += GetLong(usage, "total_tokens") ??
                                         (GetLong(usage, "input_tokens") ?? 0) + (GetLong(usage, "output_tokens") ?? 0);
                    modelUsage.LastUpdate = !modelUsage.LastUpdate.HasValue || timestamp > modelUsage.LastUpdate ? timestamp : modelUsage.LastUpdate;
                }

                if (!newest.LastUpdate.HasValue || timestamp >= newest.LastUpdate)
                    newest.LastUpdate = timestamp;

                // A token event can carry multiple rate-limit scopes. Only the Codex scope
                // represents the balance and 5-hour/week allowances shown by this monitor.
                // For example, a later "premium" scope has no windows and a placeholder
                // zero balance; accepting it would erase the valid Codex values.
                if (TryGetCodexLimits(payload, out var limits))
                {
                    var balance = GetDecimalPath(limits, "credits", "balance");
                    if (balance is decimal value)
                        creditBalanceHistory.Add(new CreditBalanceSample(timestamp, value));

                    if (!newest.RateLimitsUpdate.HasValue || timestamp >= newest.RateLimitsUpdate)
                    {
                        newest.RateLimitsUpdate = timestamp;
                        newest.CreditBalance = balance;
                        newest.FiveHourPercent = GetDoublePath(limits, "primary", "used_percent");
                        newest.FiveHourResetsAt = GetUnixTimePath(limits, "primary", "resets_at");
                        newest.WeekPercent = GetDoublePath(limits, "secondary", "used_percent");
                    }
                }
            }
            catch (JsonException)
            {
                // An incomplete active log line is safe to skip; it will be read on the next refresh.
            }
        }

        return byModel.Select(entry => new SessionUsage(
                ReadableSessionName(name),
                entry.Key,
                entry.Value.LastUpdate ?? last ?? DateTimeOffset.MinValue,
                entry.Value.Requests,
                entry.Value.Tokens))
            .ToList();
    }

    private static bool TryGetTimestamp(JsonElement root, out DateTimeOffset timestamp)
    {
        timestamp = default;
        return root.TryGetProperty("timestamp", out var value) &&
               DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out timestamp);
    }

    private static string? FindString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.String) return property.Value.GetString();
                var found = FindString(property.Value, propertyName);
                if (found is not null) return found;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var found = FindString(item, propertyName);
                if (found is not null) return found;
            }
        }
        return null;
    }

    private static long? GetLong(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.TryGetInt64(out var result) ? result : null;

    private static decimal? GetDecimalPath(JsonElement element, string parent, string property)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        if (!element.TryGetProperty(parent, out var child) || !child.TryGetProperty(property, out var value)) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var numeric)) return numeric;
        return value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static double? GetDoublePath(JsonElement element, string parent, string property)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(parent, out var child) &&
           child.ValueKind == JsonValueKind.Object && child.TryGetProperty(property, out var value) && value.TryGetDouble(out var result) ? result : null;

    private static DateTimeOffset? GetUnixTimePath(JsonElement element, string parent, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(parent, out var child) ||
            child.ValueKind != JsonValueKind.Object || !child.TryGetProperty(property, out var value)) return null;
        if (!value.TryGetInt64(out var seconds)) return null;
        try { return DateTimeOffset.FromUnixTimeSeconds(seconds); }
        catch (ArgumentOutOfRangeException) { return null; }
    }

    // Codex has used both a single `rate_limits` object and a per-limit map.
    // Keep the monitor compatible with either local log shape, without guessing
    // values from a non-Codex scope such as "premium".
    private static bool TryGetCodexLimits(JsonElement payload, out JsonElement limits)
    {
        limits = default;
        if (payload.ValueKind != JsonValueKind.Object) return false;

        if (payload.TryGetProperty("rate_limits", out var direct) && IsCodexLimit(direct))
        {
            limits = direct;
            return true;
        }

        foreach (var mapName in new[] { "rate_limits_by_limit_id", "rateLimitsByLimitId" })
        {
            if (payload.TryGetProperty(mapName, out var map) &&
                map.ValueKind == JsonValueKind.Object &&
                map.TryGetProperty("codex", out var codex) &&
                codex.ValueKind == JsonValueKind.Object)
            {
                limits = codex;
                return true;
            }
        }

        return false;
    }

    private static bool IsCodexLimit(JsonElement limits)
        => limits.ValueKind == JsonValueKind.Object &&
           limits.TryGetProperty("limit_id", out var limitId) &&
           string.Equals(limitId.GetString(), "codex", StringComparison.OrdinalIgnoreCase);

    private static string ReadableSessionName(string value)
    {
        var marker = value.LastIndexOf('-');
        return marker > 0 ? value[..marker].Replace("rollout-", "Sessie ") : value;
    }

    private sealed class LatestState
    {
        public decimal? CreditBalance { get; set; }
        public double? FiveHourPercent { get; set; }
        public DateTimeOffset? FiveHourResetsAt { get; set; }
        public double? WeekPercent { get; set; }
        public DateTimeOffset? LastUpdate { get; set; }
        public DateTimeOffset? RateLimitsUpdate { get; set; }
    }

    private sealed class ModelUsage
    {
        public int Requests { get; set; }
        public long Tokens { get; set; }
        public DateTimeOffset? LastUpdate { get; set; }
    }
}
