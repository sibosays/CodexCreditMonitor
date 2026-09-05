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
        var rate = longSpend / Math.Max(0.01m, (decimal)longDuration.TotalHours);

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
}
