# Manual QA matrix

Record one row per environment. Never run automated tests with a real power
executor. A release cannot claim hardware validation until the evidence columns
are complete.

| Scenario | Preconditions | Steps | Expected result | Evidence | Tester | Date | Version |
|---|---|---|---|---|---|---|---|
| Windows 10 desktop, AC | Clean user | Install, enable routine, exercise warning | Tray remains available; warning cancels on input | Pending | Pending | Pending | Pending |
| Windows 11 laptop, AC/battery transition | Battery available | Start on AC, unplug during monitoring and warning | Policy refreshes; no implicit action substitution | Pending | Pending | Pending | Pending |
| Hibernation available/unavailable | Toggle OS capability | Evaluate explicit hibernate routine | Eligible only when supported; failure is safe | Pending | Pending | Pending | Pending |
| Suspend/resume and wake success | Wake routine enabled | Suspend before precheck | Scheduled task wakes and evaluates without opening shell | Pending | Pending | Pending | Pending |
| Wake unavailable/disabled | Wake timers disabled | Suspend before precheck | Health shows degradation; no silent action | Pending | Pending | Pending | Pending |
| Lock/unlock | Active routine | Lock then unlock during candidate/warning | Relevant transition cancels and requires fresh idle | Pending | Pending | Pending | Pending |
| Remote desktop | RDP session | Connect/disconnect during monitoring | Remote activity protects; unknown state blocks | Pending | Pending | Pending | Pending |
| One/two monitors | Mixed layouts | Open shell, tray, warning on each active monitor | Entire surface visible on active work area | Pending | Pending | Pending | Pending |
| DPI 100/125/150/200% | Per-monitor DPI | Move all surfaces between monitors | No clipping, overlap, or unreadable text | Pending | Pending | Pending | Pending |
| Reduced motion | Windows animation disabled | Open and operate all surfaces | No nonessential animation | Pending | Pending | Pending | Pending |
| Keyboard-only and screen reader | Narrator enabled | Complete onboarding, tray, warning cancel | Logical focus, names, live countdown announcements | Pending | Pending | Pending | Pending |
| Fullscreen media and active audio | Media playing | Reach eligibility before/after stale protection limit | Protected while fresh; policy follows explicit expiry | Pending | Pending | Pending | Pending |
| Sustained transfer/render/build/backup | Real workload | Reach eligibility | Protected evidence blocks; unknown detector blocks | Pending | Pending | Pending | Pending |
| Windows Update | Update active | Reach eligibility | Update protection blocks state-changing action | Pending | Pending | Pending | Pending |
| Clean install | New disposable user | Install package | Per-user install; startup healthy; no elevation | Pending | Pending | Pending | Pending |
| Legacy upgrade | Smart Sleep Shutdown state present | Install and review migration | Data is mapped safely; legacy removed only after health checks | Pending | Pending | Pending | Pending |
| Corrupt config recovery | Corrupt live config, valid backup | Launch | Backup restored; recovery disclosed; automatic action blocked if unresolved | Pending | Pending | Pending | Pending |
| Update and rollback | Disposable VM/user | Run lifecycle script, then rollback | Data retained; registrations healthy | Pending | Pending | Pending | Pending |
| Uninstall retain/remove | Disposable VM/user | Test both uninstall choices | Explicit choice honored; registrations removed | Pending | Pending | Pending | Pending |
