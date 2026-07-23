# Privacy review

Hushward is local-first. It has no account, backend, telemetry, remote control,
listener, content inspection, screenshots, clipboard access, title capture, or
command execution surface.

Persisted history contains bounded policy outcomes and reason codes, not content.
Diagnostics are local, explicitly exported, redacted, and omit user content.
Detector contracts expose classified evidence only. Update network access is
limited to Velopack against the configured GitHub Releases catalog after manual
request or explicit opt-in.

Run `scripts\Verify-Privacy.ps1` to enforce persisted-field and network
boundaries. Review any allow-list change as a privacy-impacting change.
