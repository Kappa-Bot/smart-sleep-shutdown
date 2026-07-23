# Hushward Implementation Plan — Self-Review Fixes

**Status:** Canonical addendum to `2026-07-23-hushward-product-transformation.md`  
**Rule:** Where this file conflicts with the implementation plan, this file wins.

## 1. Task 1 baseline output contains no committed placeholders

Do not copy angle-bracket template values into `docs/quality/legacy-baseline.md`. Generate the file directly from command output with this PowerShell sequence:

```powershell
$sourceCommit = (git rev-parse HEAD).Trim()
$sdk = (dotnet --version).Trim()
$testOutput = dotnet test .\SmartSleepShutdown.sln -c Release --logger "console;verbosity=minimal" 2>&1
if ($LASTEXITCODE -ne 0) { $testOutput | Write-Host; exit $LASTEXITCODE }
$summary = ($testOutput | Select-String -Pattern "Passed!|Failed!|Total tests" | ForEach-Object Line) -join "`n"
$warningOutput = dotnet build .\SmartSleepShutdown.sln -c Release --no-restore 2>&1
if ($LASTEXITCODE -ne 0) { $warningOutput | Write-Host; exit $LASTEXITCODE }
$warningLines = @($warningOutput | Select-String -Pattern "warning [A-Z]+[0-9]+")
$warningText = if ($warningLines.Count -eq 0) { "none" } else { ($warningLines | ForEach-Object Line | Sort-Object -Unique) -join "; " }
@"
# Legacy baseline

- Source commit: $sourceCommit
- SDK: $sdk
- Release build: PASS
- Test result: $summary
- Existing warnings: $warningText
- Safety behaviors covered: warning required, input cancellation, final recheck, no forced shutdown, detector failure blocks, idle re-arm.
"@ | Set-Content .\docs\quality\legacy-baseline.md -Encoding utf8
```

The generated file must contain concrete values before commit.

## 2. Task 2 invalid-alternative test correction

Replace the inconsistent test body with this exact test:

```csharp
[Fact]
public void Rejects_alternative_equal_to_primary_action()
{
    var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
    {
        PrimaryAction = NightAction.ShutDown,
        AuthorizedAlternatives =
        [
            new AuthorizedAlternative(
                NightAction.ShutDown,
                NightAction.ShutDown,
                "battery-low")
        ]
    };

    Assert.Contains(
        routine.Validate(),
        error => error.Code == RoutineValidationCode.InvalidAlternative);
}
```

An `AuthorizedAlternative` record is itself explicit authorization. The engine must reject an alternative only when the record is invalid, its condition is not satisfied, it is unsupported, or it was not included in the effective plan. It must never infer an alternative absent from this collection.

## 3. Branding assets are source-controlled and production-ready

Task 13 must create all of these files, not only an `.ico`:

```text
src/Hushward.App/Assets/Brand/HushwardMark.svg
src/Hushward.App/Assets/Brand/HushwardWordmark.svg
src/Hushward.App/Assets/Brand/HushwardAppIcon-16.png
src/Hushward.App/Assets/Brand/HushwardAppIcon-20.png
src/Hushward.App/Assets/Brand/HushwardAppIcon-24.png
src/Hushward.App/Assets/Brand/HushwardAppIcon-32.png
src/Hushward.App/Assets/Brand/HushwardAppIcon-48.png
src/Hushward.App/Assets/Brand/HushwardAppIcon-64.png
src/Hushward.App/Assets/Brand/HushwardAppIcon-128.png
src/Hushward.App/Assets/Brand/HushwardAppIcon-256.png
src/Hushward.App/Assets/Hushward.ico
scripts/Build-BrandAssets.ps1
```

The source mark must express a protected night threshold with a small warm authorized-action beacon. It must not use a generic moon, stars, power-button glyph, robot, antivirus-style shield, or copied third-party mark. `Build-BrandAssets.ps1` must deterministically regenerate raster sizes and the ICO from the SVG source using a documented toolchain. Review the 16 px tray rendering at 100%, 150%, and 200% scaling before approval.

## 4. Optional startup registration is application-owned and reversible

Add these files to Tasks 10/16:

```text
src/Hushward.Application/Abstractions/IStartupRegistration.cs
src/Hushward.Infrastructure/Startup/WindowsStartupRegistration.cs
src/Hushward.Infrastructure/Startup/StartupRegistrationHealthReader.cs
tests/Hushward.Infrastructure.Tests/Startup/WindowsStartupRegistrationTests.cs
```

Contract:

```csharp
public interface IStartupRegistration
{
    Task<OperationResult<StartupRegistrationState>> ReadAsync(CancellationToken cancellationToken);
    Task<OperationResult<StartupRegistrationState>> SetEnabledAsync(
        bool enabled,
        CancellationToken cancellationToken);
}
```

Use a per-user reversible registration. The exact mechanism may follow the stable Velopack executable stub or the current-user Startup folder, but it must use the installed stable launcher path, expose health, require no elevation, and be removed during uninstall. Migration must replace the legacy registration only after the Hushward registration reads back healthy.

## 5. Battery alternatives remain explicit

Add this policy test to Task 4:

```csharp
[Fact]
public void Low_battery_uses_hibernate_only_when_explicitly_authorized()
{
    var withoutAlternative = NightEvaluationInputBuilder.Eligible(NightAction.ShutDown)
        .WithBattery(percent: 12, charging: false)
        .Build();
    Assert.Equal(
        NightAction.ShutDown,
        NightPolicyEngine.Evaluate(withoutAlternative).AuthorizedAction);

    var withAlternative = NightEvaluationInputBuilder.Eligible(NightAction.ShutDown)
        .WithBattery(percent: 12, charging: false)
        .WithAuthorizedAlternative(
            NightAction.Hibernate,
            conditionCode: "battery-below-20")
        .Build();
    Assert.Equal(
        NightAction.Hibernate,
        NightPolicyEngine.Evaluate(withAlternative).AuthorizedAction);
}
```

Low battery alone never invents or substitutes an action.

## 6. Product update channel and network boundary

The first release uses GitHub Releases only as an optional update catalog. Network access occurs only for a manual check or after explicit opt-in to update notifications. Requests must contain no installation identifier, configuration, history, detector data, device name, user name, or custom analytics headers. Update failures are degraded, never blocking nightly safety unless installation has already entered a staged mutation state.

## 7. Plan self-review result

- Spec coverage: complete after the additions above.
- Placeholder scan: implementation outputs must be generated from commands; no unresolved value remains in committed product artifacts.
- Type consistency: `AuthorizedAlternative`, `NightAction`, `NightRoutine`, `NightPolicyEngine`, `NightRuntimeSnapshot`, `OperationResult<T>`, coordinator, and port names are consistent across tasks.
- Scope: remains one integrated product transformation executed through five gated phases and eighteen independently reviewable tasks.
- YAGNI boundary: no account, cloud state, telemetry, arbitrary automation, service, scripts, plugins, AI policy, or monetization has been added.