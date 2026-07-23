[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$')]
    [string] $Version,

    [ValidateSet("win-x64")]
    [string] $Runtime = "win-x64",

    [switch] $PreserveExisting
)

$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$publishDir = Join-Path $root "artifacts\publish\$Runtime"
$releaseDir = Join-Path $root "artifacts\releases"
$mainExe = Join-Path $publishDir "Hushward.App.exe"
$icon = Join-Path $root "src\Hushward.App\Assets\Hushward.ico"
$toolDir = Join-Path $root "artifacts\tools"
$vpk = Join-Path $toolDir "vpk.exe"

if (-not (Test-Path -LiteralPath $mainExe)) {
    & (Join-Path $PSScriptRoot "Publish-Hushward.ps1") -Runtime $Runtime
}

if ((Test-Path -LiteralPath $releaseDir) -and -not $PreserveExisting) {
    $resolvedReleaseDir = [System.IO.Path]::GetFullPath($releaseDir)
    $artifactRoot = [System.IO.Path]::GetFullPath((Join-Path $root "artifacts"))
    if (-not $resolvedReleaseDir.StartsWith($artifactRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe release output path: $resolvedReleaseDir"
    }

    Remove-Item -LiteralPath $resolvedReleaseDir -Recurse -Force
}

New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
if (-not (Test-Path -LiteralPath $vpk)) {
    New-Item -ItemType Directory -Path $toolDir -Force | Out-Null
    dotnet tool install --tool-path $toolDir vpk --version 1.2.0
    if ($LASTEXITCODE -ne 0) {
        throw "Could not install pinned Velopack CLI 1.2.0."
    }
}

& $vpk pack `
    --packId KappaBot.Hushward `
    --packVersion $Version `
    --packDir $publishDir `
    --mainExe Hushward.App.exe `
    --packTitle Hushward `
    --icon $icon `
    --outputDir $releaseDir

if ($LASTEXITCODE -ne 0) {
    throw "Velopack packaging failed."
}

$files = Get-ChildItem -LiteralPath $releaseDir -File |
    Where-Object { $_.Name -ne "hushward-$Version.manifest.json" } |
    Sort-Object Name |
    ForEach-Object {
        [ordered]@{
            file = $_.Name
            size = $_.Length
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }

$manifest = [ordered]@{
    product = "Hushward"
    version = $Version
    runtime = $Runtime
    unsigned = $true
    files = @($files)
}
$manifestPath = Join-Path $releaseDir "hushward-$Version.manifest.json"
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
Write-Host "Packaged: $releaseDir"
