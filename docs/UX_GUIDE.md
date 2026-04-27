# UX Guide

Smart Sleep Shutdown is a tray-first Windows utility. UX goal: user trusts it, understands it, then forgets it.

## Language

- UI language is Spanish.
- Keep strings short and concrete.
- Avoid technical labels when user action is clearer.
- Prefer `Pausar hoy`, `Ver ventana`, `Desactivado`, `Vigilando`.

## Window

Required first-glance answers:

- Is shutdown guard active?
- What is it waiting for?
- How do I cancel a shutdown?
- Where did it go after close?

Do not add dashboards, charts, logs, accounts, telemetry, or multi-page navigation.

## Tray

Tray is primary home after setup.

- Left click opens window.
- Right click shows actions.
- Close hides to tray and shows short hint.
- Exit menu item is only normal app exit path.

Status badge meanings:

- green: active
- amber pause: paused until tomorrow
- gray: off

## Hints

Hints should be temporary or compact. Use them only for safety-critical or discoverability-critical behavior:

- closing keeps app active in tray
- shutdown warning cancels on input
- start time format is `HH:mm`
- context checks prevent wrong shutdowns

## Visual Style

- Calm Windows utility.
- Muted blue-gray shell, white work surfaces, green active state, amber warning state.
- Cards only for actual grouped controls or status.
- No decorative blobs, charts, or marketing layout.
