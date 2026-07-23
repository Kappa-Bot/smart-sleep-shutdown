# Installation

Hushward is packaged per user with Velopack. It installs below `%LOCALAPPDATA%`,
requires no administrator rights, and keeps mutable data in
`%LOCALAPPDATA%\Hushward` outside the replaceable application directory.

## Build an installer

```powershell
.\scripts\Publish-Hushward.ps1
.\scripts\Package-Hushward.ps1 -Version 1.0.0
```

The release directory contains the installer, packages, and a SHA-256 manifest.
Current development artifacts are unsigned and must not be described as trusted
production releases.

Startup is a reversible current-user registration using the stable installed
launcher. Wake tasks are created only for routines that explicitly enable wake.
