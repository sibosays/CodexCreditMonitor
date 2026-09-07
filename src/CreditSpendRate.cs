namespace CodexCreditMonitor;

internal enum CreditSpendAlertLevel
{
    None,
    Rapid,
    Critical
}

internal sealed record CreditSpendRate(
    decimal CurrentBalance,
    decimal CreditsSpent,
    TimeSpan ObservedOver,
    decimal CreditsPerHour,
    CreditSpendAlertLevel AlertLevel);

internal static class CreditSpendRateDetector
{
    internal const decimal MaxCredibleCreditsPerHour = 10_000m;
    private static readonly TimeSpan MinimumObservation = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ShortWindow = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LongWindow = TimeSpan.FromHours(2);
    private const decimal RapidShortDrop = 12m;
    private const decimal CriticalShortDrop = 24m;
    private const decimal RapidLongDrop = 30m;
    private const decimal CriticalLongDrop = 60m;
    private const decimal BalanceTopUpTolerance = 5m;

    public static CreditSpendRate? Analyze(IReadOnlyList<CreditBalanceSample> samples)
    {
        var history = samples
            .Where(sample => sample.Balance >= 0m && sample.Timestamp != default)
            .OrderBy(sample => sample.Timestamp)
            .GroupBy(sample => sample.Timestamp)
            .Select(group => group.Last())
            .ToList();

        if (history.Count < 2) return null;

        var current = history[^1];
        // A reset or credit top-up makes the balance jump upward. Measure the following
        // consumption as a fresh run instead of comparing it with the old, lower balance.
        var lastTopUp = 0;
        for (var index = 1; index < history.Count; index++)
        {
            if (history[index].Balance - history[index - 1].Balance >= BalanceTopUpTolerance)
                lastTopUp = index;
        }

        var currentRun = history.Skip(lastTopUp).ToList();
        var longBaseline = currentRun.FirstOrDefault(sample => sample.Timestamp >= current.Timestamp - LongWindow);
        if (longBaseline is null || longBaseline.Timestamp >= current.Timestamp || longBaseline.Balance <= current.Balance) return null;

        var longSpend = longBaseline.Balance - current.Balance;
        var longDuration = current.Timestamp - longBaseline.Timestamp;
        if (longSpend <= 0m || longDuration < MinimumObservation) return null;
        var rate = longSpend / Math.Max(0.01m, (decimal)longDuration.TotalHours);
        if (rate < 0m || rate > MaxCredibleCreditsPerHour) return null;

        var shortBaseline = currentRun.FirstOrDefault(sample => sample.Timestamp >= current.Timestamp - ShortWindow);
        var shortSpend = shortBaseline is not null && shortBaseline.Timestamp < current.Timestamp && shortBaseline.Balance > current.Balance
            ? shortBaseline.Balance - current.Balance
            : 0m;

        var alertLevel = CreditSpendAlertLevel.None;
        var alertSpend = longSpend;
        var alertDuration = longDuration;
        if (shortSpend >= CriticalShortDrop || longSpend >= CriticalLongDrop)
        {
            alertLevel = CreditSpendAlertLevel.Critical;
            if (shortSpend >= CriticalShortDrop)
            {
                alertSpend = shortSpend;
                alertDuration = current.Timestamp - shortBaseline!.Timestamp;
            }
        }
        else if (shortSpend >= RapidShortDrop || longSpend >= RapidLongDrop)
        {
            alertLevel = CreditSpendAlertLevel.Rapid;
            if (shortSpend >= RapidShortDrop)
            {
                alertSpend = shortSpend;
                alertDuration = current.Timestamp - shortBaseline!.Timestamp;
            }
        }

        return new CreditSpendRate(current.Balance, alertSpend, alertDuration, rate, alertLevel);
    }

    public static CreditSpendRate? AnalyzeLatestValid(IReadOnlyList<CreditBalanceSample> samples)
    {
        var history = samples
            .Where(sample => sample.Balance >= 0m && sample.Timestamp != default)
            .OrderBy(sample => sample.Timestamp)
            .GroupBy(sample => sample.Timestamp)
            .Select(group => group.Last())
            .ToList();

        // Credit balance is repeated on many token events. Reduce long plateaus to
        // their boundaries before searching, keeping recovery fast even across
        // eight busy days of local logs.
        var changes = new List<CreditBalanceSample>();
        for (var index = 0; index < history.Count; index++)
        {
            var isBoundary = index == 0 ||
                             index == history.Count - 1 ||
                             history[index].Balance != history[index - 1].Balance ||
                             history[index].Balance != history[index + 1].Balance;
            if (isBoundary && (changes.Count == 0 || changes[^1].Timestamp != history[index].Timestamp))
                changes.Add(history[index]);
        }
        history = changes;

        // Walk backwards so a temporarily flat current balance can reuse the newest
        // genuinely measured pace. Never bridge a recharge/top-up boundary.
        for (var endIndex = history.Count - 1; endIndex > 0; endIndex--)
        {
            var end = history[endIndex];
            var runStart = 0;
            for (var index = 1; index <= endIndex; index++)
            {
                if (history[index].Balance - history[index - 1].Balance >= BalanceTopUpTolerance)
                    runStart = index;
            }

            var earliest = end.Timestamp - LongWindow;
            for (var baselineIndex = runStart; baselineIndex < endIndex; baselineIndex++)
            {
                var baseline = history[baselineIndex];
                if (baseline.Timestamp < earliest || baseline.Balance <= end.Balance) continue;

                var duration = end.Timestamp - baseline.Timestamp;
                if (duration < MinimumObservation) continue;
                var spent = baseline.Balance - end.Balance;
                var creditsPerHour = spent / Math.Max(0.01m, (decimal)duration.TotalHours);
                if (creditsPerHour < 0m || creditsPerHour > MaxCredibleCreditsPerHour) continue;

                return new CreditSpendRate(
                    end.Balance,
                    spent,
                    duration,
                    creditsPerHour,
                    CreditSpendAlertLevel.None);
            }
        }

        return null;
    }
}
