using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using SmartSleepShutdown.App.Settings;
using SmartSleepShutdown.Infrastructure.System;

namespace SmartSleepShutdown.App.Diagnostics;

public static class ConsoleDiagnostics
{
    private const uint AttachParentProcess = 0xFFFFFFFF;
    private const string WakeTaskName = "SmartSleepShutdown-NightWake";

    public static void PrintScheduleReport()
    {
        EnsureConsole();
        Console.Write(BuildScheduleReport());
    }

    public static void DumpLocalDiagnostics()
    {
        EnsureConsole();
        var path = LocalDiagnosticsSink.DefaultPath;
        if (!File.Exists(path))
        {
            Console.WriteLine($"No diagnostics file: {path}");
            return;
        }

        Console.Write(File.ReadAllText(path));
    }

    public static string BuildScheduleReport()
    {
        var builder = new StringBuilder();
        var appPath = Environment.ProcessPath ?? "unknown";
        var settingsPath = JsonUserSettingsStore.DefaultPath;
        var diagnosticsPath = LocalDiagnosticsSink.DefaultPath;
        var runValue = ReadRunKey();

        builder.AppendLine("Smart Sleep Shutdown schedule diagnostics");
        builder.AppendLine($"App path: {appPath}");
        builder.AppendLine($"Settings path: {settingsPath}");
        builder.AppendLine($"Diagnostics path: {diagnosticsPath}");
        builder.AppendLine($"Run key configured: {(!string.IsNullOrWhiteSpace(runValue)).ToString().ToLowerInvariant()}");
        builder.AppendLine($"Run key value: {runValue ?? "(missing)"}");
        builder.AppendLine();
        builder.AppendLine("Task Scheduler:");
        builder.AppendLine(RunCommand("schtasks.exe", $"/Query /TN \"{WakeTaskName}\" /FO LIST /V"));
        builder.AppendLine();
        builder.AppendLine("Wake timers:");
        builder.AppendLine(RunCommand("powercfg.exe", "/Q SCHEME_CURRENT SUB_SLEEP RTCWAKE"));
        builder.AppendLine();
        builder.AppendLine("Active wake timers:");
        builder.AppendLine(RunCommand("powercfg.exe", "/waketimers"));
        builder.AppendLine();
        builder.AppendLine("Last wake:");
        builder.AppendLine(RunCommand("powercfg.exe", "/lastwake"));
        return builder.ToString();
    }

    private static string? ReadRunKey()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegistration.RunKeyPath);
        return key?.GetValue(StartupRegistration.RunValueName) as string;
    }

    private static string RunCommand(string fileName, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return $"{fileName} could not start.";
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(5000);

            var text = string.IsNullOrWhiteSpace(output) ? error : output;
            return string.IsNullOrWhiteSpace(text) ? "(no output)" : text.TrimEnd();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return $"{fileName} failed: {ex.Message}";
        }
    }

    private static void EnsureConsole()
    {
        AttachConsole(AttachParentProcess);
        var output = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        Console.SetOut(output);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint dwProcessId);
}
