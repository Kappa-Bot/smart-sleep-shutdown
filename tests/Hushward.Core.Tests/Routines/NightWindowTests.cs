using Hushward.Core.Routines;

namespace Hushward.Core.Tests.Routines;

public sealed class NightWindowTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    [Theory]
    [InlineData("2026-07-23T23:30:00+00:00", true)]
    [InlineData("2026-07-24T02:00:00+00:00", true)]
    [InlineData("2026-07-24T07:00:00+00:00", false)]
    public void Midnight_crossing_window_is_deterministic(string instant, bool expected)
    {
        var window = new NightWindow(new TimeOnly(23, 0), new TimeOnly(6, 0));

        window.Contains(DateTimeOffset.Parse(instant), Utc).ShouldBe(expected);
    }

    [Theory]
    [InlineData("2026-07-23T01:00:00+00:00", true)]
    [InlineData("2026-07-23T04:00:00+00:00", true)]
    [InlineData("2026-07-23T06:00:00+00:00", true)]
    [InlineData("2026-07-23T06:00:01+00:00", false)]
    public void Normal_window_includes_exact_boundaries(string instant, bool expected)
    {
        var window = new NightWindow(new TimeOnly(1, 0), new TimeOnly(6, 0));

        window.Contains(DateTimeOffset.Parse(instant), Utc).ShouldBe(expected);
    }

    [Fact]
    public void Uses_supplied_time_zone_without_reading_machine_zone()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        var window = new NightWindow(new TimeOnly(1, 0), new TimeOnly(6, 0));
        var utcInstantThatIsInsideSpanishSummerWindow = DateTimeOffset.Parse("2026-07-23T23:30:00+00:00");

        window.Contains(utcInstantThatIsInsideSpanishSummerWindow, timeZone).ShouldBeTrue();
    }

    [Theory]
    [InlineData("2026-03-29T00:30:00+00:00")]
    [InlineData("2026-03-29T01:30:00+00:00")]
    [InlineData("2026-10-25T00:30:00+00:00")]
    [InlineData("2026-10-25T01:30:00+00:00")]
    public void Supplied_time_zone_handles_dst_gap_and_fold(string instant)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        var window = new NightWindow(new TimeOnly(1, 0), new TimeOnly(6, 0));

        window.Contains(DateTimeOffset.Parse(instant), timeZone).ShouldBeTrue();
    }
}
