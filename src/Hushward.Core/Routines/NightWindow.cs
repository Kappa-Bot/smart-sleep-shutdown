namespace Hushward.Core.Routines;

public readonly record struct NightWindow(TimeOnly Earliest, TimeOnly Latest)
{
    public bool Contains(DateTimeOffset now, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(now, timeZone).TimeOfDay;
        var earliest = Earliest.ToTimeSpan();
        var latest = Latest.ToTimeSpan();

        return earliest <= latest
            ? local >= earliest && local <= latest
            : local >= earliest || local <= latest;
    }
}
