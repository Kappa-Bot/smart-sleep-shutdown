# Recuperación

La configuración se escribe de forma atómica y mantiene backup. Si el archivo
principal es inválido, Hushward intenta el backup. Si ambos fallan, entra en
modo seguro: no ejecuta acciones automáticas y muestra recuperación requerida.

No edites archivos mientras Hushward está abierto. Para soporte:

1. Pausa o desactiva Hushward.
2. Conserva `%LOCALAPPDATA%\Hushward`.
3. Genera diagnóstico local y revísalo antes de compartir.
4. Restaura valores seguros o reinstala sin eliminar datos.

La desinstalación debe retirar startup y tareas. Los datos solo se eliminan tras
una elección explícita del usuario.
