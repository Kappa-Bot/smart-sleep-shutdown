[CmdletBinding()]
param(
    [ValidateSet("win-x64", "win-arm64")]
    [string] $Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$project = Join-Path $root "src\Hushward.App\Hushward.App.csproj"
$publishRoot = Join-Path $root "artifacts\publish"
$output = Join-Path $publishRoot $Runtime

if (Test-Path -LiteralPath $output) {
    $resolved = [System.IO.Path]::GetFullPath($output)
    if (-not $resolved.StartsWith([System.IO.Path]::GetFullPath($publishRoot), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe publish output path: $resolved"
    }

    Remove-Item -LiteralPath $resolved -Recurse -Force
}

dotnet publish $project `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $output

if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $output "Hushward.App.exe"))) {
    throw "Hushward publish failed."
}

Write-Host "Published: $output"
