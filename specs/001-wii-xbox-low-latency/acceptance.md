# Estado de verificación

Fecha: 2026-11-22.

## Ejecutado

- `specify init --here --integration codex --script ps --force --non-interactive`: correcto, Spec Kit 1.1.3, integración y scripts instalados.
- `check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks`: correcto; feature resuelta a `specs/001-wii-xbox-low-latency`, detectados research, data-model, contracts, quickstart y tasks.
- Revisión documental: spec completa, 22 requisitos funcionales, 10 criterios de aceptación, 6 historias y 49 tareas. Checklist de requisitos completado; sin marcadores pendientes.
- `dotnet build src/Wii2Xbox/Wii2Xbox.csproj -c Release`, SDK local 8.0.428: correcto, cero errores y cero avisos. Esqueleto desktop, no implementación funcional. Runtime de entrega pendiente de migración.
- Repositorio Git local inicializado en `main`. Sin commit ni remoto configurado en esta sesión.

## Conversión básica implementada

- .NET 10.0.404 fijado. Las 11 pruebas Release de informes básicos, máscara de botones, orientación y liberación pasaron.
- Publicación autónoma win-x64 y archivo `artifacts/Wii2Xbox-win-x64.zip` generados. Se verificó que el ejecutable publicado abre una ventana Wii2Xbox y cierra normalmente en este equipo. Queda pendiente la prueba en otro equipo sin SDK.
- La interfaz enumera rutas HID de Nintendo y controla hasta cuatro sesiones. Cada sesión utiliza una lectura dedicada y un envío completo a ViGEmBus, sin esperar al refresco de interfaz.
- El inventario PnP local mostró Bluetooth habilitado, sin dispositivos Wii o ViGEmBus presentes. La correspondencia entre mando real, salida XInput y respuesta de juego sigue pendiente.

## Asociación Bluetooth integrada

- El diálogo **Añadir mando** busca por radio, presenta los modelos Nintendo admitidos y permite seleccionar el mando y el método SYNC o 1+2. La autenticación usa el PIN de seis bytes que corresponde al método indicado. [Dolphin en Windows](https://github.com/dolphin-emu/dolphin/blob/master/Source/Core/Core/HW/WiimoteReal/IOWin.cpp) · [API de servicio HID](https://learn.microsoft.com/en-us/windows/win32/api/bluetoothapis/nf-bluetoothapis-bluetoothsetservicestate)
- La API Bluetooth nativa completó una búsqueda sin mandos en 2.644 ms. Una segunda búsqueda con cancelación solicitada a los 300 ms terminó a los 2.619 ms desde el inicio. Las 11 pruebas existentes de botones siguen pasando.

## Instrumentación de latencia

- Diagnóstico opcional por sesión. Captura monotónica de los últimos 131.072 envíos y exportación JSON con tipo de medición, condiciones, cadencia, duración de ventana, muestras, p50/p95/p99/máximo y muestras excluidas. El envío inicial se excluye por incluir la creación de la salida virtual.
- Para el informe básico 0x30, la sesión prepara el mapeo de botones antes de abrir el dispositivo. Cada informe usa una consulta por índice; la prueba compara las 2.048 combinaciones de botones en ambas orientaciones con el mapeador general.
- 17 pruebas Release aprobadas. La prueba sintética de captura no detecta asignaciones por informe tras reservar el buffer. El paquete x64 se regeneró con esta versión.

## Perfiles por mando

- El editor asigna botones digitales y cruceta, guarda por ruta HID, e importa/exporta JSON versión 1. Las sesiones aplican un nuevo mapa al recibir el siguiente informe, con un envío neutro previo.
- 23 pruebas Release aprobadas, incluidas persistencia aislada, rechazo de una versión desconocida sin sobrescribir el perfil, destinos inválidos, direcciones opuestas y liberación al compartir destino.

## Nunchuk

- Negociación de la extensión con ACK de escritura e identificación, lectura de la calibración de fábrica y zona muerta configurable. Los informes normales del Wii Remote siguen usando el modo básico durante la negociación. Retirar el accesorio libera stick y C/Z y mantiene la salida Xbox.
- 27 pruebas Release aprobadas, incluidas secuencia de comandos, calibración y límites, accesorio desconocido, retirada y calibración de respaldo. No se observó tráfico de un Nunchuk físico.
- Referencias de interoperabilidad: [informes y registros Wii Remote](https://www.wiibrew.org/wiki/Wiimote), [identificación de extensiones](https://www.wiibrew.org/wiki/Wiimote/Extension_Controllers) y [distribución de calibración del Nunchuk en Dolphin](https://github.com/dolphin-emu/dolphin/blob/master/Source/Core/Core/HW/WiimoteEmu/Extension/Nunchuk.h).

## Observador XInput

- El proceso independiente consulta XInput 1.4 y registra transiciones de Xbox A y sus intervalos de observación. La comparación usa las marcas QPC de la app y del observador, mide hasta la lectura XInput y rechaza secuencias que no coinciden. Referencias: [lectura XInput](https://learn.microsoft.com/en-us/windows/win32/api/xinput/nf-xinput-xinputgetstate) y [QPC](https://learn.microsoft.com/en-us/windows/win32/sysinfo/acquiring-high-resolution-time-stamps).
- El ejecutable publicado consultó las cuatro ranuras en Windows. Todas estaban desconectadas. Con trazas sintéticas, el comparador obtuvo p50 2 ms y p95 3 ms según los valores de entrada y rechazó una liberación incompatible. Esos valores son de la prueba del cálculo.
- Pasaron 28 pruebas automatizadas. La captura real y el objetivo de p95 para SC-006 quedan pendientes en T038.

## Ratón con barra IR

- El modo exclusivo mueve el cursor sin ViGEmBus. El modo combinado emite Xbox y ratón desde la misma sesión. Ambos usan los dos extremos detectados por la cámara del Wii Remote.
- Pruebas automatizadas cubren secuencia de ACK, formato de coordenadas, pérdida de referencia, liberación durante el cierre y coexistencia de IR/Nunchuk en el mismo informe.
- Las pruebas de sesión completa recorren la inicialización IR y la negociación del Nunchuk con informes simulados. En modo combinado se verificaron botones y ejes Xbox junto con la liberación del clic al perder la barra. En modo ratón exclusivo se comprobó que no se abre la salida Xbox ni se configura el accesorio.
- La calibración usa dos puntos, ofrece restaurar valores de fábrica y se conserva en el perfil. Las pruebas cubren el mapeo con eje invertido, los clics inhibidos mientras se captura, el borde de pulsación y la persistencia por mando.
- SC-011 pendiente de hardware. Referencias: [cámara IR de Wii Remote](https://www.wiibrew.org/wiki/Wiimote) y [coordenadas de ratón de Windows](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-mouseinput).

## Recuperación

- Cada mando reintenta tras un fallo de transporte o suspensión en esperas de 0,5 s, 1 s, 2 s y 4 s. Se conserva perfil y modo. Detener cancela el plan y un error de la salida Xbox no dispara búsqueda Bluetooth.
- La salida Xbox recibe estado neutro antes de cerrarse en las pruebas de pérdida y de cancelación con lectura bloqueada. El plazo de inactividad se calcula desde el último informe válido.
- Una prueba con cierre HID deliberadamente bloqueado confirma que el estado Xbox neutro se envía y la salida se libera antes de cerrar el transporte tras un fallo de lectura.
- Cuatro sesiones simuladas entregaron 100.000 transiciones alternadas a sus respectivas salidas sin pérdida. SC-002 conserva pendiente la hora de uso con hardware real y SC-008 requiere corte del adaptador, suspensión y cierre con la salida real.
- La sesión distingue informes HID incompletos y de formato desconocido. Los informes JSON de desarrollo registran ambos por separado. Una prueba intercala ambos casos y un ACK válido entre pulsar y soltar. Los dos estados válidos llegan a Xbox en orden.
- Manejo de suspensión y reanudación basado en [WM_POWERBROADCAST](https://learn.microsoft.com/en-us/windows/win32/power/wm-powerbroadcast).

## Integración nativa de la salida Xbox

- La comprobación del controlador no ocupa ranuras virtuales. Las pruebas cubren ausencia, incompatibilidad y acceso denegado. El comando de desarrollo `driver` verificó el controlador instalado y dejó las cuatro ranuras libres. El ensayo anterior de ventana confirmó que el controlador respondía con cuatro salidas reales activas.

- La autoprueba confirmó el servicio ViGEmBus activo mediante el propio SDK. En este equipo el servicio se presenta como **Bigscreen Gamepad Emulation Service**, versión 1.17.333.0. Su firma de controlador es válida. Esta comprobación reemplaza la inferencia anterior basada en buscar el nombre ViGEm en los identificadores PnP.
- La ruta compartida por la aplicación y el diagnóstico creó una y cuatro salidas reales, verificó sus ranuras distintas, botones, ejes y gatillos mediante XInput, neutralizó los valores y desconectó los mandos. Se completaron 100 ciclos de salida virtual y las cuatro ranuras quedaron libres.
- Se ejecutó la prueba de cuatro salidas desde el ZIP extraído, sin elevación y con rutas de acceso al SDK excluidas. El paquete incluye avisos de licencia y guía. Esto se comprobó en el equipo local, con el controlador existente.
- La búsqueda Bluetooth Classic devolvió cero Wii Remote durante el ensayo. Siguen pendientes la entrada desde mandos reales, el juego, la latencia física y la comprobación de 1.22.0 en otro equipo. [Matriz de compatibilidad](../../../docs/compatibility.md)

## Sesión completa con salida Xbox real

- Pasaron 38 pruebas ordinarias y dos pruebas nativas optativas. Uno y cuatro lectores produjeron informes Wii simulados, procesados por `ControllerSession` y enviados a ViGEmBus real. Un proceso independiente emparejó las 40 pulsaciones y liberaciones de Xbox A en cada escenario.
- El p95 desde la lectura del informe hasta la observación XInput fue de 1,007 ms con una sesión y 0,893 ms con cuatro activas. El procesamiento local obtuvo máximos de p95 y p99 de 0,165 y 0,317 ms entre las cuatro sesiones. Ver frecuencia e incertidumbre en el [informe de rendimiento](../../../docs/performance.md).
- Esta ejecución breve deja pendientes los ensayos sostenidos y físicos. La búsqueda Bluetooth Classic devolvió cero Wii Remote.

## Ensayo sostenido de cuatro salidas

- Se completaron al menos 601 segundos por salida Xbox real, con 38.732–38.734 informes Wii simulados por sesión. El mayor p95 de procesamiento fue 0,190 ms y el mayor p99 fue 0,336 ms. Las cuatro salidas quedaron libres al terminar y el buffer conservó todas las muestras.
- Se completaron 600,997 segundos con una salida y 38.664 informes simulados, con p95 de 0,195 ms, p99 de 0,335 ms y máximo de 2,102 ms. El buffer conservó todas las muestras y XInput confirmó la liberación al terminar.
- T038 tiene evidencia sostenida para una y cuatro sesiones con entrada simulada. Queda la validación con mandos físicos. [Informe de rendimiento](../../../docs/performance.md)

## Identidad del mando tras la asociación

- La aplicación distingue la autenticación Bluetooth de la aparición de la interfaz HID. Tras asociar, espera a la interfaz correspondiente y selecciona el mando identificado. Cuando el árbol de dispositivos expone la dirección Bluetooth se usa para distinguir mandos y guardar perfiles. La ruta HID sirve de alternativa y permite abrir perfiles antiguos mientras esté disponible.
- SetupAPI resolvió las 19 interfaces HID presentes en el equipo. Configuration Manager confirmó la dirección de dos dispositivos Bluetooth Classic presentes. La búsqueda sigue sin detectar un Wii Remote, por lo que falta el ensayo de asociación al modelo Nintendo real. [API de asociación entre interfaz y devnode](https://learn.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdigetdeviceinterfacedetailw) y [consulta de padres](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_get_parent).
- Pasaron 47 pruebas ordinarias, incluidas resolución de formatos Bluetooth y compatibilidad de perfiles antiguos.
- El ejecutable publicado `Wii2Xbox.Diagnostics.exe bluetooth 3` completó dos búsquedas Bluetooth Classic y comprobó la lista HID. Vio cero mandos Wii y cero interfaces HID Wii. El protocolo físico del paquete documenta cómo observar el paso de descubrimiento a HID sin cambiar las asociaciones.

## Pendiente

Quedan la comprobación de asociación y entrada con Wii Remote y Wii Remote Plus, el cambio de perfil y Nunchuk con hardware real, ensayos de recuperación y mediciones de latencia con mandos físicos. SC-004 queda respaldado por la prueba sintética prevista en la especificación.

## Primera verificación con un Wii Remote físico

- Windows asoció el `Nintendo RVL-CNT-01` (dirección terminada en DBB0) mediante SYNC y publicó su interfaz HID. Una sesión de Wii2Xbox abrió el dispositivo por su identidad Bluetooth y activó Xbox 1.
- El observador XInput independiente registró 15 pulsaciones y 15 liberaciones de Xbox A (botón 2 en el perfil predeterminado), con sondeo real de 993,3 Hz. La sesión se mantuvo activa y, al cerrarla, las cuatro ranuras quedaron libres.
- Con Xbox 1 ocupada por una salida virtual de prueba, la sesión del Wii Remote físico se identificó como Xbox 2. El observador XInput detectó ambas salidas y el mando confirmó por HID que solo estaba activo el LED 2. Los cuatro comandos de LED se verificaron por separado con el mismo mando físico.
- Wii2Xbox midió 1.505 informes en 15,26 s (98,54 Hz), sin descartes por formato o longitud. Desde lectura HID hasta retorno de ViGEmBus, p95 = 0,0593 ms, p99 = 0,0898 ms y máximo = 0,2625 ms. Esta prueba breve no incluye tiempo de radio ni respuesta de un juego.
- Quedan por verificar la matriz de modelos, el juego, cuatro dispositivos físicos y la barra IR.

## Paquete con instalador integrado

- Publicación Release Windows x64 completada. El instalador ViGEmBus 1.22.0 tiene firma Authenticode válida de Nefarius. El SHA-256 del recurso incrustado coincide con el publicado en WinGet. El ZIP incluye la licencia, la aplicación, el entorno de ejecución y las guías.
- El observador XInput y el comparador se ejecutan desde el repositorio. Se verificó que el ZIP carece de esos archivos y que no contiene un instalador separado. Pasaron 67 pruebas automatizadas y cuatro ensayos nativos de ejecución separada quedaron omitidos en esta pasada.
- La instalación desde el botón, la elevación, la cancelación y el reinicio quedan pendientes de aceptación en Windows limpio sin el controlador.

## ControllerTest en Edge

- El Wii Remote físico estaba activo como Xbox 1. XInput registró 34 pulsaciones y 34 liberaciones de A, mientras la página de Xbox en Edge indicaba **Not connected**. La página general también mostró **Not connected** al probar con una segunda salida virtual temporal.
- Windows tenía registrado Bigscreen Gamepad Emulation Bus 1.17.333.0. En la misma comprobación, XInput enumeró Xbox 1 y Windows.Gaming.Input enumeró cero mandos y cero dispositivos `RawGameController`. Falta repetir con la instalación oficial integrada en un equipo limpio y comprobar un juego XInput. La causa de la ausencia en Windows.Gaming.Input queda abierta.

## Trazabilidad de aceptación

La prueba de la ventana de asociación inició la búsqueda al abrir, la mantuvo activa durante 10,5 s con un resultado y la canceló al cerrar en 1,184 s. La versión actual retira de la ventana principal los botones de configuración Bluetooth, activación, detención y comprobación manual. Las pruebas automatizadas cubren el encendido y apagado de la captura de latencia con una misma sesión Xbox. Queda por verificar la activación automática con varios mandos físicos en T060.

| Requisitos | Evidencia que existe | Verificación que falta |
| --- | --- | --- |
| FR-001, FR-018 | Arranque verificado de la app en español en una compilación anterior. Publicación actual comprobada con .NET y guías incluidos, sin el observador de desarrollo | Repetición en instalaciones limpias de Windows 10 y Windows 11 |
| FR-002, FR-003 | Búsqueda y cancelación con radio local. Identidad HID enlazada por SetupAPI. Pruebas de formatos Bluetooth y perfiles heredados | Asociación y reconexión con Wii Remote y Wii Remote Plus físicos, ausencia de radio en otro entorno |
| FR-004, FR-005 | Ranuras XInput independientes y columna Xbox. Cuatro comandos LED confirmados por un Wii Remote físico. Sesión física en Xbox 2 con Xbox 1 ocupada | Cuatro mandos reales, aislamiento en juego y verificación de reasignación tras reconectar |
| FR-006, FR-007 | Decodificación y máscaras en pruebas. Botón 2 → Xbox A comprobado con Wii Remote físico. Perfiles horizontal y Nunchuk incluidos | Resto de botones y perfiles con hardware |
| FR-008, FR-010 | ACK, calibración, zona muerta, retirada y mapeo de Nunchuk en pruebas. Gatillos y stick observados por XInput con tráfico simulado | Nunchuk físico y desconexión en caliente |
| FR-009 | Persistencia e intercambio JSON, recuperación de perfiles antiguos y guardado validado. Editor de asignaciones | Cambio de perfil en una partida con dos mandos físicos |
| FR-011, FR-012 | Estado neutro y cierre probados. Reintentos limitados y cancelación. Cien ciclos de salida virtual | Apagado de mando, corte de radio y suspensión de Windows durante una sesión física |
| FR-013, FR-014 | Lectura→envío directo. 100.000 transiciones sintéticas preservadas. Contadores de formatos desconocidos e incompletos | Tráfico de la radio y política HID comparadas con Wii reales |
| FR-015, FR-016 | Ventanas y percentiles exportados. QPC y observación XInput documentados. Medición de una y cuatro salidas | Latencia física hasta la respuesta visual |
| FR-017 | Ausencia, incompatibilidad y acceso denegado simulados. Firma del instalador y SHA-256 del recurso integrado verificados | Instalación integrada sin Internet en equipo limpio, elevación, cancelación, reinicio y primera salida |
| FR-019, FR-020 | Configuración local, control voluntario de diagnóstico e IR y modo predeterminado Xbox sin cámara, telemetría ni cambios globales | Uso con mandos físicos y verificación de rendimiento de la radio |
| FR-021, FR-022 | Aviso de extensión desconocida y modo básico verificados. El asistente muestra los modelos Wii reconocibles fuera del catálogo y bloquea su asociación. Matriz de pruebas ejecutadas | Ensayos físicos con modelos, accesorios, adaptadores y juegos |
| FR-023, FR-024, FR-025, FR-026 | Sesión completa simulada con IR, Xbox y Nunchuk. Ratón exclusivo sin backend Xbox, clics y calibración persistente comprobados | Barra física, precisión de cursor, arrastre y dos ubicaciones de la barra |

| Criterio | Tareas de evidencia | Estado |
| --- | --- | --- |
| SC-001 | T017/T026 | Pendiente físico |
| SC-002 | T020/T021/T045 | Pendiente |
| SC-003 | T034/T038/T039 | Presupuesto sostenido verificado con una y cuatro sesiones simuladas y ViGEmBus real. Repetición con entrada Bluetooth pendiente |
| SC-004 | T020, `RecoveryTests.FourReadersKeepAllOneHundredThousandTransitionsOnTheirOwnOutputs` | Verificado. 100.000 transiciones sintéticas en orden, sin cruces y con estado final neutro |
| SC-005 | T038 | Dentro del presupuesto con cuatro salidas reales, entrada simulada, ventana WinForms fuera de la vista, minimización y exportación. Partida con mandos físicos pendiente |
| SC-006 | T036/T038 | Presupuesto verificado en XInput real con entrada simulada. Repetición con Wii físico pendiente |
| SC-007 | T040 | Pendiente físico |
| SC-008 | T015/T041/T045 | Pendiente |
| SC-009 | T017/T027/T030–T033 | Pendiente |
| SC-010 | T026/T042–T044/T055 | ZIP y recurso verificados. Pendiente equipo limpio |
| SC-011 | T052 | Pendiente barra IR y Wii Remote físicos |
| SC-012 | T054/T056 | LED 1–4 verificados por separado y coincidencia real en Xbox 2; pendientes multijugador y reconexión |

La comparación de captura activada y desactivada pasó con cuatro salidas ViGEmBus reales y entrada simulada. Se midió desde el transporte hasta el envío con el mismo instrumento en ambos casos. La evidencia y sus límites figuran en el [informe de rendimiento](../../docs/performance.md).
