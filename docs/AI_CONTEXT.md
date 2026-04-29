# AI Context

Use this file as the first read after `AGENTS.md`.

## Product Intent

The app should feel like a native Windows tray utility:

- Open -> enable -> forget.
- No dashboard, charts, logs, or extra modes.
- Visible only when needed.
- Conservative about shutdown.

## Current Defaults

- Starts disabled.
- Start time: `01:00`.
- Precheck lead time: 30 minutes before configured start.
- Stop checking time: `06:00`.
- Idle threshold: 15 minutes.
- Warning countdown: 60 seconds.
- Context checks: enabled.
- Soft context override: after one hour of user idle, fullscreen/audio/high CPU/known-process blockers no longer veto shutdown.

## Important Behaviors

- Configured start can cross midnight, for example `23:00` stays active until `06:00`.
- Invalid start time disarms monitoring and shows `Usa hora HH:mm`.
- Temporary pause lasts until local midnight.
- Closing the window hides it to tray; tray Exit closes the process.
- During the 60 second warning, keyboard/mouse input cancels immediately. Context blockers are enforced at the final re-check so transient audio/CPU/fullscreen noise cannot restart warning loops forever.
- `DetectorFailure` is a hard blocker. Fullscreen app, known process, audio, and high CPU are soft blockers; this allows shutdown after the user falls asleep on a game home screen.
- Local install registers `SmartSleepShutdown-NightWake` in Task Scheduler at `00:30` with `WakeToRun`, plus the existing Run key. `--startup` is a background launch and must not activate an already-running primary window.
- UI language is Spanish; keep new UX strings Spanish.
- Installer uses `--exit` to request graceful shutdown before publishing.

## High-Value Future Improvements

- Add a tiny first-run toast explaining close-to-tray.
- Improve taskbar/app icon packaging with a checked-in `.ico`.
- Add signed installer/MSIX if distribution is needed.
- Add optional Windows notification API instead of only focusing the window.
- Add more integration tests around installed startup when a UI automation harness is available.

## Non-Goals

- Cloud sync.
- Accounts.
- Telemetry.
- Logs UI.
- Forced shutdown.
- Background services.
