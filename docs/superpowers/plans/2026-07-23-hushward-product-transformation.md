# Hushward Product Transformation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform Smart Sleep Shutdown into Hushward, a production-grade, local-first Windows night guardian that executes only explicitly authorized actions after deterministic safety checks, visible warning, cancellation, and a fresh final re-evaluation.

**Architecture:** Evolve the current three-project solution into `Hushward.Core`, `Hushward.Application`, `Hushward.Infrastructure`, and `Hushward.App`. Preserve the existing deterministic safety behavior through characterization tests, introduce new policy models side-by-side, then migrate orchestration and UI to one immutable `NightRuntimeSnapshot` before removing obsolete code.

**Tech Stack:** .NET 10, C# with nullable reference types, WPF on `net10.0-windows`, xUnit 2.9.3, `System.Text.Json`, Windows APIs/Task Scheduler, Velopack 1.2.0 for per-user packaging and optional updates, PowerShell release/QA scripts.

## Global Constraints

- Read `docs/superpowers/specs/2026-07-23-hushward-product-transformation-design.md` before changing code.
- Work in an isolated worktree created from the latest target branch; never implement directly on `main`.
- Preserve every existing safety invariant before replacing any implementation.
- Never execute shutdown, hibernate, sleep, or lock from an automated test.
- Shutdown must never use `/f` or force-close applications.
- Every automatic state-changing action requires a visible cancellable warning and a fresh final authorization check.
- Fresh keyboard or mouse activity cancels the warning and requires a new idle period.
- Detector failure or stale required evidence blocks automatic state-changing actions.
- Alternatives are valid only when explicitly authorized by the user; there is no implicit fallback.
- Wake behavior is configured per routine and disabled by default.
- No accounts, cloud synchronization, telemetry, backend, remote control, inbound listener, arbitrary scripts, custom shell commands, permanent Windows service, generative AI, medical claims, or behavioral profiling.
- Persist settings, history, and diagnostics under `%LOCALAPPDATA%\Hushward`, never beside installed binaries.
- Spanish is the release-gating UI language; every user-facing string must be resource-backed.
- Use Segoe UI Variable; do not bundle fonts.
- Meet WCAG 2.2 AA contrast and support keyboard, screen reader, reduced motion, high DPI, and multiple monitors.
- Keep each source file focused; coordinators orchestrate, Core decides, Infrastructure performs side effects, App presents.
- Use TDD for domain and application behavior, typed results for failures, and frequent focused commits.
- Do not accept build warnings as undocumented debt.
- Use at most six independent specialist workstreams when parallelizing.

## Dependency Order

```text
Task 1 baseline/rename
  -> Tasks 2-5 Core policy
  -> Tasks 6-9 Application and local data
  -> Tasks 10-12 Windows integration
  -> Tasks 13-15 WPF experience
  -> Task 16 packaging/update
  -> Tasks 17-18 release hardening
```

Tasks inside the same phase may be parallelized only when their `Consumes` contracts are already committed. Each task ends with an independently reviewable commit.

---

### Task 1: Freeze Legacy Safety and Establish Hushward Solution Boundaries

**Files:**
- Rename: `SmartSleepShutdown.sln` -> `Hushward.sln`
- Rename directory: `src/SmartSleepShutdown.Core` -> `src/Hushward.Core`
- Rename directory: `src/SmartSleepShutdown.Infrastructure` -> `src/Hushward.Infrastructure`
- Rename directory: `src/SmartSleepShutdown.App` -> `src/Hushward.App`
- Rename directory: `tests/SmartSleepShutdown.Core.Tests` -> `tests/Hushward.Core.Tests`
- Rename directory: `tests/SmartSleepShutdown.Infrastructure.Tests` -> `tests/Hushward.Infrastructure.Tests`
- Rename directory: `tests/SmartSleepShutdown.App.Tests` -> `tests/Hushward.App.Tests`
- Create: `src/Hushward.Application/Hushward.Application.csproj`
- Create: `tests/Hushward.Application.Tests/Hushward.Application.Tests.csproj`
- Create: `docs/quality/legacy-baseline.md`
- Modify: all renamed `.csproj` files, namespaces, project references, assembly names, `AGENTS.md`, `README.md`
- Preserve for migration: legacy product folder/registry/task identifiers as constants, not active branding

**Interfaces:**
- Produces four production assemblies: `Hushward.Core`, `Hushward.Application`, `Hushward.Infrastructure`, `Hushward.App`.
- Produces four matching test assemblies.
- Produces `LegacyProductIdentifiers` for migration-only use.

- [ ] **Step 1: Record the untouched baseline**

Run from the original repository state:

```powershell
dotnet format .\SmartSleepShutdown.sln --verify-no-changes
dotnet build .\SmartSleepShutdown.sln -c Release
dotnet test .\SmartSleepShutdown.sln -c Release --logger "console;verbosity=normal"
```

Expected: all commands exit `0`. Copy the SDK version, total passed test count, and existing warnings into `docs/quality/legacy-baseline.md` using this exact structure:

```markdown
# Legacy baseline

- Source commit: `<git rev-parse HEAD output>`
- SDK: `<dotnet --version output>`
- Release build: PASS
- Tests: `<passed count>` passed, 0 failed
- Existing warnings: `<exact count and codes, or none>`
- Safety behaviors covered: warning required, input cancellation, final recheck, no forced shutdown, detector failure blocks, idle re-arm.
```

Replace angle-bracket values with command output; do not leave placeholders in the committed file.

- [ ] **Step 2: Rename with Git history preserved**

```powershell
git mv SmartSleepShutdown.sln Hushward.sln
git mv src/SmartSleepShutdown.Core src/Hushward.Core
git mv src/SmartSleepShutdown.Infrastructure src/Hushward.Infrastructure
git mv src/SmartSleepShutdown.App src/Hushward.App
git mv tests/SmartSleepShutdown.Core.Tests tests/Hushward.Core.Tests
git mv tests/SmartSleepShutdown.Infrastructure.Tests tests/Hushward.Infrastructure.Tests
git mv tests/SmartSleepShutdown.App.Tests tests/Hushward.App.Tests
```

Rename each project file to match its directory and replace project/namespace references with `Hushward.*`. Do not rename legacy disk/registry/task strings yet.

- [ ] **Step 3: Add the Application project**

Create `src/Hushward.Application/Hushward.Application.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Hushward.Core\Hushward.Core.csproj" />
  </ItemGroup>
</Project>
```

Create `tests/Hushward.Application.Tests/Hushward.Application.Tests.csproj` using the same test package versions already pinned by the repository and references to Core and Application.

- [ ] **Step 4: Set exact project dependencies**

```text
Hushward.Core                 -> no project references
Hushward.Application          -> Hushward.Core
Hushward.Infrastructure       -> Hushward.Core, Hushward.Application
Hushward.App                  -> Hushward.Core, Hushward.Application, Hushward.Infrastructure
Hushward.Core.Tests           -> Hushward.Core
Hushward.Application.Tests    -> Hushward.Core, Hushward.Application
Hushward.Infrastructure.Tests -> Hushward.Core, Hushward.Application, Hushward.Infrastructure
Hushward.App.Tests            -> all production projects
```

Update `Hushward.sln` with `dotnet sln` rather than editing GUID blocks manually.

- [ ] **Step 5: Add migration-only identifiers**

Create `src/Hushward.Infrastructure/Migration/LegacyProductIdentifiers.cs`:

```csharp
namespace Hushward.Infrastructure.Migration;

public static class LegacyProductIdentifiers
{
    public const string ProductName = "Smart Sleep Shutdown";
    public const string LocalDataDirectoryName = "SmartSleepShutdown";
    public const string RunValueName = "SmartSleepShutdown";
    public const string WakeTaskName = "SmartSleepShutdown-NightWake";
}
```

No production UI may display these constants except the migration/recovery explanation.

- [ ] **Step 6: Verify the renamed baseline**

```powershell
dotnet format .\Hushward.sln --verify-no-changes
dotnet build .\Hushward.sln -c Release
dotnet test .\Hushward.sln -c Release
```

Expected: same passed-test count as the baseline, zero failures, no new warnings.

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "refactor: establish Hushward solution boundaries"
```

---

### Task 2: Add Night Action, Routine, Window, and Explicit Authorization Primitives

**Files:**
- Create: `src/Hushward.Core/Actions/NightAction.cs`
- Create: `src/Hushward.Core/Actions/AuthorizedAlternative.cs`
- Create: `src/Hushward.Core/Routines/NightWindow.cs`
- Create: `src/Hushward.Core/Routines/NightRoutine.cs`
- Create: `src/Hushward.Core/Routines/WakePolicy.cs`
- Create: `src/Hushward.Core/Routines/LatestDecisionPolicy.cs`
- Create: `src/Hushward.Core/Routines/TonightOverride.cs`
- Create: `src/Hushward.Core/Routines/RoutineValidation.cs`
- Test: `tests/Hushward.Core.Tests/Routines/NightRoutineTests.cs`
- Test: `tests/Hushward.Core.Tests/Routines/NightWindowTests.cs`

**Interfaces:**
- Produces immutable domain primitives consumed by Tasks 3-8.
- `NightWindow.Contains(DateTimeOffset now, TimeZoneInfo timeZone)` handles midnight crossing.
- `NightRoutine.Validate()` returns typed validation errors, never throws for user input.

- [ ] **Step 1: Write failing action and routine tests**

```csharp
using Hushward.Core.Actions;
using Hushward.Core.Routines;

namespace Hushward.Core.Tests.Routines;

public sealed class NightRoutineTests
{
    [Fact]
    public void Defaults_are_safe_and_disabled()
    {
        var routine = NightRoutine.CreateDefault(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.False(routine.Enabled);
        Assert.Equal(NightAction.Hibernate, routine.PrimaryAction);
        Assert.Equal(WakePolicy.NeverWake, routine.WakePolicy);
        Assert.Empty(routine.AuthorizedAlternatives);
    }

    [Fact]
    public void Rejects_implicit_alternative()
    {
        var routine = NightRoutine.CreateDefault(Guid.NewGuid()) with
        {
            PrimaryAction = NightAction.ShutDown,
            AuthorizedAlternatives = [new AuthorizedAlternative(NightAction.ShutDown, NightAction.Sleep, "battery-low")]
        };

        Assert.Contains(routine.Validate(), error => error.Code == RoutineValidationCode.InvalidAlternative);
    }
}
```

```csharp
namespace Hushward.Core.Tests.Routines;

public sealed class NightWindowTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    [Theory]
    [InlineData("2026-07-23T23:30:00+00:00", true)]
    [InlineData("2026-07-24T02:00:00+00:00", true)]
    [InlineData("2026-07-24T07:00:00+00:00", false)]
    public void Midnight_crossing_window_is_deterministic(string instant, bool expected)
    {
        var window = new NightWindow(new TimeOnly(23, 0), new TimeOnly(6, 0));
        Assert.Equal(expected, window.Contains(DateTimeOffset.Parse(instant), Utc));
    }
}
```

- [ ] **Step 2: Run tests and verify failure**

```powershell
dotnet test .\tests\Hushward.Core.Tests\Hushward.Core.Tests.csproj --filter "FullyQualifiedName~Routines"
```

Expected: compilation fails because the new domain types do not exist.

- [ ] **Step 3: Implement immutable primitives**

Use these exact public shapes:

```csharp
namespace Hushward.Core.Actions;

public enum NightAction
{
    ShutDown,
    Hibernate,
    Sleep,
    Lock,
    WarnOnly
}

public sealed record AuthorizedAlternative(
    NightAction Primary,
    NightAction Alternative,
    string ConditionCode);
```

```csharp
namespace Hushward.Core.Routines;

public enum WakePolicy
{
    NeverWake,
    WakeToEvaluate,
    WakeToActWhenEligible
}

public enum LatestDecisionPolicy
{
    KeepWaitingForProtections,
    UseAuthorizedAlternative,
    WarnAndAbandon
}

public readonly record struct NightWindow(TimeOnly Earliest, TimeOnly Latest)
{
    public bool Contains(DateTimeOffset now, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(now, timeZone).TimeOfDay;
        var earliest = Earliest.ToTimeSpan();
        var latest = Latest.ToTimeSpan();
        return earliest <= latest
            ? local >= earliest && local <= latest
            : local >= earliest || local <= latest;
    }
}
```

```csharp
using Hushward.Core.Actions;

namespace Hushward.Core.Routines;

public sealed record NightRoutine(
    Guid Id,
    string Name,
    bool Enabled,
    DayOfWeek[] Days,
    NightWindow Window,
    TimeSpan MinimumIdle,
    NightAction PrimaryAction,
    TimeSpan WarningDuration,
    WakePolicy WakePolicy,
    LatestDecisionPolicy LatestDecisionPolicy,
    IReadOnlyList<AuthorizedAlternative> AuthorizedAlternatives)
{
    public static NightRoutine CreateDefault(Guid id) => new(
        id,
        "Mi rutina nocturna",
        false,
        [DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday],
        new NightWindow(new TimeOnly(1, 0), new TimeOnly(6, 0)),
        TimeSpan.FromMinutes(20),
        NightAction.Hibernate,
        TimeSpan.FromSeconds(45),
        WakePolicy.NeverWake,
        LatestDecisionPolicy.KeepWaitingForProtections,
        []);

    public IReadOnlyList<RoutineValidationError> Validate() => RoutineValidation.Validate(this);
}

public sealed record TonightOverride(
    Guid RoutineId,
    DateTimeOffset ExpiresAt,
    NightAction? Action,
    TimeOnly? Earliest,
    DateTimeOffset? PostponedUntil,
    bool PauseUntilTomorrow,
    bool DisableWake,
    bool RequireManualConfirmation);
```

Implement exact warning bounds from the approved spec in `RoutineValidation` and reject duplicate IDs, empty days, nonpositive idle, overlapping alternatives, an alternative equal to its primary action, and action-specific warning durations outside the approved range.

- [ ] **Step 4: Run tests**

```powershell
dotnet test .\tests\Hushward.Core.Tests\Hushward.Core.Tests.csproj --filter "FullyQualifiedName~Routines"
```

Expected: PASS.

- [ ] **Step 5: Add boundary cases**

Add tests for normal windows, midnight crossing, exact boundaries, invalid warnings, duplicate alternatives, empty days, and DST spring/fall instants using an available Windows time zone. Core must receive `TimeZoneInfo`; it must not read the machine zone directly.

- [ ] **Step 6: Commit**

```powershell
git add src/Hushward.Core tests/Hushward.Core.Tests
git commit -m "feat: add explicit nightly routine model"
```

---

### Task 3: Add Classified Protection Evidence and Privacy-Bounded Signal Contracts

**Files:**
- Create: `src/Hushward.Core/Protections/ProtectionClass.cs`
- Create: `src/Hushward.Core/Protections/ProtectionCategory.cs`
- Create: `src/Hushward.Core/Protections/ObservationState.cs`
- Create: `src/Hushward.Core/Protections/ProtectionSignal.cs`
- Create: `src/Hushward.Core/Protections/ProtectionPolicy.cs`
- Create: `src/Hushward.Core/Protections/ProtectionSummary.cs`
- Test: `tests/Hushward.Core.Tests/Protections/ProtectionPolicyTests.cs`

**Interfaces:**
- Produces `ProtectionSignal`, the only detector evidence Core accepts.
- Produces `ProtectionPolicy.Resolve`, which classifies active/expired/unknown evidence without deciding the final action.

- [ ] **Step 1: Write failing policy tests**

```csharp
using Hushward.Core.Protections;

namespace Hushward.Core.Tests.Protections;

public sealed class ProtectionPolicyTests
{
    [Fact]
    public void Unknown_required_detector_is_critical()
    {
        var signal = ProtectionSignal.Unknown(
            detectorId: "audio",
            category: ProtectionCategory.Media,
            observedAt: DateTimeOffset.Parse("2026-07-23T01:00:00Z"),
            reasonCode: "detector.timeout");

        var summary = ProtectionPolicy.Summarize([signal], DateTimeOffset.Parse("2026-07-23T01:00:01Z"));

        Assert.True(summary.HasCriticalBlock);
        Assert.Contains(signal, summary.Critical);
    }

    [Fact]
    public void Expired_temporary_signal_does_not_block()
    {
        var signal = new ProtectionSignal(
            "network",
            ProtectionCategory.Transfer,
            ProtectionClass.Temporary,
            ObservationState.Active,
            "transfer.sustained",
            "Protection.Transfer",
            DateTimeOffset.Parse("2026-07-23T00:00:00Z"),
            DateTimeOffset.Parse("2026-07-23T00:05:00Z"),
            null);

        var summary = ProtectionPolicy.Summarize([signal], DateTimeOffset.Parse("2026-07-23T00:06:00Z"));

        Assert.False(summary.HasTemporaryBlock);
    }
}
```

- [ ] **Step 2: Run and verify failure**

```powershell
dotnet test .\tests\Hushward.Core.Tests\Hushward.Core.Tests.csproj --filter "FullyQualifiedName~ProtectionPolicyTests"
```

Expected: compilation failure.

- [ ] **Step 3: Implement typed evidence**

```csharp
namespace Hushward.Core.Protections;

public enum ProtectionClass { Critical, Temporary, Contextual }

public enum ObservationState { Inactive, Active, Unknown }

public enum ProtectionCategory
{
    RemoteSession,
    Meeting,
    CameraOrMicrophone,
    Backup,
    RenderBuildOrCompute,
    WindowsUpdate,
    UserSelectedProcess,
    Transfer,
    Media,
    FullscreenOrPresentation,
    ResourceWorkload,
    PowerTransition,
    SessionTransition,
    SystemTransition
}

public sealed record ProtectionSignal(
    string DetectorId,
    ProtectionCategory Category,
    ProtectionClass Class,
    ObservationState State,
    string ReasonCode,
    string ExplanationKey,
    DateTimeOffset ObservedAt,
    DateTimeOffset? ExpiresAt,
    string? FriendlyApplicationLabel)
{
    public static ProtectionSignal Unknown(
        string detectorId,
        ProtectionCategory category,
        DateTimeOffset observedAt,
        string reasonCode) => new(
            detectorId,
            category,
            ProtectionClass.Critical,
            ObservationState.Unknown,
            reasonCode,
            "Protection.Unknown",
            observedAt,
            null,
            null);
}
```

`ProtectionSummary` must expose separate immutable lists for critical, temporary, contextual, expired, and inactive evidence plus `HasCriticalBlock` and `HasTemporaryBlock`.

- [ ] **Step 4: Enforce privacy in the contract**

Do not add properties for window title, URL, document, file path, command line, browser tab, screenshot, clipboard, audio content, video content, or raw input. A transient process name may exist only inside Infrastructure and must be mapped to `FriendlyApplicationLabel` before crossing the port.

- [ ] **Step 5: Run tests**

```powershell
dotnet test .\tests\Hushward.Core.Tests\Hushward.Core.Tests.csproj --filter "FullyQualifiedName~Protections"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/Hushward.Core/Protections tests/Hushward.Core.Tests/Protections
git commit -m "feat: classify protected activity evidence"
```

---

### Task 4: Build the Deterministic Night Policy Engine and Proportional Warning Rules

**Files:**
- Create: `src/Hushward.Core/Decisions/NightDecisionKind.cs`
- Create: `src/Hushward.Core/Decisions/DecisionReasonCode.cs`
- Create: `src/Hushward.Core/Decisions/NightEvaluationInput.cs`
- Create: `src/Hushward.Core/Decisions/NightDecision.cs`
- Create: `src/Hushward.Core/Decisions/NightPolicyEngine.cs`
- Create: `src/Hushward.Core/Warnings/WarningPolicy.cs`
- Create: `src/Hushward.Core/Warnings/WarningState.cs`
- Test: `tests/Hushward.Core.Tests/Decisions/NightPolicyEngineTests.cs`
- Test: `tests/Hushward.Core.Tests/Warnings/WarningPolicyTests.cs`

**Interfaces:**
- Consumes Task 2 routine primitives and Task 3 evidence.
- Produces one pure `NightPolicyEngine.Evaluate(NightEvaluationInput)` decision.
- Produces stable reason codes; presentation text remains outside Core.

- [ ] **Step 1: Write the safety matrix as failing tests**

```csharp
using Hushward.Core.Actions;
using Hushward.Core.Decisions;
using Hushward.Core.Protections;
using Hushward.Core.Routines;

namespace Hushward.Core.Tests.Decisions;

public sealed class NightPolicyEngineTests
{
    [Fact]
    public void Disabled_routine_never_warns_or_executes()
    {
        var input = NightEvaluationInputBuilder.Eligible() with
        {
            Routine = NightRoutine.CreateDefault(Guid.NewGuid())
        };

        var result = NightPolicyEngine.Evaluate(input);

        Assert.Equal(NightDecisionKind.Disabled, result.Kind);
        Assert.Null(result.AuthorizedAction);
    }

    [Fact]
    public void Critical_protection_blocks_shutdown()
    {
        var input = NightEvaluationInputBuilder.Eligible(NightAction.ShutDown) with
        {
            Protections = new ProtectionSummary(
                Critical: [ProtectionSignal.Unknown("audio", ProtectionCategory.Media, DateTimeOffset.UtcNow, "detector.failure")],
                Temporary: [], Contextual: [], Expired: [], Inactive: [])
        };

        var result = NightPolicyEngine.Evaluate(input);

        Assert.Equal(NightDecisionKind.Protected, result.Kind);
        Assert.Null(result.AuthorizedAction);
    }

    [Fact]
    public void No_implicit_fallback_when_primary_is_unavailable()
    {
        var input = NightEvaluationInputBuilder.Eligible(NightAction.Hibernate) with
        {
            SupportedActions = [NightAction.Sleep, NightAction.ShutDown]
        };

        var result = NightPolicyEngine.Evaluate(input);

        Assert.Equal(NightDecisionKind.CapabilityBlocked, result.Kind);
        Assert.Null(result.AuthorizedAction);
    }
}
```

Create a test-only `NightEvaluationInputBuilder` under `tests/Hushward.Core.Tests/TestSupport` with fixed timestamps and no machine-clock dependency.

- [ ] **Step 2: Verify failure**

```powershell
dotnet test .\tests\Hushward.Core.Tests\Hushward.Core.Tests.csproj --filter "FullyQualifiedName~NightPolicyEngineTests"
```

Expected: compilation failure.

- [ ] **Step 3: Implement decision contracts**

```csharp
namespace Hushward.Core.Decisions;

public enum NightDecisionKind
{
    Disabled,
    OutsideSchedule,
    WaitingForIdle,
    Protected,
    Degraded,
    CapabilityBlocked,
    ManualConfirmationRequired,
    ReadyToWarn,
    WarningActive,
    AuthorizedToExecute,
    AbandonedForNight
}

public enum DecisionReasonCode
{
    RoutineDisabled,
    OutsideNightWindow,
    DayNotSelected,
    IdleThresholdNotMet,
    CriticalProtectionActive,
    TemporaryProtectionActive,
    RequiredEvidenceUnknown,
    ActionUnsupported,
    ManualConfirmationRequired,
    LatestDecisionReached,
    AuthorizedAlternativeSelected,
    Ready,
    WarningCancelledByInput,
    WarningCancelledByProtection,
    FinalCheckFailed
}
```

`NightEvaluationInput` must include only immutable values: evaluation time, time zone, effective routine, effective tonight override, idle duration, protection summary, supported actions, warning state, current session/power transition flags, and whether an update/install/recovery operation is active.

`NightDecision` must contain `Kind`, optional `AuthorizedAction`, primary reason, supporting reasons, optional warning duration, and next evaluation time. It must never contain localized text.

- [ ] **Step 4: Implement evaluation precedence**

Use this exact order:

```text
1. routine/override enabled
2. selected local day and nightly window
3. installation/update/recovery blockers
4. action capability and explicit authorization
5. fresh required detector health
6. critical protections
7. non-expired temporary protections
8. idle threshold and re-arm state
9. latest-decision policy
10. manual-confirmation override
11. warning lifecycle
12. final authorization
```

A lower item may never override an earlier blocking item. `WarnOnly` may proceed when evidence is unknown, but its decision must include the degradation reason.

- [ ] **Step 5: Implement proportional warning defaults**

```csharp
using Hushward.Core.Actions;

namespace Hushward.Core.Warnings;

public static class WarningPolicy
{
    public static TimeSpan DefaultFor(NightAction action) => action switch
    {
        NightAction.ShutDown => TimeSpan.FromSeconds(60),
        NightAction.Hibernate => TimeSpan.FromSeconds(45),
        NightAction.Sleep => TimeSpan.FromSeconds(30),
        NightAction.Lock => TimeSpan.FromSeconds(10),
        NightAction.WarnOnly => TimeSpan.Zero,
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };
}
```

Add exact min/max validation from the design spec.

- [ ] **Step 6: Add exhaustive cases**

Cover all actions, detector unknown, critical/temporary/contextual evidence, warning cancellation, fresh idle requirement, capability absence, latest-decision policies, explicitly authorized alternative, unsupported alternative, and final-check invalidation.

- [ ] **Step 7: Verify and commit**

```powershell
dotnet test .\tests\Hushward.Core.Tests\Hushward.Core.Tests.csproj --filter "FullyQualifiedName~Decisions|FullyQualifiedName~Warnings"
git add src/Hushward.Core tests/Hushward.Core.Tests
git commit -m "feat: add deterministic night policy engine"
```

---

### Task 5: Add Tonight Overrides, Overlap Validation, and Production-Parity Simulation

**Files:**
- Create: `src/Hushward.Core/Routines/EffectiveNightPlan.cs`
- Create: `src/Hushward.Core/Routines/RoutineOverlapDetector.cs`
- Create: `src/Hushward.Core/Routines/TonightOverrideResolver.cs`
- Create: `src/Hushward.Core/Simulation/NightSimulationRequest.cs`
- Create: `src/Hushward.Core/Simulation/NightSimulationResult.cs`
- Create: `src/Hushward.Core/Simulation/NightPolicySimulator.cs`
- Test: `tests/Hushward.Core.Tests/Routines/TonightOverrideResolverTests.cs`
- Test: `tests/Hushward.Core.Tests/Routines/RoutineOverlapDetectorTests.cs`
- Test: `tests/Hushward.Core.Tests/Simulation/NightPolicySimulatorTests.cs`

**Interfaces:**
- Consumes Tasks 2-4.
- Produces `EffectiveNightPlan` used by all application coordinators.
- Simulator delegates to the same `NightPolicyEngine`; it has no action port.

- [ ] **Step 1: Write failing override-expiry and parity tests**

```csharp
[Fact]
public void Temporary_action_expires_without_mutating_routine()
{
    var routine = TestRoutines.Enabled(NightAction.ShutDown);
    var temporary = new TonightOverride(
        routine.Id,
        DateTimeOffset.Parse("2026-07-24T06:00:00Z"),
        NightAction.Hibernate,
        null,
        null,
        false,
        false,
        false);

    var effective = TonightOverrideResolver.Resolve(
        routine,
        temporary,
        DateTimeOffset.Parse("2026-07-24T01:00:00Z"));

    Assert.Equal(NightAction.Hibernate, effective.Action);
    Assert.Equal(NightAction.ShutDown, routine.PrimaryAction);
}

[Fact]
public void Simulation_matches_direct_policy_evaluation()
{
    var request = NightSimulationRequestBuilder.Eligible();
    var simulated = NightPolicySimulator.Simulate(request);
    var direct = NightPolicyEngine.Evaluate(request.ToEvaluationInput());

    Assert.Equal(direct, simulated.Decision);
}
```

- [ ] **Step 2: Run and verify failure**

```powershell
dotnet test .\tests\Hushward.Core.Tests\Hushward.Core.Tests.csproj --filter "FullyQualifiedName~TonightOverride|FullyQualifiedName~Overlap|FullyQualifiedName~Simulation"
```

- [ ] **Step 3: Implement resolution rules**

An active override may change only the action, earliest time, postponement, pause-until-tomorrow, wake setting, and manual-confirmation requirement. It must not weaken critical-protection handling or create an unauthorized alternative. Expired overrides resolve to `null` and are eligible for removal from persistence.

Overlap detection must compare enabled routines in local time, including midnight-crossing windows and selected day carry-over. Any overlap produces a typed conflict list and disables automatic execution until resolved.

- [ ] **Step 4: Implement simulator**

```csharp
namespace Hushward.Core.Simulation;

public static class NightPolicySimulator
{
    public static NightSimulationResult Simulate(NightSimulationRequest request)
    {
        var decision = NightPolicyEngine.Evaluate(request.ToEvaluationInput());
        return new NightSimulationResult(
            decision,
            decision.AuthorizedAction,
            decision.PrimaryReason,
            decision.SupportingReasons,
            decision.NextEvaluationAt,
            decision.Kind == NightDecisionKind.ReadyToWarn);
    }
}
```

Do not reference Infrastructure or action executors.

- [ ] **Step 5: Verify and commit**

```powershell
dotnet test .\tests\Hushward.Core.Tests\Hushward.Core.Tests.csproj --filter "FullyQualifiedName~Routines|FullyQualifiedName~Simulation"
git add src/Hushward.Core tests/Hushward.Core.Tests
git commit -m "feat: add tonight overrides and policy simulation"
```

---

### Task 6: Define Application Ports and the Canonical Runtime Snapshot

**Files:**
- Create: `src/Hushward.Application/Abstractions/IClock.cs`
- Create: `src/Hushward.Application/Abstractions/IIdleStateProvider.cs`
- Create: `src/Hushward.Application/Abstractions/IProtectionDetector.cs`
- Create: `src/Hushward.Application/Abstractions/IPowerStateProvider.cs`
- Create: `src/Hushward.Application/Abstractions/ISessionStateProvider.cs`
- Create: `src/Hushward.Application/Abstractions/INightActionExecutor.cs`
- Create: `src/Hushward.Application/Abstractions/IScheduleSynchronizer.cs`
- Create: `src/Hushward.Application/Abstractions/IConfigurationStore.cs`
- Create: `src/Hushward.Application/Abstractions/IHistoryStore.cs`
- Create: `src/Hushward.Application/Abstractions/IUpdateService.cs`
- Create: `src/Hushward.Application/Runtime/NightRuntimeSnapshot.cs`
- Create: `src/Hushward.Application/Runtime/RuntimeState.cs`
- Create: `src/Hushward.Application/Results/OperationResult.cs`
- Test: `tests/Hushward.Application.Tests/Runtime/NightRuntimeSnapshotTests.cs`

**Interfaces:**
- Consumes Core types.
- Produces stable ports implemented by Infrastructure.
- Produces immutable monotonically sequenced snapshots rendered by every UI surface.

- [ ] **Step 1: Write failing snapshot tests**

```csharp
namespace Hushward.Application.Tests.Runtime;

public sealed class NightRuntimeSnapshotTests
{
    [Fact]
    public void Newer_sequence_replaces_older_sequence()
    {
        var older = TestSnapshots.Create(sequence: 10);
        var newer = TestSnapshots.Create(sequence: 11);

        Assert.True(newer.IsNewerThan(older));
        Assert.False(older.IsNewerThan(newer));
    }

    [Fact]
    public void Stale_snapshot_cannot_authorize_execution()
    {
        var snapshot = TestSnapshots.Create(
            sequence: 10,
            capturedAt: DateTimeOffset.Parse("2026-07-23T01:00:00Z"));

        Assert.True(snapshot.IsStaleAt(
            DateTimeOffset.Parse("2026-07-23T01:01:00Z"),
            TimeSpan.FromSeconds(30)));
    }
}
```

- [ ] **Step 2: Implement typed results and ports**

```csharp
namespace Hushward.Application.Results;

public sealed record OperationError(string Code, string MessageKey, string? TechnicalDetail);

public sealed record OperationResult<T>(T? Value, OperationError? Error)
{
    public bool IsSuccess => Error is null;
    public static OperationResult<T> Success(T value) => new(value, null);
    public static OperationResult<T> Failure(string code, string messageKey, string? detail = null) =>
        new(default, new OperationError(code, messageKey, detail));
}
```

```csharp
using Hushward.Core.Actions;
using Hushward.Core.Protections;

namespace Hushward.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    TimeZoneInfo LocalTimeZone { get; }
}

public interface IProtectionDetector
{
    string Id { get; }
    Task<ProtectionSignal> ObserveAsync(CancellationToken cancellationToken);
}

public interface INightActionExecutor
{
    Task<OperationResult<Unit>> ExecuteAsync(NightAction action, CancellationToken cancellationToken);
}
```

Define `Unit` as a zero-field readonly record struct. Every side-effect port returns `OperationResult<T>`; exceptions are caught at the Infrastructure boundary.

- [ ] **Step 3: Implement snapshot shape**

`NightRuntimeSnapshot` must contain the exact approved fields: sequence, captured time, monitoring state, effective plan, active routine, current window, idle, power, session, protection summary, detector health, decision, primary/supporting reasons, warning, execution, wake schedule health, persistence health, update state, next evaluation, and last meaningful event.

```csharp
public bool IsNewerThan(NightRuntimeSnapshot other) => Sequence > other.Sequence;

public bool IsStaleAt(DateTimeOffset now, TimeSpan maximumAge) =>
    now - CapturedAt > maximumAge;
```

- [ ] **Step 4: Add architecture tests**

Create `tests/Hushward.Application.Tests/Architecture/ProjectReferenceTests.cs` that loads project files as XML and asserts Core has no project references, Application references only Core, and App is the only project allowed to reference WPF.

- [ ] **Step 5: Verify and commit**

```powershell
dotnet test .\tests\Hushward.Application.Tests\Hushward.Application.Tests.csproj
git add src/Hushward.Application tests/Hushward.Application.Tests
git commit -m "feat: define application ports and runtime snapshot"
```

---

### Task 7: Implement Serialized Evaluation, Warning, and Exactly-Once Action Coordination

**Files:**
- Create: `src/Hushward.Application/Coordinators/NightGuardCoordinator.cs`
- Create: `src/Hushward.Application/Coordinators/ProtectionCoordinator.cs`
- Create: `src/Hushward.Application/Coordinators/WarningCoordinator.cs`
- Create: `src/Hushward.Application/Coordinators/ActionCoordinator.cs`
- Create: `src/Hushward.Application/Coordinators/TonightOverrideCoordinator.cs`
- Create: `src/Hushward.Application/Runtime/RuntimeSnapshotPublisher.cs`
- Create: `src/Hushward.Application/Warnings/WarningInvalidation.cs`
- Test: `tests/Hushward.Application.Tests/Coordinators/NightGuardCoordinatorTests.cs`
- Test: `tests/Hushward.Application.Tests/Coordinators/WarningCoordinatorTests.cs`
- Test: `tests/Hushward.Application.Tests/Coordinators/ActionCoordinatorTests.cs`

**Interfaces:**
- Consumes Tasks 4-6.
- Produces `IObservable<NightRuntimeSnapshot>` or an equivalent subscription abstraction with immediate latest-snapshot delivery.
- `ActionCoordinator.ExecuteOnceAsync(long authorizedSequence, NightAction action, ...)` is the only application path to a state-changing adapter.

- [ ] **Step 1: Write race and exactly-once tests with fakes**

```csharp
[Fact]
public async Task Two_finalization_requests_execute_action_once()
{
    var executor = new RecordingNightActionExecutor();
    var coordinator = new ActionCoordinator(executor);

    await Task.WhenAll(
        coordinator.ExecuteOnceAsync(42, NightAction.Hibernate, CancellationToken.None),
        coordinator.ExecuteOnceAsync(42, NightAction.Hibernate, CancellationToken.None));

    Assert.Single(executor.Calls);
}

[Fact]
public async Task Input_event_invalidates_active_warning()
{
    var warning = WarningTestHarness.Active(sequence: 12);

    await warning.Coordinator.InvalidateAsync(
        new WarningInvalidation(WarningInvalidationKind.UserInput, "input.resumed"));

    Assert.Equal(WarningStateKind.Cancelled, warning.Latest.State.Kind);
    Assert.True(warning.Latest.RequiresFreshIdle);
}
```

- [ ] **Step 2: Run and verify failure**

```powershell
dotnet test .\tests\Hushward.Application.Tests\Hushward.Application.Tests.csproj --filter "FullyQualifiedName~Coordinator"
```

- [ ] **Step 3: Implement coordinator ownership rules**

`NightGuardCoordinator` owns a `SemaphoreSlim(1,1)` around state commits, but detector observations may run concurrently with individual timeouts. Join observations into one freshness-bounded set, evaluate Core once, increment sequence once, publish once.

`WarningCoordinator` binds a warning session to its authorizing snapshot sequence. It cancels on input, protection activation, power transition, session transition, suspend/resume, display topology change, routine/config change, update/install start, or application shutdown.

`ActionCoordinator` rejects a duplicate sequence, action mismatch, stale authorization, or cancellation. It must not retry an action automatically after an adapter failure.

- [ ] **Step 4: Add final-check test**

The warning completion path must call `NightGuardCoordinator.EvaluateFinalAsync(expectedSequence, cancellationToken)` and execute only when the returned decision is `AuthorizedToExecute` with the same action. Test that a new protection between countdown start and completion prevents the executor call.

- [ ] **Step 5: Verify and commit**

```powershell
dotnet test .\tests\Hushward.Application.Tests\Hushward.Application.Tests.csproj --filter "FullyQualifiedName~Coordinator"
git add src/Hushward.Application tests/Hushward.Application.Tests
git commit -m "feat: coordinate safe nightly execution"
```

---

### Task 8: Add Versioned Configuration, Atomic Persistence, Legacy Migration, and Safe Mode

**Files:**
- Create: `src/Hushward.Application/Configuration/HushwardConfiguration.cs`
- Create: `src/Hushward.Application/Configuration/ConfigurationEnvelope.cs`
- Create: `src/Hushward.Application/Configuration/ConfigurationCoordinator.cs`
- Create: `src/Hushward.Application/Configuration/ConfigurationHealth.cs`
- Create: `src/Hushward.Infrastructure/Persistence/JsonConfigurationStore.cs`
- Create: `src/Hushward.Infrastructure/Persistence/AtomicFileWriter.cs`
- Create: `src/Hushward.Infrastructure/Migration/LegacySettingsReader.cs`
- Create: `src/Hushward.Infrastructure/Migration/LegacyToHushwardMigrator.cs`
- Create: `src/Hushward.Infrastructure/Migration/MigrationReceipt.cs`
- Test: `tests/Hushward.Infrastructure.Tests/Persistence/JsonConfigurationStoreTests.cs`
- Test: `tests/Hushward.Infrastructure.Tests/Migration/LegacyToHushwardMigratorTests.cs`
- Fixtures: `tests/Hushward.Infrastructure.Tests/Fixtures/legacy-settings-valid.json`, `legacy-settings-corrupt.json`, `hushward-v1-valid.json`

**Interfaces:**
- Implements Task 6 `IConfigurationStore`.
- Produces schema version `2` for the transformed product.
- Produces visible safe-mode state when live and backup configuration cannot be validated.

- [ ] **Step 1: Write failing atomicity and migration tests**

```csharp
[Fact]
public async Task Corrupt_live_file_restores_last_known_good_backup()
{
    using var temp = new TemporaryDirectory();
    await File.WriteAllTextAsync(temp.PathOf("config.json"), "{not-json");
    await File.WriteAllTextAsync(temp.PathOf("config.backup.json"), TestJson.ValidEnvelope);
    var store = new JsonConfigurationStore(temp.Path);

    var result = await store.LoadAsync(CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal(ConfigurationSource.Backup, result.Value!.Source);
    Assert.True(File.Exists(temp.PathOf("config.invalid.json")));
}

[Fact]
public async Task Legacy_enabled_setting_migrates_to_disabled_review_required_routine()
{
    var legacy = LegacySettingsFixture.EnabledAtOneAm;
    var migrated = await LegacyToHushwardMigrator.MigrateAsync(legacy, CancellationToken.None);

    Assert.Single(migrated.Configuration.Routines);
    Assert.False(migrated.Configuration.Routines[0].Enabled);
    Assert.True(migrated.Configuration.RequiresMigrationReview);
}
```

- [ ] **Step 2: Implement envelope**

```csharp
public sealed record ConfigurationEnvelope(
    int SchemaVersion,
    string ProductVersion,
    DateTimeOffset WrittenAt,
    HushwardConfiguration Settings);

public sealed record HushwardConfiguration(
    IReadOnlyList<NightRoutine> Routines,
    TonightOverride? TonightOverride,
    IReadOnlyList<ProtectionRule> ProtectionRules,
    PrivacySettings Privacy,
    UiPreferences UiPreferences,
    InstallationState InstallationState,
    bool RequiresMigrationReview);
```

Keep `schemaVersion`, `productVersion`, and `writtenAt` at the JSON root using explicit `JsonPropertyName` attributes. Reject unknown future major schemas instead of discarding fields.

- [ ] **Step 3: Implement exact atomic write sequence**

1. serialize and validate to `config.tmp.json`;
2. flush file contents to disk;
3. rotate current valid file to `config.backup.json`;
4. replace live `config.json` atomically where supported;
5. reopen and validate the live file;
6. on failure preserve invalid data and restore backup;
7. never silently replace with defaults.

- [ ] **Step 4: Implement migration receipt**

Receipt fields: source folder, source settings SHA-256, source executable version when available, migration time, target schema, new config SHA-256, old startup registration state, old wake-task state, and completion state. Never include usernames or full user-profile paths; store paths relative to `%LOCALAPPDATA%`.

- [ ] **Step 5: Implement safe mode**

If neither live nor backup validates, return `ConfigurationHealth.RecoveryRequired`, disable automatic actions/wake/update installation, preserve status and diagnostics, and expose restore/reset/export choices. A reset requires explicit user confirmation and archives the invalid files.

- [ ] **Step 6: Verify and commit**

```powershell
dotnet test .\tests\Hushward.Infrastructure.Tests\Hushward.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Persistence|FullyQualifiedName~Migration"
git add src/Hushward.Application/Configuration src/Hushward.Infrastructure/Persistence src/Hushward.Infrastructure/Migration tests/Hushward.Infrastructure.Tests
git commit -m "feat: add recoverable versioned configuration"
```

---

### Task 9: Add Coalesced Local History and Redacted Diagnostic Export

**Files:**
- Create: `src/Hushward.Application/History/HistoryEvent.cs`
- Create: `src/Hushward.Application/History/HistoryEventKind.cs`
- Create: `src/Hushward.Application/History/HistoryCoordinator.cs`
- Create: `src/Hushward.Application/Diagnostics/DiagnosticsCoordinator.cs`
- Create: `src/Hushward.Application/Diagnostics/DiagnosticSnapshot.cs`
- Create: `src/Hushward.Infrastructure/History/JsonLinesHistoryStore.cs`
- Create: `src/Hushward.Infrastructure/Diagnostics/DiagnosticBundleWriter.cs`
- Test: `tests/Hushward.Application.Tests/History/HistoryCoordinatorTests.cs`
- Test: `tests/Hushward.Infrastructure.Tests/History/JsonLinesHistoryStoreTests.cs`
- Test: `tests/Hushward.Infrastructure.Tests/Diagnostics/DiagnosticBundleWriterTests.cs`

**Interfaces:**
- Implements Task 6 `IHistoryStore`.
- Normal history stores stable event/reason/category codes and safe labels only.
- Diagnostic export is explicit, previewable, and redacted.

- [ ] **Step 1: Write failing coalescing and redaction tests**

```csharp
[Fact]
public async Task Repeated_identical_waiting_state_is_coalesced()
{
    var store = new RecordingHistoryStore();
    var coordinator = new HistoryCoordinator(store, TimeSpan.FromMinutes(10));
    var evt = TestHistory.Waiting("transfer.sustained");

    await coordinator.RecordAsync(evt, CancellationToken.None);
    await coordinator.RecordAsync(evt with { OccurredAt = evt.OccurredAt.AddMinutes(1) }, CancellationToken.None);

    Assert.Single(store.Events);
    Assert.Equal(2, store.Events[0].OccurrenceCount);
}

[Theory]
[InlineData("C:\\Users\\Ana\\secret.txt")]
[InlineData("https://example.test/private")]
[InlineData("--token abc123")]
public async Task Diagnostic_bundle_never_contains_sensitive_raw_values(string secret)
{
    var bundle = await DiagnosticBundleWriter.WriteAsync(
        DiagnosticFixture.WithTechnicalDetail(secret), CancellationToken.None);

    Assert.DoesNotContain(secret, bundle.ManifestText, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Implement history model**

```csharp
public sealed record HistoryEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    HistoryEventKind Kind,
    string ReasonCode,
    string? CategoryCode,
    string? FriendlyApplicationLabel,
    int OccurrenceCount,
    DateTimeOffset LastOccurredAt);
```

Support retention values `Off`, `7`, `14`, and `30` days; default `14`. Coalesce only semantically identical events within the configured interval. Do not store every evaluation tick.

- [ ] **Step 3: Implement diagnostic allow-list**

The bundle may include app/Windows versions, architecture, power capabilities, redacted configuration, task health, typed recent events, detector health, normalized errors, and local crash metadata. It must exclude user/profile names, full paths, command lines, window titles, URLs, documents, file names, secrets, raw input, media, screenshots, and process dumps.

- [ ] **Step 4: Verify and commit**

```powershell
dotnet test .\tests\Hushward.Application.Tests\Hushward.Application.Tests.csproj --filter "FullyQualifiedName~History"
dotnet test .\tests\Hushward.Infrastructure.Tests\Hushward.Infrastructure.Tests.csproj --filter "FullyQualifiedName~History|FullyQualifiedName~Diagnostics"
git add src/Hushward.Application/History src/Hushward.Application/Diagnostics src/Hushward.Infrastructure/History src/Hushward.Infrastructure/Diagnostics tests
git commit -m "feat: add private local history and diagnostics"
```

---

### Task 10: Implement Windows Power, Session, Capability, and Action Adapters

**Files:**
- Create: `src/Hushward.Infrastructure/Power/WindowsPowerStateProvider.cs`
- Create: `src/Hushward.Infrastructure/Power/WindowsPowerCapabilitiesProvider.cs`
- Create: `src/Hushward.Infrastructure/Power/WindowsNightActionExecutor.cs`
- Create: `src/Hushward.Infrastructure/Sessions/WindowsSessionStateProvider.cs`
- Create: `src/Hushward.Infrastructure/Sessions/SystemTransitionMonitor.cs`
- Create: `src/Hushward.Infrastructure/Input/WindowsIdleStateProvider.cs`
- Create: `src/Hushward.Infrastructure/Interop/NativeMethods.cs`
- Test: `tests/Hushward.Infrastructure.Tests/Power/WindowsNightActionExecutorTests.cs`
- Test: `tests/Hushward.Infrastructure.Tests/Power/WindowsPowerCapabilitiesProviderTests.cs`
- Test: `tests/Hushward.Infrastructure.Tests/Sessions/SystemTransitionMonitorTests.cs`

**Interfaces:**
- Implements Task 6 power/session/idle/action ports.
- All native calls sit behind injectable narrow wrappers so tests use fakes.

- [ ] **Step 1: Write command-safety and capability tests**

```csharp
[Fact]
public async Task Shutdown_uses_exact_non_forced_arguments()
{
    var process = new RecordingProcessLauncher();
    var executor = new WindowsNightActionExecutor(process, new RecordingPowerApi(), new RecordingSessionApi());

    var result = await executor.ExecuteAsync(NightAction.ShutDown, CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal("shutdown.exe", process.FileName);
    Assert.Equal(["/s", "/t", "0"], process.ArgumentList);
    Assert.DoesNotContain("/f", process.ArgumentList);
}

[Fact]
public async Task Unsupported_hibernate_returns_typed_failure_without_fallback()
{
    var executor = WindowsActionFixture.WithHibernateUnavailable();

    var result = await executor.ExecuteAsync(NightAction.Hibernate, CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal("power.hibernate.unsupported", result.Error!.Code);
    Assert.Empty(executor.InvokedActions);
}
```

- [ ] **Step 2: Implement action mapping**

```text
ShutDown -> shutdown.exe /s /t 0 through ArgumentList; never /f
Hibernate -> supported Windows power API after capability check
Sleep     -> supported Windows power API after capability check
Lock      -> LockWorkStation through a wrapper
WarnOnly  -> no OS action and success result
```

Do not silently substitute actions. Catch native/process exceptions, normalize them to typed failures, and never auto-retry.

- [ ] **Step 3: Implement state providers**

Power state must include AC/battery/unknown, percentage, charging state, hibernate/sleep capability, and recent power-transition timestamp. Session state must include locked/unlocked, local/remote, and recent lock/unlock/suspend/resume/display-topology timestamps. Input provider exposes aggregate idle duration only.

- [ ] **Step 4: Verify and commit**

```powershell
dotnet test .\tests\Hushward.Infrastructure.Tests\Hushward.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Power|FullyQualifiedName~Session|FullyQualifiedName~Idle"
git add src/Hushward.Infrastructure tests/Hushward.Infrastructure.Tests
git commit -m "feat: add safe Windows power and session adapters"
```

---

### Task 11: Implement Local Protection Detectors and Freshness-Bounded Aggregation

**Files:**
- Create: `src/Hushward.Infrastructure/Detectors/DetectorBase.cs`
- Create: `src/Hushward.Infrastructure/Detectors/RemoteSessionDetector.cs`
- Create: `src/Hushward.Infrastructure/Detectors/MeetingDetector.cs`
- Create: `src/Hushward.Infrastructure/Detectors/MediaDetector.cs`
- Create: `src/Hushward.Infrastructure/Detectors/FullscreenDetector.cs`
- Create: `src/Hushward.Infrastructure/Detectors/ResourceWorkloadDetector.cs`
- Create: `src/Hushward.Infrastructure/Detectors/TransferDetector.cs`
- Create: `src/Hushward.Infrastructure/Detectors/WindowsUpdateDetector.cs`
- Create: `src/Hushward.Infrastructure/Detectors/ProtectedProcessDetector.cs`
- Create: `src/Hushward.Infrastructure/Detectors/DetectorEvidenceSanitizer.cs`
- Modify: `src/Hushward.Application/Coordinators/ProtectionCoordinator.cs`
- Test: `tests/Hushward.Infrastructure.Tests/Detectors/DetectorContractTests.cs`
- Test: `tests/Hushward.Application.Tests/Coordinators/ProtectionCoordinatorTests.cs`

**Interfaces:**
- Implements `IProtectionDetector`.
- Each detector returns one typed signal or `Unknown`; it never throws across the port.
- Aggregation applies per-detector timeout and common freshness boundary.

- [ ] **Step 1: Write detector contract tests**

```csharp
[Theory]
[MemberData(nameof(AllDetectors))]
public async Task Detector_failure_becomes_unknown_instead_of_throwing(IProtectionDetector detector)
{
    var signal = await detector.ObserveAsync(CancellationToken.None);

    Assert.Equal(ObservationState.Unknown, signal.State);
    Assert.Equal(ProtectionClass.Critical, signal.Class);
    Assert.Null(signal.FriendlyApplicationLabel);
}

[Fact]
public async Task Timed_out_detector_blocks_automatic_action()
{
    var coordinator = ProtectionFixture.WithNeverCompletingDetector(timeout: TimeSpan.FromMilliseconds(20));

    var result = await coordinator.ObserveAsync(CancellationToken.None);

    Assert.True(result.Summary.HasCriticalBlock);
    Assert.Contains(result.Health, health => health.Code == "detector.timeout");
}
```

- [ ] **Step 2: Implement conservative detectors**

Use OS metadata only. Do not inspect or persist titles, URLs, files, tabs, message content, screenshots, clipboard, audio, video, or keystrokes. Resource and transfer detectors require sustained samples rather than a single spike. Known process matching is user-configured and maps to a friendly label before returning evidence.

Meeting/camera/microphone detection must return `Unknown` when the required Windows API is unavailable or permissions prevent a reliable observation. Do not infer a meeting solely because Teams/Zoom is running; combine process presence with available activity evidence.

- [ ] **Step 3: Preserve explicit class configuration**

Default mappings follow the approved spec. A user may change allowed category classes through bounded settings, but `Unknown` required evidence remains critical and cannot be downgraded. Windows Update reboot-critical activity and remote session remain critical.

- [ ] **Step 4: Add resource-cost guardrails**

No detector may poll continuously. The coordinator schedules one-shot observations, reuses short-lived samples when fresh, and backs off outside nightly precheck windows. Add tests proving cancellation stops sampling and inactive monitoring creates no recurring detector loop.

- [ ] **Step 5: Verify and commit**

```powershell
dotnet test .\tests\Hushward.Infrastructure.Tests\Hushward.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Detector"
dotnet test .\tests\Hushward.Application.Tests\Hushward.Application.Tests.csproj --filter "FullyQualifiedName~ProtectionCoordinator"
git add src/Hushward.Infrastructure/Detectors src/Hushward.Application/Coordinators tests
git commit -m "feat: detect protected Windows activity locally"
```

---

### Task 12: Synchronize Dynamic Wake Tasks with Effective Routines

**Files:**
- Create: `src/Hushward.Application/Scheduling/DesiredWakeSchedule.cs`
- Create: `src/Hushward.Application/Coordinators/ScheduleSyncCoordinator.cs`
- Create: `src/Hushward.Infrastructure/Scheduling/WindowsTaskSchedulerSync.cs`
- Create: `src/Hushward.Infrastructure/Scheduling/TaskSchedulerCommandBuilder.cs`
- Create: `src/Hushward.Infrastructure/Scheduling/WakeTaskHealthReader.cs`
- Test: `tests/Hushward.Application.Tests/Scheduling/ScheduleSyncCoordinatorTests.cs`
- Test: `tests/Hushward.Infrastructure.Tests/Scheduling/TaskSchedulerCommandBuilderTests.cs`
- Modify: startup handling in `src/Hushward.App/App.xaml.cs`

**Interfaces:**
- Implements `IScheduleSynchronizer`.
- Uses one product-owned task name such as `Hushward-NightWake` and removes obsolete legacy/product-owned tasks only after health verification.
- Scheduled launch uses `--scheduled-check` and signals an existing instance without opening the UI.

- [ ] **Step 1: Write schedule mapping tests**

```csharp
[Fact]
public void Never_wake_produces_no_task()
{
    var routine = TestRoutines.Enabled() with { WakePolicy = WakePolicy.NeverWake };
    Assert.Null(DesiredWakeSchedule.From(routine, TimeZoneInfo.Utc));
}

[Fact]
public void Wake_schedule_tracks_routine_earliest_time()
{
    var routine = TestRoutines.Enabled() with
    {
        Window = new NightWindow(new TimeOnly(1, 0), new TimeOnly(6, 0)),
        WakePolicy = WakePolicy.WakeToEvaluate
    };

    var desired = DesiredWakeSchedule.From(routine, TimeZoneInfo.Utc)!;

    Assert.Equal(new TimeOnly(0, 30), desired.LocalStartTime);
    Assert.Equal("--scheduled-check", desired.Arguments);
}
```

- [ ] **Step 2: Implement desired schedule**

Compute precheck from the routine's earliest time using the approved lead time, preserve local-time semantics through DST, and regenerate on routine/time-zone/wake-policy changes. When multiple non-overlapping routines require wake, create deterministic triggers under one product-owned task or a documented deterministic set; never retain the fixed legacy `00:30` behavior when the configured routine differs.

- [ ] **Step 3: Implement failure degradation**

Task creation failure records typed health, shows a user-actionable explanation, and falls back to in-session monitoring. It must not disable the routine or broaden privileges. Never mutate global `powercfg` settings silently.

- [ ] **Step 4: Add migration ordering test**

Prove the old task remains until the Hushward task is created and read back as healthy; then remove the old task. On failure, preserve the old registration but keep the migrated routine disabled pending review.

- [ ] **Step 5: Verify and commit**

```powershell
dotnet test .\tests\Hushward.Application.Tests\Hushward.Application.Tests.csproj --filter "FullyQualifiedName~Schedule"
dotnet test .\tests\Hushward.Infrastructure.Tests\Hushward.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Scheduling"
git add src/Hushward.Application/Scheduling src/Hushward.Application/Coordinators/ScheduleSyncCoordinator.cs src/Hushward.Infrastructure/Scheduling src/Hushward.App tests
git commit -m "feat: synchronize wake tasks with nightly routines"
```

---

### Task 13: Establish the Hushward WPF Shell, Design Tokens, Localization, and Composition Root

**Files:**
- Create: `src/Hushward.App/Program.cs`
- Modify: `src/Hushward.App/App.xaml`, `App.xaml.cs`, `Hushward.App.csproj`
- Replace: `src/Hushward.App/MainWindow.xaml`, `MainWindow.xaml.cs`
- Create: `src/Hushward.App/Views/ShellWindow.xaml`, `ShellWindow.xaml.cs`
- Create: `src/Hushward.App/ViewModels/ShellViewModel.cs`
- Create: `src/Hushward.App/Presentation/ObservableObject.cs`
- Create: `src/Hushward.App/Presentation/AsyncCommand.cs`
- Create: `src/Hushward.App/Design/Colors.xaml`
- Create: `src/Hushward.App/Design/Typography.xaml`
- Create: `src/Hushward.App/Design/Controls.xaml`
- Create: `src/Hushward.App/Design/Motion.xaml`
- Create: `src/Hushward.App/Resources/Strings.resx`
- Create: `src/Hushward.App/Localization/UiText.cs`
- Create: `src/Hushward.App/Assets/Hushward.ico`
- Test: `tests/Hushward.App.Tests/Architecture/CompositionRootTests.cs`
- Test: `tests/Hushward.App.Tests/Localization/ResourceCompletenessTests.cs`

**Interfaces:**
- Consumes Application coordinators/snapshots and Infrastructure adapters.
- App owns composition only; ViewModels receive interfaces/coordinators and never instantiate Infrastructure.
- All surfaces subscribe to the same snapshot publisher.

- [ ] **Step 1: Add failing architecture and localization tests**

```csharp
[Fact]
public void ViewModels_do_not_reference_infrastructure_namespace()
{
    var files = Directory.GetFiles(TestPaths.AppViewModels, "*.cs", SearchOption.AllDirectories);
    foreach (var file in files)
    {
        var text = File.ReadAllText(file);
        Assert.DoesNotContain("using Hushward.Infrastructure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("new Windows", text, StringComparison.Ordinal);
    }
}

[Fact]
public void Every_referenced_ui_key_exists_in_spanish_resources()
{
    var missing = ResourceAudit.FindMissingKeys(TestPaths.AppRoot);
    Assert.Empty(missing);
}
```

- [ ] **Step 2: Add exact brand tokens**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Color x:Key="DeepNightColor">#0B1020</Color>
  <Color x:Key="NightSlateColor">#141B2D</Color>
  <Color x:Key="MistColor">#E8ECF3</Color>
  <Color x:Key="QuietGrayColor">#98A3B8</Color>
  <Color x:Key="BeaconColor">#F2B84B</Color>
  <Color x:Key="SafeColor">#66C7A5</Color>
  <Color x:Key="WarningColor">#F29E4C</Color>
  <Color x:Key="CriticalColor">#E56B6F</Color>
</ResourceDictionary>
```

Create brushes from these colors, reusable spacing/radius/focus styles, and high-contrast-compatible state templates. Do not hardcode colors or user-facing text in view files.

- [ ] **Step 3: Build the composition root**

`Program.Main` performs Velopack bootstrap hooks before WPF startup, then calls `App.Run`. `App.xaml.cs` creates singleton coordinators/adapters in one composition method, acquires single-instance ownership, handles `--startup`, `--scheduled-check`, `--exit`, and routes second-instance messages. It must not contain policy decisions.

- [ ] **Step 4: Build the calm shell**

Shell navigation has four primary destinations: `Inicio`, `Esta noche`, `Rutinas`, `Protecciones`. Settings/history/diagnostics/update/recovery are secondary. Use a responsive minimum layout that remains usable at 560x640 and scales cleanly at 100-200% DPI. Respect reduced motion; animations remain 120-220 ms and never convey essential state alone.

- [ ] **Step 5: Verify and commit**

```powershell
dotnet test .\tests\Hushward.App.Tests\Hushward.App.Tests.csproj --filter "FullyQualifiedName~Architecture|FullyQualifiedName~Localization"
dotnet build .\Hushward.sln -c Release
git add src/Hushward.App tests/Hushward.App.Tests
git commit -m "feat: establish Hushward WPF shell and design system"
```

---

### Task 14: Build Onboarding and the Four Focused Product Areas

**Files:**
- Create: `src/Hushward.App/Views/Onboarding/OnboardingWindow.xaml`, `.xaml.cs`
- Create: `src/Hushward.App/ViewModels/Onboarding/OnboardingViewModel.cs`
- Create: `src/Hushward.App/Views/Home/HomeView.xaml`, `.xaml.cs`
- Create: `src/Hushward.App/ViewModels/Home/HomeViewModel.cs`
- Create: `src/Hushward.App/Views/Tonight/TonightView.xaml`, `.xaml.cs`
- Create: `src/Hushward.App/ViewModels/Tonight/TonightViewModel.cs`
- Create: `src/Hushward.App/Views/Routines/RoutinesView.xaml`, `.xaml.cs`
- Create: `src/Hushward.App/ViewModels/Routines/RoutinesViewModel.cs`
- Create: `src/Hushward.App/Views/Protections/ProtectionsView.xaml`, `.xaml.cs`
- Create: `src/Hushward.App/ViewModels/Protections/ProtectionsViewModel.cs`
- Create: `src/Hushward.App/Views/Secondary/HistoryView.xaml`
- Create: `src/Hushward.App/Views/Secondary/DiagnosticsView.xaml`
- Create: `src/Hushward.App/Views/Secondary/RecoveryView.xaml`
- Test: `tests/Hushward.App.Tests/ViewModels/OnboardingViewModelTests.cs`
- Test: `tests/Hushward.App.Tests/ViewModels/HomeViewModelTests.cs`
- Test: `tests/Hushward.App.Tests/ViewModels/TonightViewModelTests.cs`
- Test: `tests/Hushward.App.Tests/ViewModels/RoutinesViewModelTests.cs`
- Test: `tests/Hushward.App.Tests/ViewModels/ProtectionsViewModelTests.cs`

**Interfaces:**
- ViewModels consume snapshots and coordinator commands only.
- Onboarding saves a disabled draft, then enables only after explicit confirmation of the natural-language summary.
- Routine simulation calls the Core simulator through Application.

- [ ] **Step 1: Write onboarding safety test**

```csharp
[Fact]
public async Task Routine_remains_disabled_until_summary_is_confirmed()
{
    var harness = OnboardingHarness.Create();

    await harness.ViewModel.NextAsync();
    await harness.ViewModel.NextAsync();
    await harness.ViewModel.NextAsync();

    Assert.False(harness.SavedRoutine.Enabled);

    await harness.ViewModel.ConfirmAndEnableAsync();

    Assert.True(harness.SavedRoutine.Enabled);
}
```

- [ ] **Step 2: Implement four onboarding steps**

1. explain what Hushward does and never does;
2. choose primary action;
3. choose days, earliest time, latest time, and inactivity;
4. review protections, startup, and optional wake behavior.

The final summary must be a complete Spanish sentence derived from typed policy data, for example: `De domingo a jueves, Hushward podrá hibernar desde la 01:00 tras 20 minutos sin actividad. Nunca actuará durante una protección crítica.`

- [ ] **Step 3: Implement primary views**

`Inicio`: current status, effective tonight plan, next action, primary reason, active protections, health, one primary quick action.

`Esta noche`: temporary action/time/postpone/pause/wake/manual-confirmation changes with explicit expiry and permanent-routine comparison.

`Rutinas`: list/editor, non-overlap validation, action-specific warning bounds, latest-decision policy, wake behavior, explicit alternatives, preview, simulator.

`Protecciones`: category/class overview, user-selected applications, detector health, safe test controls, privacy explanation. Required unknown evidence cannot be downgraded.

- [ ] **Step 4: Implement explanation presenters**

Map stable reason codes to short Spanish resources. Never show raw detector IDs or exceptions as primary copy. Technical details belong only in diagnostics.

- [ ] **Step 5: Verify and commit**

```powershell
dotnet test .\tests\Hushward.App.Tests\Hushward.App.Tests.csproj --filter "FullyQualifiedName~ViewModel"
dotnet build .\Hushward.sln -c Release
git add src/Hushward.App tests/Hushward.App.Tests
git commit -m "feat: add guided Hushward product experience"
```

---

### Task 15: Build Tray Flyout, Warning Overlay, Accessibility, and Multi-Monitor Behavior

**Files:**
- Create: `src/Hushward.App/Tray/TrayIconHost.cs`
- Create: `src/Hushward.App/Tray/TrayFlyoutWindow.xaml`, `.xaml.cs`
- Create: `src/Hushward.App/ViewModels/Tray/TrayFlyoutViewModel.cs`
- Create: `src/Hushward.App/Warnings/WarningWindow.xaml`, `.xaml.cs`
- Create: `src/Hushward.App/ViewModels/Warnings/WarningViewModel.cs`
- Create: `src/Hushward.App/Warnings/WarningPlacementService.cs`
- Create: `src/Hushward.App/Accessibility/LiveRegionAnnouncer.cs`
- Test: `tests/Hushward.App.Tests/ViewModels/TrayFlyoutViewModelTests.cs`
- Test: `tests/Hushward.App.Tests/ViewModels/WarningViewModelTests.cs`
- Test: `tests/Hushward.App.Tests/Warnings/WarningPlacementServiceTests.cs`

**Interfaces:**
- Tray and warning render the same `NightRuntimeSnapshot` as the main app.
- Warning commands call `WarningCoordinator`; they never call an OS action.
- Placement service uses active-monitor work area and DPI-aware coordinates.

- [ ] **Step 1: Write warning cancellation tests**

```csharp
[Fact]
public async Task Any_user_input_cancels_warning_and_requests_fresh_idle()
{
    var harness = WarningHarness.Active(NightAction.ShutDown, TimeSpan.FromSeconds(60));

    await harness.ViewModel.HandleUserInputAsync();

    Assert.Equal(WarningStateKind.Cancelled, harness.Coordinator.State.Kind);
    Assert.True(harness.Coordinator.State.RequiresFreshIdle);
    Assert.Empty(harness.ActionExecutor.Calls);
}

[Fact]
public void Tray_status_comes_from_snapshot_without_recomputation()
{
    var snapshot = TestSnapshots.Protected("backup.active");
    var vm = new TrayFlyoutViewModel(TestSnapshotSource.From(snapshot), TestCommands.None);

    Assert.Equal(snapshot.PrimaryReason, vm.PrimaryReasonCode);
}
```

- [ ] **Step 2: Implement tray states**

Only: off, ready, waiting, protected, warning, degraded/error. No continuous animation. Flyout shows current state, effective plan, action/time, top waiting reason, protection count, postpone/pause, `Esta noche`, main app, and exit.

- [ ] **Step 3: Implement warning overlay**

Show action, remaining time, eligibility reason, `Cancelar`, postpone 15/30/60 minutes, `Cambiar acción` only when an explicit alternative exists, and `Mantener activo hasta mañana`. Visible warning remains mandatory even when sound is disabled. Do not imitate the Windows lock screen or trap focus.

- [ ] **Step 4: Implement accessibility and display behavior**

Keyboard navigation and Escape cancellation must work. Announce warning start, 30 seconds, 10 seconds, and final 5 seconds without announcing every tick. Place on the active monitor, remain usable across topology changes, and invalidate/re-evaluate on DPI/display changes. Respect reduced motion and high contrast.

- [ ] **Step 5: Manual smoke test without real action**

Start the app with a test-only `--warning-preview` argument available only in Debug builds. Confirm all controls, keyboard cancellation, screen reader names, 100/125/150/200% DPI, and two monitors. Release builds must not expose a path that bypasses normal authorization.

- [ ] **Step 6: Verify and commit**

```powershell
dotnet test .\tests\Hushward.App.Tests\Hushward.App.Tests.csproj --filter "FullyQualifiedName~Tray|FullyQualifiedName~Warning"
dotnet build .\Hushward.sln -c Release
git add src/Hushward.App tests/Hushward.App.Tests
git commit -m "feat: add accessible tray and warning surfaces"
```

---

### Task 16: Add Per-User Installer, Optional Updates, Rollback, and Clean Uninstall

**Files:**
- Modify: `src/Hushward.App/Hushward.App.csproj`
- Modify: `src/Hushward.App/Program.cs`
- Create: `src/Hushward.Application/Updates/UpdateCoordinator.cs`
- Create: `src/Hushward.Application/Updates/UpdateState.cs`
- Create: `src/Hushward.Infrastructure/Updates/VelopackUpdateService.cs`
- Create: `scripts/Publish-Hushward.ps1`
- Create: `scripts/Package-Hushward.ps1`
- Create: `scripts/Test-InstallLifecycle.ps1`
- Create: `docs/release/INSTALLATION.md`
- Create: `docs/release/UPDATE-AND-ROLLBACK.md`
- Test: `tests/Hushward.Application.Tests/Updates/UpdateCoordinatorTests.cs`
- Test: `tests/Hushward.Infrastructure.Tests/Updates/VelopackUpdateServiceTests.cs`

**Interfaces:**
- Implements `IUpdateService` with Velopack 1.2.0.
- Update checks are manual or opt-in notifications; installation is never forced.
- Update installation is blocked during warning, pending/executing action, migration, recovery-required state, or unresolved degradation.

- [ ] **Step 1: Add pinned package and WPF bootstrap**

Add to `Hushward.App.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Velopack" Version="1.2.0" />
</ItemGroup>
<PropertyGroup>
  <StartupObject>Hushward.App.Program</StartupObject>
</PropertyGroup>
<ItemGroup>
  <ApplicationDefinition Remove="App.xaml" />
  <Page Include="App.xaml" />
</ItemGroup>
```

`Program.Main` must invoke Velopack app hooks before constructing WPF.

- [ ] **Step 2: Write update-blocking tests**

```csharp
[Theory]
[InlineData(true, false, false, false)]
[InlineData(false, true, false, false)]
[InlineData(false, false, true, false)]
[InlineData(false, false, false, true)]
public async Task Unsafe_runtime_state_blocks_update_install(
    bool warning, bool action, bool migration, bool recovery)
{
    var coordinator = UpdateHarness.Create(warning, action, migration, recovery);

    var result = await coordinator.InstallAsync(CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Empty(coordinator.Service.InstallCalls);
}
```

- [ ] **Step 3: Implement update policy**

Checks may query a configured static release feed/GitHub Releases only on explicit request or when the user opted into notifications. Do not send device identifiers, settings, history, or detector data. Verify package integrity through Velopack metadata and code signature when signing is configured. Never claim signing is complete without a real certificate pipeline.

- [ ] **Step 4: Implement packaging scripts**

`Publish-Hushward.ps1` publishes self-contained `win-x64` release output to `artifacts/publish/win-x64`.

`Package-Hushward.ps1` requires an explicit semantic version and runs:

```powershell
vpk pack `
  --packId KappaBot.Hushward `
  --packVersion $Version `
  --packDir .\artifacts\publish\win-x64 `
  --mainExe Hushward.App.exe `
  --packTitle Hushward `
  --icon .\src\Hushward.App\Assets\Hushward.ico `
  --outputDir .\artifacts\releases
```

The per-user installer must target `%LOCALAPPDATA%`, require no elevation, and keep mutable data under `%LOCALAPPDATA%\Hushward` outside the replaceable install directory.

- [ ] **Step 5: Implement install lifecycle test**

The PowerShell test installs version A, creates representative configuration/history, updates to version B, verifies data and task/startup registrations, exercises rollback to A-compatible state, and uninstalls with both retain-data and remove-data choices. It must run in a disposable Windows test user or VM and never invoke a real power action.

- [ ] **Step 6: Verify and commit**

```powershell
dotnet test .\tests\Hushward.Application.Tests\Hushward.Application.Tests.csproj --filter "FullyQualifiedName~Update"
dotnet test .\tests\Hushward.Infrastructure.Tests\Hushward.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Update"
.\scripts\Publish-Hushward.ps1
.\scripts\Package-Hushward.ps1 -Version 0.1.0-test
git add src scripts docs/release tests
git commit -m "feat: add Hushward packaging updates and rollback"
```

---

### Task 17: Add CI, Architecture Gates, Privacy Audits, and Real-Hardware QA Documentation

**Files:**
- Create: `.github/workflows/ci.yml`
- Create: `.github/workflows/release.yml`
- Create: `scripts/Verify-Architecture.ps1`
- Create: `scripts/Verify-Privacy.ps1`
- Create: `scripts/Verify-Release.ps1`
- Create: `docs/quality/MANUAL-QA.md`
- Create: `docs/quality/ACCESSIBILITY.md`
- Create: `docs/quality/PRIVACY-REVIEW.md`
- Create: `docs/quality/RELEASE-GATES.md`
- Modify: `AGENTS.md`

**Interfaces:**
- Produces one local/CI verification entry point.
- Release workflow packages only after test, architecture, privacy, and documentation gates pass.

- [ ] **Step 1: Create local verification script**

`Verify-Release.ps1` must run, in order:

```powershell
dotnet format .\Hushward.sln --verify-no-changes
dotnet build .\Hushward.sln -c Release
dotnet test .\Hushward.sln -c Release --collect:"XPlat Code Coverage"
.\scripts\Verify-Architecture.ps1
.\scripts\Verify-Privacy.ps1
.\scripts\Publish-Hushward.ps1
```

Exit immediately on failure and print only concise stage status plus actionable error output.

- [ ] **Step 2: Implement architecture audit**

Fail when Core references Windows/WPF/file/process/registry/logging packages; Application references WPF or concrete Infrastructure; ViewModels instantiate Infrastructure; action adapters are reachable from test composition; user-facing strings are hardcoded outside resource or approved diagnostic-only files.

- [ ] **Step 3: Implement privacy audit**

Scan persisted DTOs, history, diagnostics, and detector contracts for forbidden fields/terms: window title, URL, browser tab, document/file name, clipboard, screenshot, audio/video content, keystroke, command line, token, secret. Allow test fixture mentions only through an explicit path allow-list.

- [ ] **Step 4: Document exact manual QA matrix**

Include declared Windows versions, desktop/laptop, AC/battery transitions, hibernation available/unavailable, suspend/resume, lock/unlock, remote desktop, one/two monitors, 100/125/150/200% DPI, fullscreen media, active audio, sustained transfer, protected render/build/backup, Windows Update, wake success/failure, clean install, legacy upgrade, corrupt config recovery, rollback, and uninstall choices. Each row has preconditions, steps, expected result, evidence, tester, date, and release version.

- [ ] **Step 5: Add CI**

CI runs on Windows, restores pinned dependencies, executes `Verify-Release.ps1`, and uploads test/coverage artifacts. Release workflow requires an annotated version tag, reruns all gates, builds Velopack packages, and creates draft artifacts; it does not publish an unsigned artifact as a trusted production release.

- [ ] **Step 6: Verify and commit**

```powershell
.\scripts\Verify-Release.ps1
git add .github scripts docs/quality AGENTS.md
git commit -m "ci: enforce Hushward release gates"
```

---

### Task 18: Complete Legacy Cutover, Documentation, Acceptance, and Independent Review

**Files:**
- Modify: `README.md`
- Modify: `AGENTS.md`
- Replace/update: `docs/ARCHITECTURE.md`, `docs/AI_CONTEXT.md`, `docs/UX_GUIDE.md`
- Create: `docs/PRIVACY.md`
- Create: `docs/TROUBLESHOOTING.md`
- Create: `docs/MIGRATION.md`
- Create: `docs/RECOVERY.md`
- Create: `docs/RELEASE-CHECKLIST.md`
- Remove only after parity: obsolete legacy ViewModel/orchestration/persistence/tray code and old installer script paths
- Preserve: migration readers/constants/fixtures for the documented support window

**Interfaces:**
- Produces final product documentation matching implemented behavior.
- Removes dual ownership only after all surfaces consume `NightRuntimeSnapshot` and migration tests pass.

- [ ] **Step 1: Prove no legacy orchestration remains**

Search and review:

```powershell
rg -n "SmartSleepShutdown|MainWindowViewModel|shutdown\.exe|TaskScheduler|settings\.json" src tests scripts docs
```

Allowed remaining `SmartSleepShutdown` occurrences: migration constants, migration code/tests/fixtures, historical migration documentation. Allowed `shutdown.exe`: only the action adapter and its tests/docs. No UI ViewModel may own timers, hardware adapters, persistence, or decision logic.

- [ ] **Step 2: Run full acceptance suite**

```powershell
.\scripts\Verify-Release.ps1
.\scripts\Test-InstallLifecycle.ps1 -VersionA 0.1.0-test.1 -VersionB 0.1.0-test.2
```

On representative Windows hardware, complete every row in `docs/quality/MANUAL-QA.md`, including migration from the current Smart Sleep Shutdown configuration and registrations. Record failures as issues; do not mark acceptance complete while any critical/high finding remains.

- [ ] **Step 3: Conduct independent reviews**

Use separate reviewers/workstreams for:

1. Core safety and race conditions;
2. Windows power/session/task integration;
3. WPF accessibility and localization;
4. persistence/migration/recovery;
5. privacy/security/update packaging;
6. documentation and release reproducibility.

Each reviewer must inspect code and tests, run relevant commands, and report findings by severity. Fix all critical/high issues and rerun affected gates.

- [ ] **Step 4: Validate all acceptance criteria**

Create a checked copy of the 19 acceptance criteria from the approved design in `docs/RELEASE-CHECKLIST.md`, with a link to test/QA evidence for each criterion. Do not use subjective statements such as `looks good`; cite a test, script result, screenshot, or manual QA row.

- [ ] **Step 5: Commit final cutover**

```powershell
git add -A
git commit -m "release: complete Hushward product transformation"
```

- [ ] **Step 6: Final verification before claiming completion**

```powershell
git status --short
.\scripts\Verify-Release.ps1
git log --oneline --decorate -20
```

Expected: clean worktree, every gate passes, focused task commits present. Do not claim real shutdown/hibernate/sleep/lock behavior is verified unless the documented manual hardware QA was actually performed.

---

## Six-Workstream Execution Map

When subagent parallelism is beneficial, use no more than these six workstreams:

1. **Domain safety:** Tasks 2-5 and Core tests.
2. **Application orchestration:** Tasks 6-9 and Application tests.
3. **Windows reliability:** Tasks 10-12 and Infrastructure tests.
4. **Product experience:** Tasks 13-15 and App tests.
5. **Distribution and recovery:** Task 16 plus install lifecycle.
6. **Quality and governance:** Tasks 17-18, independent review, docs, privacy/accessibility gates.

Do not run dependent workstreams against uncommitted interfaces. Rebase or merge only at phase gates, resolve conflicts centrally, and rerun the full solution after every integration.

## Completion Contract

The implementation is complete only when:

- all 18 tasks are checked and committed;
- all approved design acceptance criteria have linked evidence;
- `Verify-Release.ps1` passes from a clean checkout;
- installer upgrade/rollback/uninstall tests pass in a disposable Windows environment;
- real-hardware manual QA is completed and recorded;
- no unresolved critical/high review finding remains;
- no forbidden scope item was introduced;
- the final response reports only implemented changes, verification evidence, remaining non-blocking limitations, commit range, and release artifacts.