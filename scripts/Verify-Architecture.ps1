[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$violations = [System.Collections.Generic.List[string]]::new()

function Find-Text {
    param([string] $Path, [string] $Pattern, [string] $Label)
    $files = Get-ChildItem -LiteralPath $Path -Recurse -File |
        Where-Object {
            $_.Extension -in @(".cs", ".csproj") -and
            $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
        }
    foreach ($match in ($files | Select-String -Pattern $Pattern)) {
        $relative = $match.Path.Substring($root.Length).TrimStart("\", "/")
        $violations.Add("$Label`: $relative`:$($match.LineNumber):$($match.Line.Trim())")
    }
}

Find-Text (Join-Path $root "src\Hushward.Core") `
    'System\.Windows|Microsoft\.Win32|System\.Diagnostics|System\.IO|Hushward\.Infrastructure|UseWPF|WindowsDesktop' `
    "Core boundary"
Find-Text (Join-Path $root "src\Hushward.Application") `
    'System\.Windows|Hushward\.Infrastructure|UseWPF|WindowsDesktop' `
    "Application boundary"
Find-Text (Join-Path $root "src\Hushward.App\ViewModels") `
    'new\s+(Win32|Windows|Json|Aggregate|Velopack|Registry|TaskScheduler)' `
    "ViewModel composition"

$appProject = Get-Content -LiteralPath (Join-Path $root "src\Hushward.App\Hushward.App.csproj") -Raw
if ($appProject -notmatch '<ProjectReference Include="\.\.\\Hushward\.Application\\') {
    $violations.Add("App must reference the Application boundary.")
}

$coreProject = Get-Content -LiteralPath (Join-Path $root "src\Hushward.Core\Hushward.Core.csproj") -Raw
if ($coreProject -match '<PackageReference|<ProjectReference') {
    $violations.Add("Core must not have package or project dependencies.")
}

if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Host "Architecture OK"
$global:LASTEXITCODE = 0
