param(
    [string] $Configuration = "Release",
    [string] $InstallRoot = "$env:LOCALAPPDATA\SmartSleepShutdown"
)

$ErrorActionPreference = "Stop"

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $projectRoot "src\SmartSleepShutdown.App\SmartSleepShutdown.App.csproj"
$exePath = Join-Path $InstallRoot "SmartSleepShutdown.exe"
$wakeTaskName = "SmartSleepShutdown-NightWake"

$runningProcesses = Get-Process -Name "SmartSleepShutdown" -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -eq $exePath }

if ($runningProcesses) {
    $exitSignalProcess = Start-Process -FilePath $exePath -ArgumentList "--exit" -PassThru
    try {
        Wait-Process -Id $exitSignalProcess.Id -Timeout 5 -ErrorAction Stop
    }
    catch {
        Stop-Process -Id $exitSignalProcess.Id -Force -ErrorAction SilentlyContinue
    }

    foreach ($process in $runningProcesses) {
        try {
            Wait-Process -Id $process.Id -Timeout 10 -ErrorAction Stop
        }
        catch {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            Wait-Process -Id $process.Id -Timeout 5 -ErrorAction SilentlyContinue
        }
    }
}

dotnet publish $projectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o $InstallRoot

if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Installed executable not found: $exePath"
}

$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
New-Item -Path $runKey -Force | Out-Null
New-ItemProperty `
    -Path $runKey `
    -Name "SmartSleepShutdown" `
    -Value "`"$exePath`" --startup" `
    -PropertyType String `
    -Force | Out-Null

$taskAction = New-ScheduledTaskAction `
    -Execute $exePath `
    -Argument "--startup"
$taskTrigger = New-ScheduledTaskTrigger `
    -Daily `
    -At "00:30"
$taskSettings = New-ScheduledTaskSettingsSet `
    -WakeToRun `
    -StartWhenAvailable `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -MultipleInstances IgnoreNew `
    -ExecutionTimeLimit (New-TimeSpan -Hours 6)
$taskPrincipal = New-ScheduledTaskPrincipal `
    -UserId ([System.Security.Principal.WindowsIdentity]::GetCurrent().Name) `
    -LogonType Interactive `
    -RunLevel Limited
$wakeTask = New-ScheduledTask `
    -Action $taskAction `
    -Trigger $taskTrigger `
    -Settings $taskSettings `
    -Principal $taskPrincipal `
    -Description "Wake and start Smart Sleep Shutdown before the nightly shutdown window."

Register-ScheduledTask `
    -TaskName $wakeTaskName `
    -InputObject $wakeTask `
    -Force | Out-Null

try {
    powercfg /SETACVALUEINDEX SCHEME_CURRENT SUB_SLEEP RTCWAKE 1 | Out-Null
    powercfg /SETDCVALUEINDEX SCHEME_CURRENT SUB_SLEEP RTCWAKE 1 | Out-Null
    powercfg /S SCHEME_CURRENT | Out-Null
    Write-Host "Wake timers enabled for the current power plan."
}
catch {
    Write-Warning "Could not enable wake timers automatically. Enable 'Allow wake timers' in Windows Power Options."
}

Write-Host "Installed Smart Sleep Shutdown to $InstallRoot"
Write-Host "Startup registration: $exePath --startup"
Write-Host "Wake scheduled task: $wakeTaskName at 00:30"
