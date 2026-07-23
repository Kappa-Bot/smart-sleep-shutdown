[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
Push-Location $root
try {
    $stages = @(
        @{ Name = "Format"; Command = { dotnet format .\Hushward.sln --verify-no-changes } },
        @{ Name = "Build"; Command = { dotnet build .\Hushward.sln -c Release } },
        @{ Name = "Tests"; Command = { dotnet test .\Hushward.sln -c Release --no-build --collect:"XPlat Code Coverage" --results-directory .\TestResults } },
        @{ Name = "Architecture"; Command = { .\scripts\Verify-Architecture.ps1 } },
        @{ Name = "Privacy"; Command = { .\scripts\Verify-Privacy.ps1 } },
        @{ Name = "Publish"; Command = { .\scripts\Publish-Hushward.ps1 } }
    )

    foreach ($stage in $stages) {
        Write-Host "$($stage.Name)..."
        & $stage.Command
        if ($LASTEXITCODE -ne 0) {
            throw "$($stage.Name) failed with exit code $LASTEXITCODE."
        }
        Write-Host "$($stage.Name) OK"
    }
}
finally {
    Pop-Location
}
