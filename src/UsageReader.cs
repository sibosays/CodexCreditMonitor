using System.Globalization;
using System.Text.Json;

namespace CodexCreditMonitor;

internal sealed record UsageSummary(
    bool HasData,
    decimal? CreditBalance,
    double? FiveHourPercent,
    double? WeekPercent,
    DateTimeOffset? LastUpdate,
    int TodayRequests,
    long TodayTokens,
    IReadOnlyList<SessionUsage> RecentSessions,
    string? Error);

internal sealed record SessionUsage(string Name, string Model, DateTimeOffset LastUpdate, int Requests, long Tokens);

internal static class UsageReader
{
    public static UsageSummary Read()
    {
        try
        {
            var sessionsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "sessions");
            if (!Directory.Exists(sessionsRoot))
                return new UsageSummary(false, null, null, null, null, 0, 0, [], "Geen lokale Codex-sessies gevonden.");

            var newest = new LatestState();
            var sessions = new List<SessionUsage>();
            var today = DateTimeOffset.Now.Date;

            var earliestRelevantFile = DateTime.Now.AddDays(-8);
            foreach (var path in Directory.EnumerateFiles(sessionsRoot, "*.jsonl", SearchOption.AllDirectories)
                         .Where(path => File.GetLastWriteTime(path) >= earliestRelevantFile))
            {
                sessions.AddRange(ParseSession(path, today, newest));
            }

            var todaySessions = sessions.Where(s => s.LastUpdate.LocalDateTime.Date == today)
                .OrderByDescending(s => s.LastUpdate)
                .ToList();

            return new UsageSummary(
                newest.LastUpdate is not null,
                newest.CreditBalance,
                newest.FiveHourPercent,
                newest.WeekPercent,
                newest.LastUpdate,
                todaySessions.Sum(s => s.Requests),
                todaySessions.Sum(s => s.Tokens),
                todaySessions.Take(5).ToList(),
                newest.LastUpdate is null ? "Nog geen gebruiksgegevens gevonden." : null);
        }
        catch (Exception ex)
        {
            return new UsageSummary(false, null, null, null, null, 0, 0, [], ex.Message);
        }
    }

    private static IReadOnlyList<SessionUsage> ParseSession(string path, DateTimeOffset today, LatestState newest)
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
                {
                    newest.LastUpdate = timestamp;
                    if (payload.TryGetProperty("rate_limits", out var limits))
                    {
                        newest.CreditBalance = GetDecimalPath(limits, "credits", "balance");
                        newest.FiveHourPercent = GetDoublePath(limits, "primary", "used_percent");
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

    private static string ReadableSessionName(string value)
    {
        var marker = value.LastIndexOf('-');
        return marker > 0 ? value[..marker].Replace("rollout-", "Sessie ") : value;
    }

    private sealed class LatestState
    {
        public decimal? CreditBalance { get; set; }
        public double? FiveHourPercent { get; set; }
        public double? WeekPercent { get; set; }
        public DateTimeOffset? LastUpdate { get; set; }
    }

    private sealed class ModelUsage
    {
        public int Requests { get; set; }
        public long Tokens { get; set; }
        public DateTimeOffset? LastUpdate { get; set; }
    }
}
