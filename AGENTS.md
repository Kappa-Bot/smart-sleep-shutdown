# Agent Guide

This repository is optimized for future AI agents. Keep the app focused: one Windows utility that shuts down only when it is late, idle, and safe.

## Safety Invariants

- Never call shutdown silently.
- Always show the 60 second warning before shutdown.
- Cancel warning on keyboard or mouse activity.
- Re-check idle and blocking context immediately before shutdown.
- Do not restart the warning for transient context blockers during countdown; final re-check blocks shutdown if the blocker persists.
- Soft context blockers, including fullscreen/game/audio/high CPU/known process, must not veto shutdown after one hour of user idle. `DetectorFailure` remains a hard blocker.
- Never use `/f`; the fixed command is `shutdown.exe /s /t 0`.
- Detector failures block shutdown, except expected optional-audio absence.
- Do not restart warning loops after cancel until idle has reset below threshold.

## Architecture

- `Hushward.Core`: pure models and logic. No WPF, WinAPI, registry, process APIs, or file I/O.
- `Hushward.Infrastructure`: Windows adapters: WinAPI idle, context probes, startup registration, shutdown command.
- `Hushward.App`: WPF shell, tray icon, settings persistence, orchestration.
- `tests`: xUnit tests split by project.

## Verification

Run before claiming completion:

```powershell
.\scripts\Verify-Release.ps1
```

Never mark real power, wake, mixed-DPI, Narrator, rollback, or uninstall gates
complete without recorded evidence in `docs/quality/MANUAL-QA.md`.

Manual startup check:

```powershell
$exe = Join-Path $env:LOCALAPPDATA 'Hushward\Hushward.exe'
Start-Process -FilePath $exe -ArgumentList '--startup'
Start-Sleep -Seconds 5
Get-Process -Name Hushward
```

## Coding Rules

- Add or update tests for every behavior change.
- Keep UI calm and tray-first. Every surface must improve safety, trust, or recovery.
- Keep UI language Spanish unless user explicitly asks otherwise.
- Avoid background loops. Prefer scheduled one-shot delays and cancellation.
- Wake tasks follow enabled routines, use `WakeToRun` and `--scheduled-check`, and remain disabled by default; Run key alone cannot wake a suspended PC.
- `--scheduled-check` must signal the existing primary instance to restart monitoring/evaluate now, but must not open the window.
- Keep Windows awake during the warning countdown; otherwise it may sleep again before `shutdown.exe`.
- Keep tray behavior predictable: close hides, Exit exits, second launch opens existing instance.
- Preserve project isolation under this root folder.

## UX Rules

- Read `docs/UX_GUIDE.md` before UI changes.
- Preserve tray-first behavior.
- Add hints only when they prevent user confusion or unsafe operation.
