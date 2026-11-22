# Tareas: ejecución en segundo plano y arranque con Windows

- [x] T001 [US1] Mantener sesiones y comprobaciones activas al ocultar la ventana. Añadir el icono con opciones de reapertura y salida (FR-001/002).
- [x] T002 [US1] Coordinar lanzamientos en la misma sesión y restaurar la ventana desde una apertura manual (FR-003).
- [x] T003 [US2] Añadir la casilla y el registro reversible de arranque de la cuenta. Inicializar el proceso oculto y mostrar avisos de controlador (FR-004/005).
- [x] T004 [US1, US2] Verificar la compilación y el ciclo de ventana y proceso. Consultar la evidencia local.
- [ ] T005 [US2] Verificar la casilla con dos inicios de sesión reales, uno activado y el siguiente desactivado (SC-003).
- [ ] T006 [US1] Repetir ocultación y reapertura con mandos físicos en un juego. Comprobar las ranuras y liberar las salidas al elegir **Salir** (SC-001/002).

## Evidencia local

- Pasaron 68 pruebas automatizadas. Se verificaron el paquete Release y las cuatro columnas de la lista de mandos.
- Una ventana WinForms inició oculta, restauró el mismo identificador de ventana en tres intentos y conservó una sesión con entrada simulada. **Salir** envió estado neutro y liberó esa salida.
- El ejecutable de publicación solicitó restaurar la ventana si se abrió manualmente como segunda instancia. Con `--background` salió sin solicitar que se mostrara.
- La prueba con una clave de registro aislada verificó el estado inicial, la escritura de la ruta entre comillas y la eliminación de la entrada al desactivar. El inicio de sesión real queda pendiente en T005.
