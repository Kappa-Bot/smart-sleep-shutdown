namespace SmartSleepShutdown.Core.Tests;

internal static class TestAssertions
{
    public static void ShouldBe<T>(this T actual, T expected)
    {
        Assert.Equal(expected, actual);
    }

    public static void ShouldBeTrue(this bool actual)
    {
        Assert.True(actual);
    }

    public static void ShouldBeFalse(this bool actual)
    {
        Assert.False(actual);
    }
}
