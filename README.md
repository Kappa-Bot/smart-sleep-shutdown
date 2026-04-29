# Smart Sleep Shutdown

Windows WPF utility that shuts down the PC only when it is late enough, the user is idle, and no blocking activity is detected.

## Behavior

- Default start time: `01:00`
- Default idle threshold: `15` minutes
- Default warning: `60` seconds
- App starts disabled
- Shutdown command: `shutdown.exe /s /t 0`

The app never shuts down silently. It shows `PC will shut down in 60 seconds`, starts a countdown, cancels on keyboard or mouse input, and re-checks activity/context before executing shutdown. Transient context noise during the countdown does not restart the countdown; a blocker must still be present at the final re-check to stop shutdown.

## Tray Menu

The app stays available from the Windows notification area in the lower-right corner.

Left-click the tray icon to open the app.

Right-click the tray icon to:

- see whether Smart Sleep Shutdown is `ON`, `OFF`, or paused until tomorrow
- open the main window
- activate or deactivate monitoring
- pause monitoring until the next day
- exit the app

Closing the window hides it to the tray and shows a short hint so the user knows it is still running. Use `Salir` from the tray menu to close the app.

The tray icon has a status badge:

- green: active
- amber pause: suspended for today
- gray: off

Settings are saved locally, including enabled/off state, start time, idle threshold, context checks, and pause-until-tomorrow state.

## Smart Schedule

The app avoids constant polling.

- before the precheck window, it sleeps
- the precheck window starts 30 minutes before the configured start time
- from start time to `06:00`, it checks intelligently
- while clearly active, it waits longer between checks
- near the idle threshold or during countdown, it checks faster
- the local installer registers a daily `SmartSleepShutdown-NightWake` scheduled task at `00:30` with `WakeToRun`, so Windows can wake the PC from sleep before the shutdown window

The start time can cross midnight. For example, `23:00` means active from `23:00` until `06:00`.

## Context Checks

When enabled, shutdown is delayed by soft context blockers:

- fullscreen foreground window
- audio output activity
- sustained high CPU usage
- known running apps: Teams, Zoom, OBS, Steam, Visual Studio, VS Code, PowerPoint

Soft blockers prevent shutdown during the first hour of idle. After one hour idle, they no longer veto shutdown; this handles cases like falling asleep on a fullscreen game home screen. Detector failure is the hard blocker and still prevents shutdown.

## Project Layout

```text
smart-sleep-shutdown/
  SmartSleepShutdown.sln
  README.md
  src/
    SmartSleepShutdown.App/
    SmartSleepShutdown.Core/
    SmartSleepShutdown.Infrastructure/
  tests/
    SmartSleepShutdown.App.Tests/
    SmartSleepShutdown.Core.Tests/
    SmartSleepShutdown.Infrastructure.Tests/
```

## Build

```powershell
dotnet build
```

## Test

```powershell
dotnet test
```

## Run

```powershell
dotnet run --project src/SmartSleepShutdown.App
```

## Local Install

```powershell
.\scripts\Install-Local.ps1
```

This publishes to `%LOCALAPPDATA%\SmartSleepShutdown` and registers:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SmartSleepShutdown
Task Scheduler\SmartSleepShutdown-NightWake
```

Startup launches hidden with `--startup`; control stays in the Windows tray.
The wake task runs daily at `00:30`, wakes the computer when Windows wake timers are allowed, and launches hidden with `--startup`. The installer also attempts to enable wake timers for the current Windows power plan.
The installer asks the currently installed process to exit gracefully before replacing files.

## Agent-Friendly Docs

Future maintainers and AI agents should read:

- `AGENTS.md`
- `docs/ARCHITECTURE.md`
- `docs/AI_CONTEXT.md`
- `docs/UX_GUIDE.md`
