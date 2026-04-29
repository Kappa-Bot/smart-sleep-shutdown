namespace SmartSleepShutdown.App;

public static class StartupIntent
{
    public static bool IsExitRequest(IEnumerable<string> args)
    {
        return HasArgument(args, "--exit");
    }

    public static bool IsBackgroundLaunch(IEnumerable<string> args)
    {
        return HasArgument(args, "--startup");
    }

    public static bool ShouldActivateExistingPrimary(IEnumerable<string> args)
    {
        return !IsExitRequest(args) && !IsBackgroundLaunch(args);
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
