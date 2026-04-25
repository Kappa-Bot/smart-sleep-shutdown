# Agent Guide

This repository is optimized for future AI agents. Keep the app focused: one Windows utility that shuts down only when it is late, idle, and safe.

## Safety Invariants

- Never call shutdown silently.
- Always show the 60 second warning before shutdown.
- Cancel warning on keyboard or mouse activity.
- Re-check idle and blocking context immediately before shutdown.
- Never use `/f`; the fixed command is `shutdown.exe /s /t 0`.
- Detector failures block shutdown, except expected optional-audio absence.
- Do not restart warning loops after cancel until idle has reset below threshold.

## Architecture

- `SmartSleepShutdown.Core`: pure models and logic. No WPF, WinAPI, registry, process APIs, or file I/O.
- `SmartSleepShutdown.Infrastructure`: Windows adapters: WinAPI idle, context probes, startup registration, shutdown command.
- `SmartSleepShutdown.App`: WPF shell, tray icon, settings persistence, orchestration.
- `tests`: xUnit tests split by project.

## Verification

Run before claiming completion:

```powershell
dotnet format .\SmartSleepShutdown.sln --verify-no-changes
dotnet build .\SmartSleepShutdown.sln
dotnet test .\SmartSleepShutdown.sln
.\scripts\Install-Local.ps1
```

Manual startup check:

```powershell
$exe = Join-Path $env:LOCALAPPDATA 'SmartSleepShutdown\SmartSleepShutdown.exe'
Start-Process -FilePath $exe -ArgumentList '--startup'
Start-Sleep -Seconds 5
Get-Process -Name SmartSleepShutdown
```

## Coding Rules

- Add or update tests for every behavior change.
- Keep UI minimal: status, ON/OFF, start time, idle threshold, context checks, countdown cancel.
- Avoid background loops. Prefer scheduled one-shot delays and cancellation.
- Keep tray behavior predictable: close hides, Exit exits, second launch opens existing instance.
- Preserve project isolation under this root folder.
