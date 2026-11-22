# Especificación: ejecución en segundo plano y arranque con Windows

**Creada** 2026-11-22  
**Alcance** Wii2Xbox para Windows 10/11 x64  
**Funcionalidad base** [Conversión Wii a Xbox](../001-wii-xbox-low-latency/spec.md)

## Escenarios y pruebas de usuario

### US1. Mantener activos los mandos al cerrar la ventana (P1)

Como jugador, quiero quitar la ventana del escritorio y seguir usando los mandos.

**Prueba independiente**. Con una sesión activa, cierra la ventana, comprueba la entrada en un juego y vuelve a abrir Wii2Xbox desde el icono del área de notificación.

1. Al cerrar la ventana, desaparece del escritorio y el proceso sigue funcionando con las mismas sesiones.
2. **Abrir Wii2Xbox** en el icono o abrir de nuevo la aplicación restaura la misma ventana.
3. **Salir** en el icono termina la aplicación, neutraliza las entradas y libera las salidas. Cerrar sesión o apagar Windows también termina el programa.

### US2. Arrancar al iniciar Windows (P1)

Como jugador, quiero poder iniciar Wii2Xbox con mi sesión de Windows y tener los mandos disponibles sin abrirlo a mano.

**Prueba independiente**. Marca **Arrancar al iniciar Windows**, vuelve a iniciar sesión y comprueba que aparece el icono sin mostrarse la ventana. Desmarca la casilla y repite el inicio de sesión.

1. La casilla empieza desmarcada en una cuenta nueva. Cambiarla aplica la preferencia al siguiente inicio de sesión.
2. Con la casilla marcada, la aplicación arranca oculta, comprueba el controlador Xbox y busca los Wii Remote disponibles.
3. Con la casilla desmarcada, Windows deja de iniciar Wii2Xbox automáticamente.

### Casos límite

- Un segundo arranque manual debe restaurar la ventana de la instancia en ejecución. Un segundo arranque automático conserva oculto el proceso que ya esté en segundo plano.
- Si el controlador Xbox falla al arrancar en segundo plano, la aplicación solicita una notificación y mantiene la explicación disponible al abrir la ventana.
- Si Windows deniega un cambio de inicio, Wii2Xbox conserva el estado anterior de la casilla y muestra el error.

## Requisitos

- **FR-001**. Mantener detección, entradas y reconexión de mandos al ocultar la ventana.
- **FR-002**. Ofrecer **Abrir Wii2Xbox** y **Salir** desde el área de notificación. La salida explícita libera las entradas y las salidas.
- **FR-003**. Limitar la ejecución a una instancia por sesión de Windows y reutilizar su ventana al abrir la aplicación de nuevo.
- **FR-004**. Mantener **Arrancar al iniciar Windows** desactivado por defecto. Guardar y aplicar la preferencia solo para la cuenta que la cambió, sin elevar la aplicación.
- **FR-005**. Arrancar en segundo plano al iniciar sesión si está habilitado, con las mismas comprobaciones y el mismo comportamiento de detección que al abrir la aplicación manualmente.

## Criterios de éxito

- **SC-001**. Tras cerrar y volver a abrir la ventana en tres intentos, se conserva el mismo proceso y las sesiones activas reciben entrada sin reasignar ranuras.
- **SC-002**. Un segundo arranque manual restaura la ventana. **Salir** elimina el icono y termina el proceso en condiciones normales en hasta tres segundos.
- **SC-003**. Tras dos inicios de sesión, uno con la casilla marcada y otro tras desmarcarla, Wii2Xbox arranca solo en el primero. Su ventana permanece oculta hasta que el usuario la abre.

## Supuestos

- El programa residente pertenece a la sesión interactiva del usuario, donde Windows presenta la ventana, el icono y el cursor. El arranque automático ocurre al iniciar sesión.
- La ruta del ejecutable se fija al marcar la opción. Si se mueve Wii2Xbox a otra carpeta, se puede actualizar desmarcando y volviendo a marcar la casilla.
