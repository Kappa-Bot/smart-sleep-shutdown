# Hushward

Hushward es un guardián nocturno local para Windows. Vive en el área de
notificación, espera una ventana configurada y solo actúa cuando la inactividad
y las protecciones permiten hacerlo con seguridad.

## Garantías

- Nunca ejecuta una acción automática sin aviso visible y cancelable.
- Teclado, ratón o una protección nueva cancelan el aviso.
- Revalida actividad, contexto y autorización justo antes de actuar.
- Un detector fallido o evidencia obsoleta bloquea acciones automáticas.
- Apagar usa exclusivamente `shutdown.exe /s /t 0`; nunca usa `/f`.
- No hay cuenta, nube, telemetría, servicio permanente ni inspección de contenido.

## Uso

1. Abre Hushward y completa los cuatro pasos.
2. Elige acción, horario, inactividad y, opcionalmente, activación desde suspensión.
3. Cierra la ventana. Hushward continúa en el área de notificación.
4. Usa `Pausar hoy` o desactiva la rutina cuando no quieras que actúe.

La suspensión del PC requiere que la rutina tenga wake habilitado y que Windows
permita temporizadores de activación. Hushward sincroniza `Hushward-NightWake`
con el horario real, usa `WakeToRun`, repite cada cinco minutos durante seis
horas y ejecuta `--scheduled-check` sin abrir la ventana.

## Desarrollo

Requiere Windows y .NET 10 SDK.

```powershell
dotnet run --project .\src\Hushward.App
.\scripts\Verify-Release.ps1
.\scripts\Install-Local.ps1
.\scripts\Package-Hushward.ps1 -Version 1.0.0
```

Los instaladores de desarrollo no están firmados. Los artefactos se generan en
`artifacts\releases` junto con un manifiesto SHA-256.

## Estructura

```text
src/Hushward.Core             politica determinista, sin Windows ni IO
src/Hushward.Application      casos de uso, puertos y snapshot canónico
src/Hushward.Infrastructure   WinAPI, detectores, energía, tareas y persistencia
src/Hushward.App              WPF, bandeja y composición
tests                         pruebas por capa
docs                          arquitectura, privacidad, UX, operación y release
```

Empieza por `AGENTS.md`, `docs/ARCHITECTURE.md` y `docs/AI_CONTEXT.md`.
