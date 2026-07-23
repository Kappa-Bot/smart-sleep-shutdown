# Arquitectura

## Límites

`Hushward.Core` contiene rutinas, ventanas nocturnas, protecciones, advertencias
y `NightPolicyEngine`. Es determinista y no referencia WPF, Windows, procesos,
registro ni archivos.

`Hushward.Application` contiene coordinadores y puertos. `NightRuntimeSnapshot`
es la fuente canónica de estado para ventana, bandeja y advertencia. Los
coordinadores serializan evaluación final, aviso, acción, historial,
diagnósticos, schedule y updates.

`Hushward.Infrastructure` implementa WinAPI, sesión, energía, audio, procesos,
carga sostenida, Windows Update, Task Scheduler, startup HKCU, persistencia
atómica, migración, diagnósticos y Velopack. Los fallos se convierten en
resultados seguros; no se inventan alternativas.

`Hushward.App` compone dependencias y presenta WPF. `NightMonitorController` es
el controlador de compatibilidad de la rutina activa; no es un ViewModel. Los
ViewModels traducen el snapshot y delegan cambios, sin crear adapters Windows.

## Flujo automático

1. El schedule despierta o señala la instancia primaria.
2. El controlador espera sin sondeo intensivo hasta la ventana previa.
3. Core evalúa hora, inactividad, sesión, energía y protecciones clasificadas.
4. Si procede, `WarningCoordinator` publica advertencia y el sistema se mantiene despierto.
5. Input o evidencia relevante invalida la secuencia y exige nueva inactividad.
6. Al expirar, se obtiene evidencia fresca con antigüedad máxima de cinco segundos.
7. `ActionCoordinator` ejecuta una sola vez la acción explícitamente autorizada.

## Wake

`DesiredWakeSchedule` deriva cada trigger del horario de rutina, treinta minutos
antes de la hora inicial. Wake está desactivado por defecto. La tarea usa
`WakeToRun`, `StartWhenAvailable`, batería permitida, repetición PT5M/PT6H y
`--scheduled-check`. Solo se elimina la tarea legacy después de verificar la
nueva.

## Persistencia

La configuración versionada usa escritura temporal, reemplazo atómico y backup.
El estado compatible de la rutina activa migra `settings.json` a
`runtime-settings.json` y recupera el backup. Historial y diagnósticos almacenan
códigos clasificados, nunca contenido del usuario.

## Dependencias

Ejecuta `scripts\Verify-Architecture.ps1`. Cualquier cambio de límites, adapter
de acción o dato persistido requiere test de regresión y revisión de privacidad.
