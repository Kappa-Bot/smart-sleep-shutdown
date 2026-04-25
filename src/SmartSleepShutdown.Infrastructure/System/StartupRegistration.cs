using Microsoft.Win32;

namespace SmartSleepShutdown.Infrastructure.System;

public static class StartupRegistration
{
    public const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public const string RunValueName = "SmartSleepShutdown";

    public static string BuildRunCommand(string executablePath)
    {
        return $"\"{executablePath}\" --startup";
    }

    public static void RegisterForCurrentUser(string executablePath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Could not open current-user startup registry key.");

        key.SetValue(RunValueName, BuildRunCommand(executablePath), RegistryValueKind.String);
    }
}
