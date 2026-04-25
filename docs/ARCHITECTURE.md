# Architecture

Smart Sleep Shutdown is a small WPF utility with strict separation between policy, Windows adapters, and UI orchestration.

## Core

`src/SmartSleepShutdown.Core` owns safe shutdown policy:

- `SleepShutdownSettings`: enabled state, start time, idle threshold, warning duration, context checks.
- `IdleSnapshot`: current idle duration and input-reset signal.
- `ContextSnapshot`: blocking context list.
- `DecisionEngine`: state machine.
- `MonitoringSchedule`: active window and polling cadence.

Core must stay deterministic and testable.

## Infrastructure

`src/SmartSleepShutdown.Infrastructure` owns side effects:

- `Win32IdleDetector`: `GetLastInputInfo`.
- `ForegroundFullscreenContextProbe`: fullscreen foreground window detection.
- `AudioPlayingContextProbe`: output peak meter.
- `CpuUsageContextProbe`: sustained CPU blocker.
- `KnownProcessContextProbe`: known app blocker.
- `WindowsShutdownExecutor`: fixed `shutdown.exe /s /t 0`.
- `StartupRegistration`: HKCU Run key command generation.

Infrastructure should fail safe. Unexpected detector failures become blockers.

## App

`src/SmartSleepShutdown.App` owns:

- WPF window and tray menu.
- Single-instance coordination.
- Settings persistence.
- Monitoring loop.
- Countdown notification and cancellation.

The app starts hidden with `--startup`, opens the existing instance on second launch, and supports `--exit` for graceful installer shutdown.

## Runtime Flow

1. App starts and enforces single instance.
2. If enabled, monitor sleeps until the next precheck window.
3. During the active window, it samples idle/context.
4. If eligible, it enters warning and shows countdown.
5. Any input or blocker cancels.
6. At countdown expiry, it re-checks eligibility.
7. Only then it runs shutdown.
