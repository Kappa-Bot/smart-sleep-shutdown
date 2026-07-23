param(
    [string] $Configuration = "Release",
    [string] $InstallRoot = "$env:LOCALAPPDATA\Hushward\App"
)

$ErrorActionPreference = "Stop"

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$projectPath = Join-Path $projectRoot "src\Hushward.App\Hushward.App.csproj"
$exePath = Join-Path $InstallRoot "Hushward.App.exe"

$legacyExePath = Join-Path $env:LOCALAPPDATA "Hushward\Hushward.App.exe"
$runningProcesses = Get-Process -Name "Hushward.App" -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -in @($exePath, $legacyExePath) }

if ($runningProcesses) {
    $signalExecutable = $runningProcesses[0].Path
    $exitSignalProcess = Start-Process -FilePath $signalExecutable -ArgumentList "--exit" -PassThru
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
    -Name "Hushward" `
    -Value "`"$exePath`" --startup" `
    -PropertyType String `
    -Force | Out-Null

Write-Host "Installed Hushward to $InstallRoot"
Write-Host "Startup registration: $exePath --startup"
Write-Host "Wake scheduled task: managed by Hushward when a routine enables wake."
