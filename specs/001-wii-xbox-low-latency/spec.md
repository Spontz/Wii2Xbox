# Feature Specification: Wii2Xbox con latencia mínima

**Feature Directory**: `specs/001-wii-xbox-low-latency`

**Created**: 2026-11-22

**Status**: Implementación disponible y validación parcial. Consultar [evidencia y pendientes](acceptance.md).

**Input**: Aplicación Windows para conectar uno o varios mandos Wii por Bluetooth y convertirlos en mandos Xbox independientes para jugar. Prioridad explícita: lograr la latencia mínima. Inicializar Spec Kit y especificar el desarrollo completo.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Jugar con respuesta inmediata (Priority: P1)

Como jugador quiero usar el Wii Remote como mando Xbox con la menor demora y variabilidad posibles.

**Why this priority**: Es el objetivo esencial y la prioridad explícita del usuario.

**Independent Test**: Un mando físico, una salida virtual y un juego compatible; verificar botones y medir la respuesta con un consumidor independiente de la salida.

**Acceptance Scenarios**:

1. **Given** el controlador Xbox disponible, **When** Wii2Xbox encuentra un Wii Remote al abrirse o al conectarlo, **Then** crea automáticamente una salida Xbox y cada botón produce su entrada esperada.
2. **Given** conversión activa, **When** llegan pulsaciones rápidas, **Then** los cambios salen en orden, sin demoras intencionales ni pulsaciones inventadas.
3. **Given** juego activo, **When** minimizo o uso la interfaz, **Then** la entrada continúa cumpliendo el presupuesto de latencia.
4. **Given** un botón sostenido, **When** cambio de salida o salgo de Wii2Xbox desde el área de notificación, **Then** se neutraliza y libera la salida anterior.

### User Story 2 - Conectar por Bluetooth (Priority: P1)

Como jugador quiero buscar y conectar mis mandos mediante un flujo guiado que explique cada estado y fallo.

**Why this priority**: La conexión fiable es imprescindible para jugar.

**Independent Test**: Mando sin asociar, sincronización, descubrimiento, conexión, cancelación y ensayo sin radio.

**Acceptance Scenarios**:

1. **Given** una radio compatible, **When** pulso **Añadir mando…**, **Then** la app busca automáticamente y muestra los mandos encontrados conforme aparecen.
2. **Given** radio ausente/apagada, **When** intento conectar, **Then** se muestra causa y acción concreta, sin estado listo falso.
3. **Given** mando conocido disponible de nuevo, **When** reconecto, **Then** recupero una sola sesión sin duplicados.
4. **Given** búsqueda activa, **When** cierro **Añadir mando**, **Then** termina en ≤5 segundos y otras sesiones continúan.

### User Story 3 - Hasta cuatro jugadores (Priority: P1)

Como grupo queremos que cada Wii controle un jugador Xbox distinto.

**Why this priority**: El objetivo incluye múltiples mandos y salidas independientes.

**Independent Test**: Cuatro mandos con combinaciones diferentes; retirar uno y verificar aislamiento del resto.

**Acceptance Scenarios**:

1. **Given** cuatro mandos disponibles, **When** activo conversión, **Then** hay cuatro salidas independientes con asociación física/virtual visible.
2. **Given** límite de salidas ocupado, **When** añado otro, **Then** se explica el límite sin reemplazar a los jugadores.
3. **Given** otros controles Xbox en Windows, **When** activo la conversión y XInput asigna una ranura, **Then** la columna Xbox muestra ese número y se enciende únicamente el LED correspondiente en el Wii Remote.
4. **Given** varias sesiones, **When** una falla, **Then** solo esa salida se neutraliza.

### User Story 4 - Perfiles y Nunchuk (Priority: P2)

Como jugador quiero configurar cada mando para diferentes juegos, horizontalmente o con Nunchuk.

**Why this priority**: Los perfiles adaptan juegos y el Nunchuk aporta entrada analógica.

**Independent Test**: Dos perfiles distintos en dos mandos; guardar, reiniciar y verificar orientación, asignaciones y stick calibrado.

**Acceptance Scenarios**:

1. **Given** perfil horizontal, **When** pulso cruceta, **Then** el juego recibe la dirección visualmente correcta.
2. **Given** Nunchuk compatible, **When** muevo stick o pulso C/Z, **Then** obtengo ejes y entradas asignadas; el centro calibrado no deriva.
3. **Given** editor, **When** cambio asignación, **Then** puedo elegir botones Xbox, cruceta, hombros, pulsación de sticks, gatillos digitales o desactivar.
4. **Given** botones activos, **When** cambio perfil, **Then** se neutraliza el estado anterior antes de aplicar el nuevo.
5. **Given** perfil inválido o accesorio desconocido, **When** lo cargo/conecto, **Then** aparece un error útil sin sustituir configuración válida ni inventar entradas.

### User Story 5 - Medir y optimizar latencia (Priority: P1)

Como jugador quiero que el rendimiento medido distinga la demora añadida por Wii2Xbox, Bluetooth y la respuesta del juego.

**Why this priority**: La latencia mínima necesita evidencia y comparaciones correctas.

**Independent Test**: Ensayos reproducibles de una y cuatro entradas, exportación de percentiles, observador independiente y medición física.

**Acceptance Scenarios**:

1. **Given** una sesión en el entorno de pruebas, **When** se captura el procesamiento, **Then** el informe indica cadencia, ventana, muestra y alcance de la medición.
2. **Given** un ensayo de rendimiento, **When** se exporta su resultado, **Then** incluye mediana, p95, p99, máximo, duración, muestras, versión, hardware, modo y condiciones.
3. **Given** diagnóstico apagado, **When** juego, **Then** no hay registros por informe ni esperas añadidas para medir.
4. **Given** optimización candidata, **When** comparo con base equivalente, **Then** se conserva si reduce demora/variabilidad sin perder transiciones.

### User Story 6 - Instalar y recuperarse de fallos (Priority: P2)

Como usuario quiero ejecutar sin SDK y recuperar desconexiones sin botones atascados.

**Why this priority**: Hace utilizable el producto fuera del entorno de desarrollo.

**Independent Test**: Equipo limpio, controlador ausente, apagado del mando, suspensión/reanudación y cierre con botón sostenido.

**Acceptance Scenarios**:

1. **Given** Windows sin SDK, **When** ejecuto el paquete, **Then** abre y comprueba requisitos.
2. **Given** Windows sin el controlador Xbox y sin Internet, **When** pulso **Instalar controlador Xbox** y autorizo la instalación, **Then** Wii2Xbox instala el controlador integrado, comprueba el resultado e indica si Windows requiere un reinicio.
3. **Given** botón sostenido, **When** retiro pilas o apago radio, **Then** se neutraliza la salida en ≤1 segundo desde la pérdida observable de comunicación.
4. **Given** suspensión/reanudación, **When** vuelve Bluetooth, **Then** puedo recuperar sin salidas duplicadas ni afectar dispositivos ajenos.
5. **Given** la aplicación activa, **When** elijo **Salir** desde el área de notificación, **Then** cesan las búsquedas, los lectores y las salidas. El comportamiento al cerrar solo la ventana figura en la [especificación de segundo plano](../002-background-startup/spec.md).
6. **Given** que Windows cancela la elevación o el instalador devuelve un error, **When** termina el intento, **Then** Wii2Xbox informa de lo ocurrido, permite reintentar cuando corresponda y mantiene disponible **Ratón IR**.

### Edge Cases

- Apagado durante búsqueda/negociación/configuración; doble clic de conectar; lista que cambia durante una acción.
- Cuatro salidas más otros controles XInput; índices reasignados por Windows; identidad física frente a ranura.
- Informes incompletos/desconocidos; estado y respuestas de escritura intercalados con botones.
- Nunchuk cifrado, no inicializado, retirado en caliente o clon incompatible.
- Direcciones opuestas, varias entradas al mismo destino, perfil corrupto/vacío/de versión futura.
- RVL-CNT-01 frente a RVL-CNT-01-TR; clones no verificados; batería baja e interferencia de radio.
- Lectura bloqueada al cerrar; radio silenciosa sin notificación; acceso HID denegado; reinicio del controlador virtual.
- Juego con restricciones propias de mandos virtuales; no se promete compatibilidad universal.
- Una salida comprobada por Windows puede seguir sin aparecer en el navegador. La entrada de un juego y la detección web se verifican en sus propios entornos.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Ejecutar en Windows 10/11 x64 con interfaz en español, sin consola obligatoria.
- **FR-002**: Buscar mandos automáticamente durante el uso del asistente. Pausar la búsqueda para asociar el mando elegido y cancelarla al cerrar. Distinguir radio ausente, asociación fallida y acceso denegado.
- **FR-003**: Enumerar mandos ya conectados y evitar duplicados por identidad física.
- **FR-004**: Activar automáticamente los Wii Remote disponibles, con hasta cuatro salidas Xbox 360 independientes para juegos XInput. Usar Xbox como salida inicial y aplicar cualquier cambio de salida al mando seleccionado.
- **FR-005**: Mostrar por mando la conexión física, el estado, los errores y el perfil. Cuando Windows asigne una salida Xbox, mostrar su ranura 1–4 en la columna **Xbox** y encender en el Wii Remote únicamente el LED correspondiente. Aplicar la asignación real al reconectar.
- **FR-006**: Interpretar A/B/1/2/+/−/HOME/cruceta sin confundir bits de sensores con botones.
- **FR-007**: Incluir perfil horizontal y Wii Remote + Nunchuk; no solicitar sensores innecesarios en modo botones.
- **FR-008**: Identificar e inicializar Nunchuk, calibrar centro/rango y zona muerta; neutralizar sus entradas al retirarlo.
- **FR-009**: Editar, validar, guardar, importar, exportar y aplicar perfiles por mando; mantener el último válido ante error.
- **FR-010**: Asignar botones, cruceta, hombros, pulsación de sticks y gatillos digitales; obtener ejes analógicos del stick Nunchuk.
- **FR-011**: Neutralizar y liberar las salidas propias cuando termina una sesión, falla la comunicación o se elige **Salir**. Mantener independientes las salidas ajenas.
- **FR-012**: Permitir reconexión manual y reintentos limitados sin duplicar sesiones.
- **FR-013**: Entregar entrada al llegar, independientemente de interfaz, búsqueda, almacenamiento y diagnóstico; sin espera periódica añadida.
- **FR-014**: Preservar transiciones ordenadas y evitar colas ilimitadas; todo descarte se identifica con su causa.
- **FR-015**: Medir procesamiento y cadencia en el entorno de pruebas con muestras acotadas, unidades y ventana temporal. La lista de mandos muestra **Mando**, **Xbox**, **Estado** y **Perfil**.
- **FR-016**: Exportar en las pruebas de desarrollo mediciones con percentiles y condiciones. Separar procesamiento, observación virtual y extremo a extremo.
- **FR-017**: Comprobar al arrancar si el controlador virtual está disponible y avisar si falta, es incompatible o falla el acceso. Ofrecer **Instalar controlador Xbox** si falta o es incompatible. Distribuir el instalador oficial firmado dentro de la aplicación, con instalación sin Internet iniciada por el usuario y elevación de Windows. Volver a comprobar el controlador al terminar e informar de cancelación, fallo o reinicio requerido. Mantener **Ratón IR** disponible sin controlador Xbox.
- **FR-018**: Entregar paquete Windows con entorno de ejecución incluido, licencias y guías. La utilidad de observación XInput y su comparador forman parte de las pruebas de desarrollo.
- **FR-019**: Configuración y diagnóstico locales; jugar sin cuenta ni conexión a Internet tras instalar requisitos.
- **FR-020**: Sensores, vibración y modificaciones globales no se activan por defecto; cualquier mejora opcional respeta presupuesto de latencia.
- **FR-021**: Informar modelo/accesorio no soportado y mantener entradas básicas seguras cuando sea posible.
- **FR-022**: Registrar matriz real de modelos, accesorios, adaptadores, Windows, controladores y juegos probados.

### Key Entities

- **Mando físico**: Identidad, modelo, conexión, accesorio, capacidades y último informe válido.
- **Sesión**: Un mando, una salida, perfil aplicado, estado de ciclo de vida y fallo.
- **Salida Xbox**: Botones, ejes, gatillos, índice del sistema y sesión propietaria.
- **Perfil**: Nombre, versión, orientación, asignaciones, calibración y zona muerta.
- **Medición**: Tipo de demora, unidades, muestras, duración, percentiles y condiciones.
- **Compatibilidad**: Combinación de equipo/software, casos ejecutados, resultado y evidencia.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: De sincronización a entrada Xbox visible en ≤60 s, excluyendo instalación y acciones del usuario, en ≥9/10 intentos por combinación soportada.
- **SC-002**: Cuatro mandos durante 60 minutos sin cruces, duplicados ni botones atascados; 100 ciclos conectar/desconectar sin pérdidas de recursos de sesión.
- **SC-003**: Procesamiento de informe completo disponible→salida enviada con p95 ≤1 ms y p99 ≤2 ms, tanto con uno como cuatro mandos, durante 10 minutos y ≥10.000 informes por mando. Objetivo de aceptación; no es resultado actual ni garantía de radio.
- **SC-004**: 100.000 transiciones sintéticas pulsar/soltar llegan en orden al destino correcto, sin eventos inventados ni estados finales activos.
- **SC-005**: Interacción/minimización de interfaz y exportación aumentan p99 de procesamiento ≤0,5 ms respecto al ensayo equivalente en reposo.
- **SC-006**: En equipo de referencia, recepción en aplicación→estado virtual observado independientemente con p95 ≤4 ms, indicando frecuencia e incertidumbre del observador. No mide radio.
- **SC-007**: Entrega incluye ensayo físico pulsación→respuesta con mediana/p95/p99, condiciones de vídeo/juego y comparación de configuraciones. No se fija cifra absoluta antes de medir hardware.
- **SC-008**: Neutralización en ≤1 s tras pérdida observable de comunicación. Cuando termina la sesión o se elige **Salir**, neutralizar al procesar la orden y liberar recursos en ≤3 s.
- **SC-009**: En matriz soportada pasan todos los botones, diagonales, rangos/centro, Nunchuk y retirada en caliente; perfil inválido nunca sustituye al válido.
- **SC-010**: En Windows sin SDK ni controlador Xbox, el paquete abre, instala el controlador sin Internet tras la autorización del usuario y permite comprobar una salida Xbox. Cubre cancelación, reinicio requerido, radio apagada y equipo incompatible sin cierre inesperado. El ZIP carece de ejecutables auxiliares y scripts de diagnóstico propios del proyecto.
- **SC-011**: Con una barra física, verificar movimiento en los cuatro sentidos, clic y arrastre, pérdida y recuperación de referencia, retirada de pilas con clic sostenido y Xbox concurrente. Probar ratón sin ViGEmBus y comprobar la calibración en dos posiciones de la barra.
- **SC-012**: Para cada ranura 1–4, el número de la columna **Xbox** coincide con el único LED encendido. Repetir con una ranura ocupada previamente y después de reconectar.

## Assumptions

- «Diferentes controles Xbox» significa varios jugadores independientes. Salida inicial Xbox 360/XInput; no se promete protocolo nativo Xbox One/Series.
- Primera entrega: Windows 10/11 x64, Wii Remote y Wii Remote Plus originales, solos o con Nunchuk. Cada combinación necesita verificación; clones no se presumen compatibles.
- Máximo cuatro salidas, compartiendo disponibilidad con otros XInput. Índices los asigna Windows.
- Se requiere Bluetooth Classic compatible. Asociación desde Windows es alternativa guiada si falla el flujo integrado.
- El controlador integrado se instala con elevación de Windows tras una acción explícita. El uso habitual de Wii2Xbox se mantiene sin elevación.
- Latencia mínima significa reducir demora añadida y variabilidad medida, sin prometer superar límites del mando, radio, Windows o juego. Presupuestos numéricos iniciales no se relajan silenciosamente.
- Fuera de esta entrega: Classic Controller, Balance Board, guitarras, teclado, gestos, sensores MotionPlus, macros y red. Vibración bidireccional es mejora posterior condicionada a mediciones.

## Ratón con barra infrarroja Wii

- **FR-023**: Elegir Xbox, ratón IR o ambas salidas para un Wii Remote. La opción exclusiva de ratón debe funcionar sin el controlador virtual Xbox.
- **FR-024**: Apuntar a la pantalla principal con una barra IR alimentada, mover el cursor con la cámara del mando y usar A/B para clic izquierdo/derecho, incluido mantener pulsado para arrastrar.
- **FR-025**: Una sesión como máximo controla el cursor. Perder la referencia, detener la sesión o perder Bluetooth libera los clics. En modo combinado se conservan los botones de Xbox y el Nunchuk compatible.
- **FR-026**: Calibrar el cursor apuntando a dos referencias en pantalla. Guardar el resultado con el perfil del mando y permitir cancelar o restaurar los valores de fábrica. La captura de los puntos libera e impide los clics del ratón.

## Delivery and Acceptance

La entrega incluye código, perfiles iniciales, pruebas, paquete con instalador integrado, guías, matriz física e informe de aceptación. Las herramientas de medición forman parte de las pruebas de desarrollo.

Secuencia: base verificable → un mando real → cuatro mandos → conexión/recuperación → perfiles/Nunchuk → diagnóstico/optimización → paquete y aceptación física. Cada SC debe tener evidencia antes de declarar completado el objetivo.
