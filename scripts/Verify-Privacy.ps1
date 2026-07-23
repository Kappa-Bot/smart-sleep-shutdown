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
    $files = Get-ChildItem -LiteralPath $path -Recurse -File -Filter "*.cs" |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    foreach ($match in ($files | Select-String -Pattern $forbiddenPropertyPattern)) {
        $file = $match.Path.Substring($root.Length).TrimStart("\", "/")
        $violations.Add("$relativePath`: $file`:$($match.LineNumber):$($match.Line.Trim())")
    }
}

$updateService = [System.IO.Path]::GetFullPath(
    (Join-Path $root "src\Hushward.Infrastructure\Updates\VelopackUpdateService.cs"))
$sourceFiles = Get-ChildItem -LiteralPath (Join-Path $root "src") -Recurse -File -Filter "*.cs" |
    Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and
        [System.IO.Path]::GetFullPath($_.FullName) -ne $updateService
    }
foreach ($match in ($sourceFiles | Select-String -Pattern 'HttpClient|SocketsHttpHandler|WebRequest')) {
    $file = $match.Path.Substring($root.Length).TrimStart("\", "/")
    $violations.Add("Unexpected network surface: $file`:$($match.LineNumber):$($match.Line.Trim())")
}

if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Privacy OK"
$global:LASTEXITCODE = 0
