[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$scanPaths = @(
    "src\Hushward.Application\Configuration",
    "src\Hushward.Application\History",
    "src\Hushward.Application\Diagnostics",
    "src\Hushward.Application\Abstractions",
    "src\Hushward.Infrastructure\History",
    "src\Hushward.Infrastructure\Persistence",
    "src\Hushward.Infrastructure\Detectors"
)
$forbiddenPropertyPattern = '(?i)\b(WindowTitle|Url|BrowserTab|DocumentName|FileName|Clipboard|Screenshot|AudioContent|VideoContent|Keystroke|CommandLine|Token|Secret)\b'
$violations = [System.Collections.Generic.List[string]]::new()

foreach ($relativePath in $scanPaths) {
    $path = Join-Path $root $relativePath
    if (-not (Test-Path -LiteralPath $path)) { continue }
    $matches = & rg -n --glob "*.cs" $forbiddenPropertyPattern $path 2>$null
    if ($LASTEXITCODE -eq 0) {
        foreach ($match in $matches) { $violations.Add("$relativePath`: $match") }
    }
    elseif ($LASTEXITCODE -ne 1) {
        throw "Privacy scan failed for $relativePath."
    }
}

$networkMatches = & rg -n --glob "*.cs" 'HttpClient|SocketsHttpHandler|WebRequest' (Join-Path $root "src") `
    --glob "!Hushward.Infrastructure/Updates/VelopackUpdateService.cs" 2>$null
if ($LASTEXITCODE -eq 0) {
    foreach ($match in $networkMatches) { $violations.Add("Unexpected network surface: $match") }
}
elseif ($LASTEXITCODE -ne 1) {
    throw "Network boundary scan failed."
}

if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Privacy OK"
$global:LASTEXITCODE = 0
