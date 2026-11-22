# Especificación del visor de botones Wii Remote

**Creada** 2026-11-22  
**Aplicación** Wii2Xbox para Windows

[Plan](plan.md) · [Tareas y evidencia](tasks.md)

## Escenarios y pruebas de usuario

### US1. Comprobar las entradas del mando físico (P1)

Como jugador, quiero ver qué botones reconoce Wii2Xbox antes de probar un juego.

**Prueba independiente**. Enciende un Wii Remote y pulsa A, B, 1, 2, −, +, HOME y cada dirección. Verifica que se ilumina la posición correspondiente incluso con un perfil que reasigna el botón.

### US2. Probar varios mandos a la vez (P1)

Como jugador, quiero pulsar botones en mandos diferentes y ver sus señales a la vez.

**Prueba independiente**. Mantén A en un mando y 2 en otro. Pulsa el mismo botón en los dos. Suelta uno o desconéctalo y confirma que el botón del otro continúa iluminado.

### US3. Leer el estado de la lista (P2)

Como jugador, quiero encontrar el número de Xbox en su columna y ver el estado de cada sesión por separado.

**Prueba independiente**. Con un Wii Remote asignado a Xbox 2, verifica que su estado básico es **Activo** y que la columna **Xbox** muestra **2**. Redimensiona la ventana y comprueba que las cuatro columnas llenan la lista.

### Casos límite

- Una pulsación breve se debe poder observar aunque el botón se libere entre repintados.
- Un Wii Remote desconectado deja de contribuir al visor. Las demás pulsaciones conservan su estado.
- Los botones Power y SYNC se usan para el encendido y la conexión. El visor muestra las entradas que el mando entrega a la aplicación.

## Requisitos

- **FR-001**. Mostrar una ilustración realista de un Wii Remote horizontal en la parte inferior con A, B, 1, 2, −, +, HOME y las cuatro direcciones. Representar el gatillo trasero B fuera de la vista frontal.
- **FR-002**. Iluminar los botones según la entrada física, de forma independiente del perfil y de la fila seleccionada.
- **FR-003**. Combinar las pulsaciones de todos los mandos que están enviando informes. Indicar cuándo varios mantienen el mismo botón y borrar al desconectar la contribución de ese mando.
- **FR-004**. Mantener la prueba de botones disponible para los mandos legibles mientras esperan el controlador Xbox, una ranura o el ratón IR.
- **FR-005**. Actualizar la lista automáticamente y repartir su ancho entre **Mando**, **Xbox**, **Estado**, **Perfil** y **Salida**. Usar **Activo** como estado básico y reservar la ranura para **Xbox**.
- **FR-006**. Colocar **Arrancar al iniciar Windows** alineado a la izquierda entre la lista y el visor.
- **FR-007**. Abrir el menú de salida desde la fila del mando y aplicar el cambio solo a ese mando.
- **FR-008**. Colocar **Invítame a un café** encima de la lista, alineado a la derecha.
- **FR-009**. Iluminar en azul el indicador del jugador mientras ese mando pulsa un botón. Mostrar a la vez los indicadores de varios mandos y asignar los indicadores libres a los mandos sin ranura Xbox.

## Criterios de éxito

- **SC-001**. Cada una de las once entradas se ilumina ante su pulsación y se apaga tras soltarla o desconectarla.
- **SC-002**. Dos mandos muestran botones distintos y compartidos en la misma vista. Las pulsaciones breves capturadas se mantienen visibles el tiempo suficiente para distinguirlas.
- **SC-003**. El visor funciona con redimensionado y sesiones en modos Xbox, ratón IR o solo lectura, y conserva la separación entre estado y ranura.
