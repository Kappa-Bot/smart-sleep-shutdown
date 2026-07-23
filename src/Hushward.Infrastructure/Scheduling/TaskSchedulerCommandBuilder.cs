using System.Globalization;
using Hushward.Application.Scheduling;

namespace Hushward.Infrastructure.Scheduling;

public static class TaskSchedulerCommandBuilder
{
    public static ScheduledTaskCommand BuildRegister(
        IReadOnlyList<DesiredWakeSchedule> schedules,
        string executablePath)
    {
        var triggerScripts = schedules
            .Select((schedule, index) => BuildTriggerScript(schedule, index))
            .ToArray();

        var script = string.Join("; ", triggerScripts)
            + "; $action = New-ScheduledTaskAction -Execute "
            + Quote(executablePath)
            + " -Argument "
            + Quote(DesiredWakeSchedule.ScheduledCheckArgument)
            + "; $settings = New-ScheduledTaskSettingsSet -WakeToRun -AllowStartIfOnBatteries -StartWhenAvailable"
            + "; Register-ScheduledTask -TaskName "
            + Quote(DesiredWakeSchedule.ProductTaskName)
            + " -Action $action -Trigger @("
            + string.Join(",", Enumerable.Range(0, schedules.Count).Select(index => "$trigger" + index.ToString(CultureInfo.InvariantCulture)))
            + ") -Settings $settings -Force | Out-Null";

        return PowerShell(script);
    }

    public static ScheduledTaskCommand BuildDelete(string taskName) =>
        PowerShell("Unregister-ScheduledTask -TaskName " + Quote(taskName) + " -Confirm:$false -ErrorAction SilentlyContinue");

    public static ScheduledTaskCommand BuildHealthCheck(
        IReadOnlyList<DesiredWakeSchedule> schedules,
        string executablePath)
    {
        var expectedTriggers = string.Join(
            ",",
            schedules.Select(schedule =>
                "@{ Time = "
                + Quote(schedule.LocalStartTime.ToString("HH:mm", CultureInfo.InvariantCulture))
                + "; Days = "
                + Quote(string.Join(",", schedule.Days.Select(day => day.ToString())))
                + " }"));
        var script = "$task = Get-ScheduledTask -TaskName "
            + Quote(DesiredWakeSchedule.ProductTaskName)
            + " -ErrorAction Stop"
            + "; if ($task.Actions[0].Execute -ne "
            + Quote(executablePath)
            + ") { exit 11 }"
            + "; if ($task.Actions[0].Arguments -ne "
            + Quote(DesiredWakeSchedule.ScheduledCheckArgument)
            + ") { exit 12 }"
            + "; if (-not $task.Settings.WakeToRun) { exit 13 }"
            + "; $triggers = @($task.Triggers)"
            + "; if ($triggers.Count -ne "
            + schedules.Count.ToString(CultureInfo.InvariantCulture)
            + ") { exit 17 }"
            + "; $expected = @("
            + expectedTriggers
            + ")"
            + "; foreach ($item in $expected) { if (-not ($triggers | Where-Object { $_.StartBoundary -like ('*T' + $item.Time + ':*') -and (($_.DaysOfWeek -join ',') -eq $item.Days) })) { exit 14 } }"
            + "; foreach ($trigger in $triggers) { if ($trigger.Repetition.Interval -ne 'PT5M') { exit 15 }; if ($trigger.Repetition.Duration -ne 'PT6H') { exit 16 } }";

        return PowerShell(script);
    }

    private static string BuildTriggerScript(DesiredWakeSchedule schedule, int index)
    {
        var at = schedule.LocalStartTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        var days = string.Join(",", schedule.Days.Select(day => day.ToString()));
        return "$trigger" + index.ToString(CultureInfo.InvariantCulture)
            + " = New-ScheduledTaskTrigger -Weekly -DaysOfWeek "
            + days
            + " -At "
            + Quote(at)
            + "; $trigger"
            + index.ToString(CultureInfo.InvariantCulture)
            + ".Repetition = New-ScheduledTaskRepetitionSettings -Interval (New-TimeSpan -Minutes 5) -Duration (New-TimeSpan -Hours 6)";
    }

    private static ScheduledTaskCommand PowerShell(string script) =>
        new(
            "powershell.exe",
            ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script]);

    private static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
}
