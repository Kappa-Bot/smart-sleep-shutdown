# Updates and rollback

Hushward checks GitHub Releases only after a manual request or explicit opt-in.
No device identifier, settings, history, detector evidence, user name, or
telemetry is sent.

Updates are never forced. Installation is blocked while a warning or action is
active, and during migration, recovery, or unresolved degradation. Velopack
metadata verifies package integrity; production trust additionally requires a
real code-signing certificate.

Configuration and private history remain under `%LOCALAPPDATA%\Hushward` during
updates. Before release, exercise `scripts\Test-InstallLifecycle.ps1` in a
disposable Windows user or VM. The script deliberately refuses to run against a
normal profile. Rollback and both retain-data/remove-data uninstall choices are
manual release gates until Velopack exposes a stable non-interactive contract for
those choices.
