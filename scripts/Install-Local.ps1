param(
    [string] $Configuration = "Release",
    [string] $InstallRoot = "$env:LOCALAPPDATA\SmartSleepShutdown"
)

$ErrorActionPreference = "Stop"

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $projectRoot "src\SmartSleepShutdown.App\SmartSleepShutdown.App.csproj"
$exePath = Join-Path $InstallRoot "SmartSleepShutdown.exe"

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

Write-Host "Installed Smart Sleep Shutdown to $InstallRoot"
Write-Host "Startup registration: $exePath --startup"
