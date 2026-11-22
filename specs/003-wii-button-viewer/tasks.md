# Tareas del visor

- [x] T001 Capturar botones físicos y flancos por sesión con pruebas de pulsación breve.
- [x] T002 Dibujar y agregar el estado de las sesiones en el visor horizontal.
- [x] T003 Integrar el visor, situar la casilla encima y permitir lectura cuando una salida no esté disponible.
- [x] T004 Separar el estado y la ranura. Retirar el botón de búsqueda y verificar el reparto de ancho de las columnas.
- [x] T005 Compilar Release, probar la vista con sesiones simuladas y verificar el ZIP.
- [ ] T006 Verificar con dos Wii Remote físicos la lectura simultánea de los botones en el visor.
- [x] T007 Integrar la ilustración y los LED azules por mando. Mover el menú **Salida** a cada fila y el botón de café a la esquina superior derecha. Verificar filas sin identificador, menú por ratón y teclado y posición de los controles.

Las pruebas de WinForms confirmaron la casilla alineada debajo de la lista, la separación entre **Activo** y **Xbox 2**, y el ancho exacto de la lista a 690, 850 y 1200 píxeles y con barra de desplazamiento. Dos sesiones simuladas mostraron botones diferentes y un botón compartido. Al cerrar una, la otra mantuvo su entrada. Las pruebas automatizadas verificaron pulsaciones breves y un lector activo sin salida Xbox.

La notificación de dispositivos de Windows desencadenó una nueva enumeración en la prueba de la ventana. Pasaron las 70 pruebas ordinarias y las dos integraciones con una y cuatro salidas XInput reales y entrada Wii simulada.

La revisión del visor verificó en claro y oscuro dos LED encendidos simultáneamente, la retirada del LED de un mando al desconectarlo y el apagado al soltar todos los botones. El menú **Salida** cambió la fila pulsada aunque otra estuviera seleccionada. Las filas sintéticas sin identificador se pudieron actualizar sin lanzar la excepción de clave nula.
