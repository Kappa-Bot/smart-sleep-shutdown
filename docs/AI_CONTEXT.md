# Contexto para agentes

Lee primero `AGENTS.md`, después `ARCHITECTURE.md`, `PRIVACY.md` y `UX_GUIDE.md`.

## Identidad

Hushward tiene un trabajo: ejecutar de noche una acción local explícita solo
cuando el usuario parece ausente y es seguro. Debe ser silencioso durante el
uso normal, claro al explicar bloqueos y muy visible antes de actuar.

## Invariantes

- Aviso visible para toda acción automática que cambie estado.
- Input, protección nueva o transición relevante cancelan y rearman por actividad.
- Revalidación fresca inmediatamente antes de ejecutar.
- Fallo, timeout, desconocido requerido o snapshot obsoleto bloquean.
- Sin `/f`, fallback implícito, telemetría, listener, scripts arbitrarios o contenido.
- Wake por rutina, desactivado por defecto y sincronizado dinámicamente.

## Dónde cambiar

- Política y estados: `src/Hushward.Core`.
- Orquestación y contratos: `src/Hushward.Application`.
- Windows, IO y red de updates: `src/Hushward.Infrastructure`.
- Presentación, recursos españoles y composición: `src/Hushward.App`.

No agregues otra fuente de estado junto a `NightRuntimeSnapshot`. Conserva los
lectores y constantes legacy durante la ventana de migración. No pruebes adapters
reales de energía en tests automatizados.

## Gates

`scripts\Verify-Release.ps1` es obligatorio. Hardware real, Narrator, DPI mixto,
wake, rollback y desinstalación se registran en `docs/quality/MANUAL-QA.md`; no
los declares aprobados sin evidencia.
