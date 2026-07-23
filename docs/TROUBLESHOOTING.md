# Solución de problemas

## No aparece la ventana

Busca Hushward en los iconos ocultos del área de notificación y pulsa el icono.
Una segunda ejecución abre la instancia existente. `Salir` termina el proceso.

## No actuó mientras estaba suspendido

Confirma que la rutina tiene wake habilitado, existe `Hushward-NightWake`, su
propiedad `WakeToRun` está activa y el plan de energía permite temporizadores.
Algunos equipos no despiertan con batería, tapa cerrada o firmware restrictivo.
Sin wake, Windows no ejecuta ninguna aplicación durante suspensión.

## Se muestra protegido

Abre Protecciones. Un fallo/timeout o evidencia requerida desconocida bloquea
por seguridad. Audio, fullscreen, reunión, transferencia, render/build/backup,
sesión remota y Windows Update pueden proteger según la política.

## La configuración está dañada

Hushward intenta recuperar el backup y mantiene las acciones automáticas
bloqueadas si la recuperación no es fiable. Consulta `RECOVERY.md`.

## No arranca con Windows

Revisa Aplicaciones de inicio de Windows y el valor HKCU `Hushward`. Vuelve a
confirmar onboarding o reinstala por usuario. No necesita elevación.

## Updates

Una comprobación fallida degrada updates, no la política nocturna. La instalación
queda bloqueada durante aviso, acción, migración o recuperación.
