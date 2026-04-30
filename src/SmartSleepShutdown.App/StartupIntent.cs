namespace SmartSleepShutdown.App;

public static class StartupIntent
{
    public static bool IsExitRequest(IEnumerable<string> args)
    {
        return HasArgument(args, "--exit");
    }

    public static bool IsBackgroundLaunch(IEnumerable<string> args)
    {
        return HasArgument(args, "--startup") || IsScheduledCheck(args);
    }

    public static bool IsScheduledCheck(IEnumerable<string> args)
    {
        return HasArgument(args, "--scheduled-check");
    }

    public static bool ShouldActivateExistingPrimary(IEnumerable<string> args)
    {
        return !IsExitRequest(args) && !IsBackgroundLaunch(args);
    }

    public static bool ShouldSignalScheduledCheck(IEnumerable<string> args)
    {
        return IsScheduledCheck(args);
    }

    public static bool ShouldShowMainWindow(IEnumerable<string> args)
    {
        return !IsBackgroundLaunch(args);
    }

    private static bool HasArgument(IEnumerable<string> args, string expected)
    {
        return args.Any(arg => string.Equals(arg, expected, StringComparison.OrdinalIgnoreCase));
    }
}
