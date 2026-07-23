namespace Hushward.Application.History;

public readonly record struct HistoryRetention(int? Days)
{
    public static HistoryRetention Off { get; } = new(null);

    public static HistoryRetention SevenDays { get; } = new(7);

    public static HistoryRetention FourteenDays { get; } = new(14);

    public static HistoryRetention ThirtyDays { get; } = new(30);

    public static HistoryRetention Default => FourteenDays;

    public bool IsEnabled => Days is not null;

    public TimeSpan? Period => Days is null ? null : TimeSpan.FromDays(Days.Value);

    public static bool TryFromDays(int? days, out HistoryRetention retention)
    {
        retention = days switch
        {
            null => Off,
            7 => SevenDays,
            14 => FourteenDays,
            30 => ThirtyDays,
            _ => default
        };

        return days is null or 7 or 14 or 30;
    }
}
