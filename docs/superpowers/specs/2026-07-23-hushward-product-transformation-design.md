# Hushward product transformation design

**Status:** Approved product direction, written for implementation planning  
**Date:** 2026-07-23  
**Repository:** `Kappa-Bot/smart-sleep-shutdown`  
**Working product name:** **Hushward**  
**Descriptor:** *Night guardian for Windows* / *Guardián nocturno para Windows*

> This document defines the intended product, experience, architecture, migration, safety model, and release gates. It does not authorize implementation shortcuts that weaken the existing safety invariants.

## 1. Executive summary

Smart Sleep Shutdown will become **Hushward**, a local-first Windows night guardian that leaves a personal computer in the state its owner explicitly chose after the night is over.

Hushward is not a generic automation engine, an energy dashboard, a sleep-health application, or a forced shutdown timer. Its core job is:

1. understand the user's declared nightly intention;
2. wait until the configured time and inactivity conditions are satisfied;
3. protect meaningful ongoing activity;
4. explain what it is waiting for;
5. warn before acting;
6. allow immediate cancellation or postponement;
7. re-evaluate immediately before execution;
8. perform only an explicitly authorized Windows action.

The transformation preserves the existing deterministic decision engine and its safety tests, but expands the product around it with routines, classified protections, multiple power/session actions, a calm tray-first experience, robust local persistence, diagnostics, installation, migration, and recovery.

## 2. Existing strengths to preserve

The current repository already contains valuable behavior that must be retained or strengthened:

- no silent shutdown;
- a visible warning countdown;
- cancellation when user input resumes;
- a final eligibility check immediately before execution;
- no forced process termination and no `shutdown /f`;
- detector failures block unsafe automatic action;
- cancellation requires a fresh idle period before another warning;
- deterministic Core logic separated from Windows side effects;
- single-instance behavior;
- tray-first operation;
- scheduled checks capable of waking the machine;
- test coverage around the decision state machine.

The transformation is evolutionary, not a rewrite. Existing behavior must remain covered by characterization tests while the new model is introduced.

## 3. Product position

### 3.1 Audience

Hushward is designed for people who leave a Windows desktop or laptop running at night because they:

- fall asleep while watching or listening to media;
- leave downloads, renders, transfers, backups, builds, or updates running;
- want the computer to lock, sleep, hibernate, shut down, or only warn later;
- do not trust a basic timer to distinguish inactivity from important background work;
- want local, explainable behavior without an account or cloud service.

### 3.2 Product promise

> Choose how the PC should end the night. Hushward waits, protects what matters, explains its decision, and acts only when it is safe and authorized.

### 3.3 Product characteristics

- **Calm:** visible only when useful; no alarmist copy or noisy dashboard.
- **Explainable:** every wait, delay, warning, cancellation, degradation, and action has a human-readable reason.
- **Conservative:** uncertainty blocks automatic power actions rather than guessing.
- **Local:** settings, state, history, and diagnostics remain on the device.
- **Bounded:** users configure guided policies, not arbitrary scripts or opaque automation graphs.
- **Reversible where possible:** temporary overrides expire automatically; configuration is backed up; upgrades support rollback.

### 3.4 Distribution position

Hushward is a free, open-source product with commercial-grade presentation and release discipline. This transformation does not add accounts, subscriptions, licensing, payments, or a backend.

## 4. Naming and brand

### 4.1 Name

The working product name is **Hushward**.

The name combines quiet operation with directional intent: the system moves the PC toward a safe, quiet end-of-night state without suggesting forced shutdown or medical sleep claims.

A preliminary availability scan found no prominent Windows power-management application using the exact name. This is product naming due diligence, not legal trademark clearance. Any commercial registration or paid distribution must receive a formal trademark review first.

### 4.2 Brand personality

The selected personality is **the silent sentinel**:

- protective, not controlling;
- precise, not mechanical;
- calm, not sleepy or whimsical;
- trustworthy, not invisible;
- premium, not decorative.

### 4.3 Visual direction

The identity must avoid generic moons, stars, power-button marks, robot imagery, and security shields copied from antivirus products.

The primary symbol should suggest a **protected threshold at night**: two descending or enclosing planes forming a quiet boundary, with a small warm beacon indicating authorized action. It must remain legible at 16 px in the Windows tray.

Initial design tokens:

| Token | Value | Purpose |
|---|---:|---|
| Deep Night | `#0B1020` | primary dark surface |
| Night Slate | `#141B2D` | raised surfaces |
| Mist | `#E8ECF3` | primary text on dark |
| Quiet Gray | `#98A3B8` | secondary text |
| Beacon | `#F2B84B` | planned/authorized action |
| Safe | `#66C7A5` | protected and healthy state |
| Warning | `#F29E4C` | countdown and degraded state |
| Critical | `#E56B6F` | blocked/error state |

Use Windows-native **Segoe UI Variable**. Do not bundle an external font. All text and interactive states must satisfy WCAG 2.2 AA contrast. Motion should be restrained, generally 120–220 ms, and respect reduced-motion settings.

### 4.4 Voice and terminology

The interface remains Spanish-first and uses short, concrete language.

Preferred patterns:

- `Esta noche: hibernar a partir de la 01:00`
- `Esperando: hay una copia de seguridad protegida`
- `El equipo se apagará en 60 segundos`
- `Se canceló porque volviste a usar el equipo`
- `No podemos comprobar la actividad de audio; la acción automática está bloqueada`

Avoid:

- unexplained detector names;
- raw process names as primary copy;
- anthropomorphic claims that the app “knows” the user is asleep;
- medical or wellness claims;
- threatening phrases such as `apagado inminente` unless an actual warning is active.

All user-facing strings must be moved to resources. Spanish is release-gating; the architecture must support additional locales without changing Core logic. English localization is not required for the first transformation release.

## 5. Functional model

### 5.1 Nightly intention

Each enabled routine declares one primary action:

- **Shut down**
- **Hibernate**
- **Sleep**
- **Lock**
- **Warn only**

An automatic alternative may be configured only when the user explicitly authorizes it. Hushward never invents a fallback action.

Example:

> Primary action: shut down. If the PC is running on battery below 20%, hibernate instead. No other automatic alternative is allowed.

### 5.2 Routine schedule

A routine contains:

- stable identifier;
- display name;
- enabled state;
- selected days of the week;
- earliest action time;
- minimum user inactivity;
- optional latest decision time;
- primary action;
- action-specific warning duration;
- wake policy;
- protection policy;
- explicitly authorized alternatives;
- optional temporary override state.

Routines use a bounded nightly window rather than an exact timer or arbitrary rule builder.

The initial release supports multiple routines in the domain and persistence model, while the UI prevents overlapping enabled routines for the same local time window. If an overlap is introduced through migration or corrupted data, Hushward disables automatic execution, explains the conflict, and asks the user to resolve it.

### 5.3 “Tonight” overrides

`Esta noche` provides temporary changes that expire at the end of the current nightly window:

- change the action;
- change the earliest time;
- postpone by 15, 30, or 60 minutes;
- pause until tomorrow;
- disable waking for this night;
- require manual confirmation for this night.

Temporary overrides never mutate the permanent routine. The UI must show both the effective plan and the routine it came from.

### 5.4 Latest decision time

A latest decision time does not override a critical protection.

At the latest decision time, the routine follows one explicit policy:

1. keep waiting until critical protections clear;
2. execute an authorized alternative if its conditions are satisfied;
3. warn only and abandon automatic action for that night.

Temporary and contextual protections may be overridden at the latest decision time only when the routine explicitly permits that protection category to expire. The default is to keep waiting.

## 6. Protection model

### 6.1 Protection classes

Every protection signal resolves to one of three classes.

#### Critical

Blocks every automatic power/session action. `Warn only` may still notify.

Default critical categories:

- active remote desktop or remote assistance session;
- active meeting or call evidence;
- microphone or camera actively in use when detectable;
- backup operation classified as protected;
- render, encode, build, or compute job explicitly protected by the user;
- Windows update installation or reboot-critical activity;
- user-selected critical process;
- detector failure or unavailable safety evidence.

#### Temporary

Delays the action while the signal remains valid or until the routine's explicit expiration policy applies.

Default temporary categories:

- sustained download or upload;
- media playback;
- fullscreen playback or presentation;
- sustained high GPU, CPU, disk, or network activity associated with a known workload;
- user-selected temporary process.

#### Contextual

Contributes to explanation and confidence but never blocks on its own.

Default contextual signals:

- moderate CPU, GPU, disk, or network activity;
- an application open in the background without active workload evidence;
- recent transition between AC power and battery;
- recently unlocked session;
- recently resumed system.

### 6.2 Signal contract

Every detector returns a typed result with:

- detector identifier;
- category identifier;
- protection class;
- active/inactive/unknown state;
- normalized reason code;
- user-safe explanation key and parameters;
- confidence/evidence metadata for diagnostics only;
- observation timestamp;
- optional expiry or staleness time;
- failure/degradation details when applicable.

No detector may directly decide the final action. Detectors provide evidence; Core policies decide.

### 6.3 Privacy boundary

Hushward may inspect operating-system metadata required to classify activity, but it must not collect or persist:

- window titles;
- document or file names;
- URLs;
- browser tabs;
- message or meeting content;
- screenshots;
- audio or video content;
- clipboard contents;
- keystrokes beyond aggregate idle/input timestamps.

Process names may be used transiently for matching user-selected protection rules. The normal history records the category and friendly application label, not a full command line or file path.

### 6.4 Detector failures

A detector exception, timeout, permission failure, unsupported API state, or stale result becomes an explicit `Unknown` signal.

Default policy:

- `Shut down`, `Hibernate`, `Sleep`, and `Lock`: blocked while required safety evidence is unknown.
- `Warn only`: allowed, but the warning explains the degradation.
- manual actions initiated directly by the user remain available after a confirmation that clearly describes what could not be checked.

## 7. Warning and execution safety

### 7.1 Warning defaults

| Action | Default warning | Allowed range | Presentation |
|---|---:|---:|---|
| Shut down | 60 s | 60–300 s | prominent overlay and optional sound |
| Hibernate | 45 s | 30–180 s | prominent overlay and optional sound |
| Sleep | 30 s | 15–120 s | visible overlay and optional sound |
| Lock | 10 s | 10–60 s | compact visible warning |
| Warn only | no execution countdown | n/a | notification/flyout |

Warnings are never silent by default. Sound can be disabled, but a visible warning remains mandatory for every automatic state-changing action.

### 7.2 Warning controls

Every warning provides:

- `Cancelar`;
- `Posponer 15 min`;
- `Posponer 30 min`;
- `Posponer 60 min`;
- `Cambiar acción` when a permitted alternative exists;
- `Mantener activo hasta mañana`.

Any fresh keyboard or mouse activity cancels the warning. A newly active critical or temporary protection cancels the warning. A system suspend, resume, lock/unlock, display topology change, or power-source transition invalidates the warning and forces a fresh evaluation.

After cancellation, a new warning cannot begin until the configured idle duration is satisfied again.

### 7.3 Final authorization check

Immediately before execution, Hushward must obtain a fresh snapshot and verify:

- the routine and effective `Esta noche` plan remain enabled;
- the nightly window is still valid;
- the action remains explicitly authorized;
- no critical or non-expired temporary protection is active;
- required detectors are healthy;
- user input has not resumed;
- no conflicting Hushward execution is in progress;
- the operating system reports the action as supported;
- installation/update/rollback operations are not active.

Failure of any check cancels execution and records a human-readable reason.

### 7.4 Windows action adapters

Actions are infrastructure adapters behind typed application interfaces.

- Shutdown must not force-close applications and must never add `/f`.
- Hibernate and sleep must verify platform capability before presenting the option.
- Lock must use the supported Windows session API.
- Automated tests must never invoke real power/session actions.
- The application must prevent duplicate execution attempts across processes and wake/resume races.

## 8. Wake behavior

Wake behavior is configured per routine and disabled by default.

Supported policies:

1. **Never wake:** evaluate only while Windows is already awake.
2. **Wake to evaluate:** wake near the earliest action time, evaluate, and return to normal monitoring without guaranteeing action.
3. **Wake to act when eligible:** wake, evaluate all conditions, warn visibly, and perform the authorized action only if the normal policy succeeds.

The onboarding and routine editor must explain that waking can turn on hardware, fans, displays, or attached devices.

The scheduled task must be generated from the actual effective routine schedule. A fixed `00:30` task detached from settings is not acceptable.

Task synchronization must:

- calculate a precheck time from the earliest action time;
- update after routine or wake-policy changes;
- handle daylight-saving transitions;
- remove obsolete tasks;
- expose health and last synchronization result;
- degrade to in-session monitoring if task creation fails;
- never grant broader privileges than required.

## 9. Desktop and laptop behavior

Hushward supports both desktops and laptops.

The runtime snapshot must include:

- AC or battery state;
- battery percentage and charging state when present;
- power capabilities;
- hibernation availability;
- Modern Standby capability when detectable;
- session lock state;
- remote-session state;
- display count and topology change timestamp;
- recent suspend/resume timestamp;
- docking/power transition evidence when available.

Default laptop policy:

- no automatic shutdown solely because battery is low;
- offer an explicit user-configurable battery alternative such as hibernate;
- a power-source change during warning cancels and re-evaluates;
- closing the lid does not bypass the routine or warning model;
- unavailable hibernation is shown as a capability issue, not silently replaced with sleep or shutdown.

## 10. Experience architecture

### 10.1 Surfaces

Hushward uses a calm hybrid model.

#### Tray flyout

Primary daily surface:

- current state;
- effective plan for tonight;
- planned action and earliest time;
- top reason for waiting;
- active critical/temporary protection count;
- pause/postpone controls;
- open `Esta noche`;
- open main application;
- exit.

The tray icon communicates only a small set of meaningful states: off, ready, waiting, protected, warning, degraded/error. It must not animate continuously.

#### Main application

Four primary areas:

1. **Inicio**
   - current status;
   - tonight summary;
   - next eligible action;
   - explanation of the current decision;
   - active protections;
   - health/degradation summary;
   - primary quick action.

2. **Esta noche**
   - effective temporary plan;
   - temporary action/time/wake changes;
   - postpone and pause controls;
   - current protections;
   - explicit expiry at the end of the night.

3. **Rutinas**
   - routine list;
   - days and nightly window;
   - inactivity;
   - primary action;
   - latest decision policy;
   - warning duration;
   - wake behavior;
   - authorized alternatives;
   - human-readable preview.

4. **Protecciones**
   - category overview;
   - class assignment;
   - protected applications;
   - detector health;
   - safe test/simulation controls;
   - privacy explanation.

Settings, local history, diagnostics, update, about, and recovery are secondary destinations and do not compete with primary navigation.

#### Warning overlay

A dedicated WPF warning surface appears above normal windows without imitating the Windows lock screen.

Requirements:

- clear action and countdown;
- explicit reason the action is eligible;
- visible cancellation and postponement;
- keyboard accessibility;
- correct placement on the active monitor and usable behavior across multiple monitors;
- high-DPI support;
- screen-reader announcements at meaningful countdown transitions rather than every second;
- no focus trap that prevents emergency cancellation;
- system-awake hold during the countdown when required to complete the warning safely.

### 10.2 Onboarding

First run uses four short steps:

1. what Hushward does and never does;
2. choose the primary nightly action;
3. choose days, earliest time, and inactivity;
4. review protections, Windows startup, and optional wake behavior.

The final page produces a complete sentence describing the effective policy. The routine starts disabled until the user explicitly confirms this summary.

### 10.3 Simulation

The routine editor provides a non-destructive simulator.

Inputs can include:

- local date/time;
- idle duration;
- AC/battery state;
- selected active protections;
- detector failure;
- resume/lock/remote-session state.

Output includes:

- resulting decision;
- chosen action or no action;
- blocking and contributing reasons;
- next evaluation time;
- whether a warning would begin;
- which explicit policy produced the result.

Simulation uses the same Core policy code as production and never calls infrastructure action adapters.

## 11. Local history and diagnostics

### 11.1 History

Local history is enabled by default with 14-day retention. User options are off, 7, 14, or 30 days.

Normal history events:

- routine evaluated;
- waiting reason changed;
- protection activated or cleared;
- warning started;
- warning cancelled and why;
- action executed;
- action failed;
- task synchronization changed;
- detector degraded or recovered;
- configuration migrated or restored.

Repeated evaluations with no meaningful state change are coalesced. History is a concise product explanation, not a continuous telemetry stream.

### 11.2 Diagnostic export

Export is always user-initiated and reviewable before sharing.

The bundle may contain:

- application version;
- Windows version and architecture;
- supported power capabilities;
- redacted configuration;
- scheduled-task health;
- recent typed event records;
- detector health and normalized errors;
- crash information generated locally.

It must exclude content, URLs, titles, file paths, command lines, secrets, personal identifiers, and raw media/input data.

## 12. Target architecture

The solution evolves to four production layers:

```text
Hushward.Core
Hushward.Application
Hushward.Infrastructure
Hushward.App
```

Test projects mirror each layer, with additional integration and UI smoke suites where justified.

### 12.1 Core

Pure, deterministic, side-effect-free domain logic:

- routines and nightly windows;
- effective `Esta noche` plan;
- action authorization;
- protection classifications and policies;
- action-specific warning policy;
- decision state machine;
- cancellation and re-arm rules;
- final eligibility rules;
- schedule calculations;
- simulation;
- normalized reason codes.

Core depends only on the base class library. It does not reference WPF, Windows APIs, file I/O, clocks, processes, or logging frameworks.

### 12.2 Application

Use-case orchestration and stable ports:

- `NightGuardCoordinator` — owns evaluation lifecycle and serializes state transitions;
- `RoutineCoordinator` — validates, saves, previews, and activates routines;
- `TonightOverrideCoordinator` — applies and expires temporary plans;
- `ProtectionCoordinator` — gathers detector evidence and normalizes health;
- `WarningCoordinator` — runs cancellation-aware warnings and final checks;
- `ActionCoordinator` — authorizes and invokes a typed action port once;
- `ScheduleSyncCoordinator` — keeps Task Scheduler aligned with routines;
- `ConfigurationCoordinator` — load, migrate, validate, backup, restore;
- `HistoryCoordinator` — coalesced local event recording and retention;
- `DiagnosticsCoordinator` — health model and redacted export;
- `UpdateCoordinator` — safe update checks, staging, and rollback boundaries.

Application references Core and interfaces, but no WPF types or concrete Windows APIs.

### 12.3 Infrastructure

Concrete adapters:

- monotonic/system clock;
- input idle time;
- power and battery state;
- Windows power capabilities;
- shutdown, hibernate, sleep, and lock;
- Task Scheduler;
- single-instance IPC;
- session lock/unlock and remote-session state;
- audio/media activity;
- camera/microphone usage where supported;
- CPU, GPU, disk, and network workload signals;
- process-category matching;
- Windows Update state;
- filesystem persistence;
- local history store;
- notifications and sound;
- update transport and package verification;
- installer/registration integration.

All adapters return typed results. Exceptions do not cross into Core or UI as control flow.

### 12.4 App

WPF composition and presentation:

- application startup and single-instance routing;
- shell and navigation;
- tray icon and flyout;
- onboarding;
- Home, Tonight, Routines, and Protections views;
- warning overlay;
- settings/history/diagnostics/update/recovery views;
- localized presenters;
- accessibility and focus behavior;
- dependency composition.

ViewModels consume application state and commands. They do not instantiate infrastructure, run monitoring loops, write files, call Task Scheduler, or build domain decisions.

## 13. Canonical runtime state

All visible surfaces render from one immutable `NightRuntimeSnapshot` produced by Application.

Minimum shape:

```text
NightRuntimeSnapshot
  Sequence
  CapturedAt
  MonitoringState
  EffectivePlan
  ActiveRoutine
  CurrentWindow
  IdleState
  PowerState
  SessionState
  ProtectionSummary
  DetectorHealth
  Decision
  PrimaryReason
  SupportingReasons
  WarningState
  ActionExecutionState
  WakeScheduleHealth
  PersistenceHealth
  UpdateState
  NextEvaluationAt
  LastMeaningfulEvent
```

Rules:

- snapshots are immutable and monotonically sequenced;
- tray, main window, warning surface, and history presenter do not independently recompute status;
- internal reason codes remain stable and are localized only in presentation;
- stale snapshots are recognizable and never interpreted as fresh eligibility;
- only the coordinator owning the current sequence may authorize execution.

This removes the current pattern in which a large main-window ViewModel owns hardware access, persistence, monitoring timers, decisions, and copy generation.

## 14. Runtime data flow

1. Startup acquires the single-instance lock and parses startup intent.
2. Configuration loads from versioned local storage, with migration and validation.
3. Application builds the effective routine and `Esta noche` plan.
4. ProtectionCoordinator requests detector observations with per-detector timeouts.
5. Core receives a normalized evaluation input and returns a typed decision.
6. Application publishes a new immutable runtime snapshot.
7. All visible surfaces update from that snapshot.
8. If the decision requests warning, WarningCoordinator starts a cancellable warning session bound to the snapshot sequence.
9. Input, protection, power, session, suspend/resume, configuration, or update events invalidate the warning and trigger re-evaluation.
10. At countdown completion, WarningCoordinator requests a fresh observation set and Core final authorization.
11. ActionCoordinator invokes exactly one explicitly authorized adapter operation.
12. Result, cancellation, or error is converted into a new snapshot and a coalesced local history event.

Only one evaluation or action transition may commit at a time. Slow detectors may run concurrently, but their results are joined into one observation set with a common freshness boundary.

## 15. Persistence and migration

### 15.1 Versioned configuration

Configuration uses a versioned envelope:

```text
schemaVersion
productVersion
writtenAt
settings
routines
tonightOverride
protectionRules
privacy
uiPreferences
installationState
```

Writes are atomic:

1. serialize and validate to a temporary file;
2. flush to disk;
3. rotate the previous valid file to backup;
4. replace the live file;
5. verify the new file can be read and validated.

### 15.2 Recovery

If live configuration is invalid:

1. attempt the last known-good backup;
2. preserve the invalid file for diagnostics;
3. enter a visible safe mode with automatic actions disabled;
4. offer restore, reset, or diagnostic export;
5. never silently replace user settings with defaults.

### 15.3 Existing product migration

The transformation must migrate from `%LOCALAPPDATA%\SmartSleepShutdown` to `%LOCALAPPDATA%\Hushward`.

Migration rules:

- detect the old settings once;
- copy before transforming;
- map the old single schedule and context settings to one disabled Hushward routine;
- preserve the old folder until the new configuration has completed at least one successful load after restart;
- write a migration receipt with source version and checksums;
- remove or replace old startup/task registrations only after new registrations are confirmed healthy;
- support rollback to the previous executable and configuration during the release's rollback window;
- never execute an automatic action merely because migration enabled a previously ambiguous setting.

The initial migrated routine remains disabled until the user reviews the new policy summary.

## 16. Installation, updates, and rollback

### 16.1 Installer

Provide a real per-user installer where Windows capabilities allow it.

Requirements:

- install under the user's local application directory;
- optional start-with-Windows registration;
- optional Task Scheduler registration only when wake behavior is enabled;
- clean upgrade detection;
- configuration preservation;
- clean uninstallation of binaries, startup registration, tasks, and app-owned caches;
- explicit choice to retain or remove user configuration/history;
- no permanent Windows service;
- no unexplained global `powercfg` mutation;
- no administrator requirement unless a selected Windows capability demonstrably requires it.

### 16.2 Updates

The product supports manual update checks and optional notification of a new release. It does not force updates.

An update cannot install while:

- a warning is active;
- an action is pending or executing;
- configuration migration is incomplete;
- the application is in an unresolved degraded state.

Packages must be integrity-verified before staging. Code signing is prepared as a release-hardening step but is not falsely represented as complete until a real certificate and signing process exist.

### 16.3 Rollback and safe mode

Rollback restores the previous compatible application package and last known-good configuration. Safe mode disables automatic actions, scheduled wake, and update installation while preserving read-only access to status, configuration, history, and diagnostics.

## 17. Error and degradation model

Errors are classified by impact:

- **Informational:** no behavior impact; shown only in diagnostics/history.
- **Degraded:** a nonessential capability is unavailable; core monitoring continues.
- **Blocking:** automatic action cannot be proven safe; action is disabled for the current evaluation.
- **Recovery required:** configuration, installation, or update state is inconsistent; safe mode activates.

Every error has:

- stable code;
- severity;
- user-safe summary;
- actionable next step when one exists;
- technical detail for local diagnostics;
- first/last occurrence and count;
- recovery state.

Do not swallow persistence errors, task synchronization failures, or detector exceptions. Do not expose raw exception messages as primary user copy.

## 18. Security and privacy posture

- no account or authentication;
- no cloud synchronization;
- no telemetry or analytics;
- no backend;
- no remote control;
- no inbound network listener;
- no plugin or arbitrary script execution;
- no custom shell commands;
- least-privilege Windows integration;
- update packages verified before use;
- diagnostics local and user-initiated;
- sensitive paths and identifiers redacted;
- deterministic policies preferred over adaptive or AI decisions.

Generative AI is outside product scope. Hushward must not infer that the user is asleep, make medical claims, or use behavioral profiling.

## 19. Explicit non-goals

This transformation does not include:

- generic IFTTT-style automation;
- action chains or arbitrary workflows;
- webhooks;
- user-provided scripts or commands;
- mobile or web applications;
- cloud dashboards;
- accounts, teams, or device synchronization;
- subscriptions or paid editions;
- energy-consumption analytics;
- productivity analytics;
- window/content surveillance;
- forced shutdown;
- a permanent Windows service;
- autonomous machine-learning policy decisions;
- automatic alternatives not explicitly authorized by the user.

## 20. Testing strategy

### 20.1 Automated tests

#### Core

- routine-window boundaries, including midnight and daylight-saving transitions;
- idle and re-arm behavior;
- protection class precedence;
- latest-decision policies;
- explicit alternative authorization;
- all action-specific warning policies;
- warning cancellation and final recheck;
- detector unknown/failure behavior;
- simulation parity with production decisions;
- no implicit fallback action.

#### Application

- coordinator serialization and race handling;
- immutable snapshot sequencing;
- warning invalidation events;
- exactly-once action invocation;
- schedule synchronization decisions;
- history coalescing and retention;
- safe-mode transitions;
- update blocking conditions;
- redacted diagnostic export.

#### Infrastructure

- settings atomic write, backup, restore, and corruption handling;
- old-configuration migration fixtures;
- Task Scheduler mapping and cleanup;
- power capability mapping;
- process and workload detector normalization;
- session/suspend/resume event handling;
- update package integrity validation;
- installer registration smoke tests where automation permits.

#### App

- ViewModel/presenter behavior from snapshots;
- navigation and command availability;
- onboarding policy summary;
- warning keyboard interactions;
- localization resource completeness;
- accessibility automation properties;
- DPI/layout smoke tests.

No automated test may perform a real shutdown, hibernate, sleep, or session lock.

### 20.2 Manual QA matrix

Documented manual validation must cover:

- supported Windows versions declared by the release;
- desktop and laptop;
- AC and battery transitions;
- hibernation available/unavailable;
- suspend and resume;
- session lock and unlock;
- remote desktop;
- one and multiple monitors;
- 100%, 125%, 150%, and 200% DPI;
- fullscreen media;
- active audio;
- sustained transfer;
- protected render/build/backup process;
- Windows Update activity;
- wake task success and failure;
- clean install;
- upgrade from Smart Sleep Shutdown;
- corrupt settings recovery;
- rollback;
- uninstall with retain/remove data choices.

## 21. Release gates

A phase may not be declared complete until:

- solution build passes without new warnings accepted as debt;
- automated tests pass;
- safety invariants remain covered;
- no real power action is reachable from tests;
- architecture boundaries are enforced;
- user-visible strings are resource-backed;
- errors have typed handling rather than silent catches;
- documentation matches implemented behavior;
- independent review finds no unresolved critical issue;
- the phase has a clean, focused commit history.

The final transformation release additionally requires:

- successful migration from a representative current configuration;
- verified installer upgrade/uninstall behavior;
- wake task synchronized from routines;
- manual QA checklist completed on real Windows hardware;
- accessibility review;
- no high-severity privacy or safety findings;
- rollback exercised successfully;
- README, architecture, privacy, troubleshooting, and release documentation updated.

## 22. Delivery phases

### Phase 1 — Foundations

- freeze and characterize existing safety behavior;
- establish Hushward naming and resource structure;
- introduce Core/Application/Infrastructure/App boundaries;
- define canonical runtime snapshot and typed results;
- add versioned persistence and migration framework;
- implement safe mode and configuration recovery.

### Phase 2 — Night policy core

- multiple routines and bounded nightly windows;
- `Esta noche` overrides;
- action authorization and alternatives;
- classified protections;
- action-specific warning policies;
- deterministic simulation;
- local history domain events.

### Phase 3 — Product experience

- design system and icon assets;
- onboarding;
- tray flyout;
- Inicio, Esta noche, Rutinas, and Protecciones;
- warning overlay;
- history, settings, diagnostics, update, and recovery surfaces;
- accessibility and localization extraction.

### Phase 4 — Windows integration

- shutdown/hibernate/sleep/lock adapters;
- battery and capability handling;
- remote/session/suspend/resume handling;
- detector categories;
- dynamic Task Scheduler synchronization;
- wake policy;
- multi-monitor warning behavior;
- integration hardening.

### Phase 5 — Distribution and release

- installer and clean upgrade path;
- update notification/staging;
- rollback;
- migration from Smart Sleep Shutdown registrations and data;
- release documentation;
- real-hardware QA;
- packaging and release gates.

Each phase must build, test, receive independent review, and commit cleanly before the next phase begins.

## 23. Acceptance criteria

The transformation is complete when all of the following are true:

1. A new user can install Hushward, complete the four-step onboarding, and understand the resulting policy without reading documentation.
2. The application supports shutdown, hibernate, sleep, lock, and warn-only actions without implicit fallbacks.
3. A user can configure routines by days, nightly window, inactivity, warning, wake policy, protections, and explicit alternatives.
4. `Esta noche` overrides are visibly temporary and expire without altering permanent routines.
5. Critical, temporary, and contextual protections are distinguishable and human-readable.
6. Detector failures block automatic power/session actions and are visibly explained.
7. Every automatic state-changing action has a visible cancellable warning and fresh final authorization check.
8. Keyboard/mouse activity or new protection evidence cancels warning and requires a fresh idle period.
9. Shutdown never uses forced termination.
10. Tray, main window, warning, and history derive from the same canonical runtime snapshot.
11. Wake tasks follow the configured routine and are disabled by default.
12. Desktop and laptop power transitions are handled without silent action substitution.
13. Configuration is versioned, atomic, recoverable, and migrates safely from the existing product.
14. Normal history remains local, concise, privacy-bounded, and retention-controlled.
15. Diagnostics are user-initiated, reviewable, and redacted.
16. The app has no account, cloud service, telemetry, arbitrary scripts, remote control, or permanent service.
17. Installer, update, rollback, and uninstall paths are documented and exercised.
18. Automated and manual safety/reliability gates pass on real Windows hardware.
19. Branding, copy, iconography, and UI consistently present Hushward as a calm night guardian rather than a generic shutdown utility.

## 24. Key design trade-offs

- **Conservative detector failure handling** may leave a PC running when evidence is unavailable. This is preferred over executing an unsafe power action.
- **Guided rules instead of arbitrary automation** reduce flexibility but keep behavior explainable, testable, and safe.
- **Visible warning for every automatic action** adds friction, including for lock, but preserves user trust and a consistent invariant.
- **Wake disabled by default** may miss actions while the PC sleeps, but avoids surprising device activation.
- **Multiple routines in the model with overlap prevention in UI** preserves future flexibility without accepting ambiguous execution priority.
- **Evolution rather than rewrite** carries migration complexity, but retains mature safety behavior and reduces regression risk.

## 25. Implementation planning constraint

The implementation plan derived from this design must be executable in small, verifiable tasks. It must identify exact files after re-inspecting the repository at the plan's target commit, preserve tests before moving behavior, and use no more than six independent specialist workstreams when parallelization is beneficial.
