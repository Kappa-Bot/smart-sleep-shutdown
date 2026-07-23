# Release gates

## Automated

`scripts\Verify-Release.ps1` is the single entry point. It verifies formatting,
Release build, all tests with coverage, architecture boundaries, privacy
boundaries, and self-contained publication.

## Distribution

- Build with an explicit semantic version.
- Verify every SHA-256 manifest entry.
- Keep the GitHub release in draft while artifacts are unsigned.
- Run update/rollback/uninstall lifecycle checks in a disposable user or VM.

## Manual

Complete every row in `MANUAL-QA.md`, including accessibility, wake, power,
migration, update, rollback, and uninstall. Critical or high findings block
release. Unsigned artifacts and incomplete real-hardware QA must be stated.
