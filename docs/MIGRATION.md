# Migración

Hushward reconoce datos y registros de Smart Sleep Shutdown bajo
`%LOCALAPPDATA%\SmartSleepShutdown`, el valor Run legacy y
`SmartSleepShutdown-NightWake`.

1. Lee y caracteriza sin borrar el origen.
2. Convierte a configuración versionada y desactivada si algo requiere revisión.
3. Escribe y relee Hushward.
4. Crea y verifica startup/wake nuevos.
5. Solo después retira registros legacy propiedad del producto.
6. Conserva un recibo de migración local para recuperación.

Los lectores, constantes y fixtures legacy se conservan durante la ventana de
soporte. Una migración parcial o ambigua bloquea acciones automáticas.
