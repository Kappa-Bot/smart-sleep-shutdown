# Checklist de release

Estado automatizado a 2026-07-23. `PENDIENTE` significa que no debe afirmarse
como validado hasta adjuntar evidencia real.

| # | Criterio | Estado | Evidencia |
|---|---|---|---|
| 1 | Instalación y onboarding de cuatro pasos comprensible | PARCIAL | `OnboardingViewModelTests`; QA manual pendiente |
| 2 | Shutdown, hibernate, sleep, lock y warn-only sin fallback | PASS | `WindowsNightActionExecutorTests`; `CoordinatedShutdownExecutorTests` |
| 3 | Rutina configura días, ventana, idle, aviso, wake y protecciones | PARCIAL | Core routine/policy tests; edición manual completa pendiente |
| 4 | Override Esta noche temporal | PASS | `TonightOverrideResolverTests`; `TonightViewModelTests` |
| 5 | Protecciones críticas, temporales y contextuales distinguibles | PASS | protection policy/presenter tests |
| 6 | Fallo de detector bloquea y se explica | PASS | `ProtectionCoordinatorTests`; reason presenter tests |
| 7 | Toda acción automática avisa y revalida | PASS | warning/action/coordinated executor tests |
| 8 | Input/protección cancela y exige nuevo idle | PASS | warning and decision transition tests |
| 9 | Shutdown nunca fuerza | PASS | `WindowsNightActionExecutorTests`; privacy/architecture gates |
| 10 | Superficies consumen snapshot canónico | PASS | `ShellViewModelTests`; tray/warning tests |
| 11 | Wake sigue rutina y está off por defecto | PASS | schedule sync/builder tests; QA wake pendiente |
| 12 | Transiciones de energía sin sustitución silenciosa | PASS | power adapter and policy tests |
| 13 | Config versionada, atómica, recuperable y migrable | PASS | configuration/migration/store tests |
| 14 | Historial local, breve y con retención | PASS | history tests; `Verify-Privacy.ps1` |
| 15 | Diagnóstico manual, revisable y redactado | PASS | diagnostic bundle/coordinator tests |
| 16 | Sin cuenta, nube, telemetría, scripts o control remoto | PASS | `Verify-Privacy.ps1`; `PRIVACY.md` |
| 17 | Instalación/update/rollback/uninstall documentados y ejercidos | PENDIENTE | scripts/docs listos; VM lifecycle pendiente |
| 18 | Gates automáticos y hardware Windows real | PENDIENTE | `Verify-Release.ps1`; `MANUAL-QA.md` pendiente |
| 19 | Branding Hushward consistente y calmado | PARCIAL | resource/brand tests; revisión visual DPI pendiente |
