[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $VersionA,

    [Parameter(Mandatory)]
    [string] $VersionB,

    [switch] $DisposableEnvironment
)

$ErrorActionPreference = "Stop"
if (-not $DisposableEnvironment -or $env:HUSHWARD_DISPOSABLE_TEST -ne "1") {
    throw "Refusing to modify the current profile. Run only in a disposable Windows user or VM with HUSHWARD_DISPOSABLE_TEST=1."
}

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$releaseDir = Join-Path $root "artifacts\releases"
$dataDir = Join-Path $env:LOCALAPPDATA "Hushward"

foreach ($version in @($VersionA, $VersionB)) {
    & (Join-Path $PSScriptRoot "Package-Hushward.ps1") -Version $version -PreserveExisting
}

$setupA = Get-ChildItem -LiteralPath $releaseDir -Filter "*$VersionA*Setup*.exe" -File | Select-Object -First 1
$setupB = Get-ChildItem -LiteralPath $releaseDir -Filter "*$VersionB*Setup*.exe" -File | Select-Object -First 1
if (-not $setupA -or -not $setupB) {
    throw "Lifecycle installers were not generated."
}

& $setupA.FullName --silent
if ($LASTEXITCODE -ne 0) { throw "Version A install failed." }

New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
'{"schemaVersion":1,"routines":[]}' | Set-Content -LiteralPath (Join-Path $dataDir "config.json") -Encoding UTF8
'{"kind":"lifecycle-test"}' | Set-Content -LiteralPath (Join-Path $dataDir "history.jsonl") -Encoding UTF8
$before = Get-FileHash -LiteralPath (Join-Path $dataDir "config.json") -Algorithm SHA256

& $setupB.FullName --silent
if ($LASTEXITCODE -ne 0) { throw "Version B update failed." }
$after = Get-FileHash -LiteralPath (Join-Path $dataDir "config.json") -Algorithm SHA256
if ($before.Hash -ne $after.Hash) { throw "Configuration changed during update." }

$runValue = (Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name Hushward -ErrorAction Stop).Hushward
if ($runValue -notmatch "Hushward.*--startup") { throw "Startup registration is unhealthy." }

$task = Get-ScheduledTask -TaskName "Hushward-NightWake" -ErrorAction SilentlyContinue
if ($task -and -not $task.Settings.WakeToRun) { throw "Wake task exists but WakeToRun is disabled." }

Write-Host "Lifecycle update and persistence checks passed. Rollback/uninstall choices require interactive Velopack QA."
