# Tasks: Wii2Xbox con latencia mínima

**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/pipeline.md](contracts/pipeline.md).

Formato: ID, historia y archivo concreto. `[x]` indica que la tarea tiene evidencia. Las comprobaciones físicas restantes figuran en [aceptación](acceptance.md).

## Phase 1: Setup

- [x] T001 Inicializar Spec Kit 1.1.3 con integración Codex y PowerShell en `.specify/` y `.agents/skills/`.
- [x] T002 Establecer constitución, spec completa, checklist y feature activa en `.specify/memory/constitution.md`, `.specify/feature.json` y `specs/001-wii-xbox-low-latency/`.
- [x] T003 Crear y verificar esqueleto desktop, exclusiones de compilados y guía raíz en `src/Wii2Xbox/`, `.gitignore` y `README.md`; build Release correcto, 0 errores/avisos (2026-11-22). No acredita conversión funcional.
- [x] T004 Migrar a .NET 10 LTS, fijar SDK y dependencias. Compilación Release verificada.
- [x] T005 Crear proyecto de pruebas y scripts reproducibles build/test. Once casos aprobados.

## Phase 2: Foundational — gate de viabilidad

- [ ] T006 Probar backend firmado en Windows objetivo, revisar licencia/mantenimiento y decidir adopción o sustitución en `specs/001-wii-xbox-low-latency/research.md` y `docs/compatibility.md` (FR-017).
- [x] T007 Definir transporte HID, salida Xbox y sesión en `HidTransport.cs`, `XboxOutput.cs` y `ControllerSession.cs`. Las pruebas de sesión ejercitan los mismos contratos con transportes simulados.
- [ ] T008 Implementar identidad estable, filtrado de modelos y catálogo de capacidades en `src/Wii2Xbox/HidTransport.cs` (FR-003/021).
- [ ] T009 Introducir estados y errores tipados con limpieza idempotente en `src/Wii2Xbox/ControllerSession.cs` (FR-011).
- [ ] T010 Crear fixtures reales/referenciados de informes, ACK, errores y truncados en `tests/Wii2Xbox.Tests/ProtocolTests.cs`; no interpretar respuestas de control como entrada (FR-006).

## Phase 3: US1 — un mando real, respuesta inmediata (P1)

**Checkpoint**: Un Wii real controla un juego mediante una salida, con liberación al detener.

- [ ] T011 [US1] Completar parser validado por ID/longitud/bits en `src/Wii2Xbox/Input.cs`; probar combinaciones/sensores en `tests/Wii2Xbox.Tests/ProtocolTests.cs` (FR-006).
- [ ] T012 [US1] Abrir/configurar Wii, negociar modos y validar ACK con lectura cancelable en `src/Wii2Xbox/HidTransport.cs` (FR-007).
- [x] T013 [US1] Enviar estado completo, consultar ranura real y explicar errores de dependencia en `XboxOutput.cs`. Comprobado con una y cuatro salidas XInput (FR-004/005/017).
- [x] T014 [US1] Implementar lector→mapeo→submit sin espera de UI y distinguir descartes en `ControllerSession.cs`. Evidencia sintética y de salida real en el informe de rendimiento (FR-013/014).
- [ ] T015 [US1] Implementar neutralización ante stop/error/inicio parcial; cierre desbloquea lectura en `src/Wii2Xbox/ControllerSession.cs` y pruebas `SessionTests.cs` (FR-011, SC-008).
- [ ] T016 [US1] Mostrar lista, conectar/detener, estado real y entrada en snapshots en `src/Wii2Xbox/MainForm.cs` (FR-001/005).
- [ ] T017 [US1] Ensayo físico de todos los botones, primer juego y stop sostenido; registrar evidencia en `docs/compatibility.md` (SC-001/009).

## Phase 4: US3 — hasta cuatro sesiones independientes (P1)

**Depends on**: US1 y control de ciclo de vida.

- [ ] T018 [US3] Implementar exclusión por identidad, capacidad y propiedad de salida en `src/Wii2Xbox/SessionManager.cs` (FR-003/004/005).
- [x] T019 [US3] Mostrar hasta cuatro sesiones y la ranura Xbox asignada por Windows en `src/Wii2Xbox/MainForm.cs` (FR-005).
- [ ] T020 [US3] Probar 100.000 transiciones, colisiones de inicio, fallos aislados y 100 ciclos en `tests/Wii2Xbox.Tests/MultiControllerTests.cs` (SC-002/004).
- [ ] T021 [US3] Ensayar cuatro mandos 60 minutos, quinto mando y controles Xbox ajenos en `docs/compatibility.md` (SC-002).

## Phase 5: US2 — Bluetooth guiado y recuperación (P1)

**Independent test**: Descubrir/asociar mando nuevo, cancelar y recuperar uno conocido sin duplicados.

- [x] T022 [US2] Implementar inventario de radios, descubrimiento y asociación de modelos Nintendo en `src/Wii2Xbox/BluetoothService.cs` (FR-002). Asociación real pendiente en T026.
- [ ] T023 [US2] Implementar cancelación real ≤5 s e informar errores de radio/acceso/asociación (FR-002). Búsqueda en curso cancelada en unos 2,3 s con el adaptador local. Verificación en otros adaptadores pendiente.
- [x] T024 [US2] Añadir guía SYNC/1+2 y alternativa de asociación de Windows en `src/Wii2Xbox/MainForm.cs` y `docs/troubleshooting.md` (FR-002/018).
- [x] T025 [US2] Reconexión manual y hasta cuatro reintentos, eventos de dispositivo y reenumeración tras suspensión en `SessionRecovery.cs` y `MainForm.cs` (FR-012). Ensayo físico pendiente.
- [ ] T026 [US2] Ensayar modelos -01/-TR, 10 intentos por combinación, cancelación y ausencia de radio en `docs/compatibility.md` (SC-001/010).

## Phase 6: US4 — perfiles completos y Nunchuk (P2)

- [x] T027 [US4] Versionar esquema y validar catálogo, direcciones opuestas y múltiples fuentes en `Input.cs` y `ProfileTests.cs` (FR-009/010).
- [x] T028 [US4] Implementar carga, guardado atómico e importación/exportación con validación previa en `ProfileStore.cs` y `ProfileDialog.cs` (FR-009/019).
- [x] T029 [US4] Añadir editor por mando y aplicar el mapeo compilado con un informe neutro previo en `MainForm.cs` y `ControllerSession.cs` (FR-009/010). Verificación en dos sesiones físicas pendiente en T033.
- [x] T030 [US4] Crear y empaquetar perfiles horizontal y Nunchuk en `profiles/horizontal.json` y `profiles/nunchuk.json`. Rotación y liberación verificadas con informes simulados (FR-007).
- [x] T031 [US4] Implementar negociación e identificación del Nunchuk, ACK de escritura, calibración y retirada en caliente en `NunchukProtocol.cs` y `HidTransport.cs` (FR-008/021). Aceptación con accesorio físico en T033.
- [x] T032 [US4] Probar extremos de calibración, centro/zona muerta, ID de accesorio desconocido y rechazo de perfiles inválidos en `NunchukTests.cs` y `ProfileTests.cs`. Ensayo físico pendiente en T033 (SC-009).
- [ ] T033 [US4] Verificar accesorios/perfiles tras reinicio y dos mandos diferentes en `docs/compatibility.md` (SC-009).

## Phase 7: US5 — medir y optimizar latencia (P1)

La arquitectura de T014 evita esperas desde el principio; esta fase aporta evidencia y optimización, no pospone el requisito.

- [x] T034 [US5] Capturar timestamps monotónicos en buffer acotado y snapshots de cadencia en `src/Wii2Xbox/LatencyMetrics.cs` (FR-015).
- [x] T035 [US5] Exportar p50/p95/p99/max de procesamiento con esquema y condiciones en `src/Wii2Xbox/LatencyReport.cs` para las pruebas de desarrollo (FR-016). La medición física de extremo a extremo queda en T040.
- [x] T036 [US5] Crear observador XInput independiente y comparación de transiciones con frecuencia e incertidumbre. `scripts/test-native.ps1` prepara ambos fuera del paquete de usuario (FR-016/018, SC-006).
- [x] T037 [US5] Validar percentiles, extremos del buffer, asignaciones y coste diagnóstico. Pasaron las pruebas unitarias y la comparación activado/desactivado con ViGEmBus real y entrada simulada. La medición con mandos físicos permanece en T038 (FR-015/016).
- [ ] T038 [US5] Ensayar 1/4 mandos, UI reposo/uso/minimizada y diagnóstico on/off en `docs/performance.md` (SC-003/005/006). Pasaron los casos nativos breves y sostenidos de una y cuatro salidas con entrada simulada. Protocolo físico en `docs/hardware-validation.md`.
- [ ] T039 [US5] Comparar política HID, informes y prioridad con base equivalente; conservar ajustes medidos y registrar alternativas en `docs/performance.md` (FR-013/014/020).
- [ ] T040 [US5] Ensayo físico pulsación→respuesta, muestra suficiente para percentiles y límites del instrumento en `docs/performance.md` (SC-007).

## Phase 8: US6 — distribución, fallos y aceptación (P2)

- [ ] T041 [US6] Watchdog y cierre con botón sostenido comprobados con transportes simulados en `RecoveryTests.cs`. Pendientes los ensayos de corte de radio y suspensión físicos (FR-011/012, SC-008).
- [x] T042 [US6] Publicar paquete self-contained win-x64 reproducible con licencias y versiones en `scripts/publish.ps1` y `docs/installation.md` (FR-017/018).
- [ ] T043 [US6] Validar en Windows limpio, sin SDK ni Internet, la instalación desde Wii2Xbox y el primer mando virtual. Repetir con cancelación de elevación y con reinicio requerido. Registrar resultados reales en `acceptance.md` (SC-010).
- [ ] T044 [US6] Documentar acceso HID, Bluetooth, límites XInput, modelos no verificados y juegos restrictivos en `docs/troubleshooting.md` (FR-017/018/021/022).
- [ ] T045 [US6] Verificar cierre ≤3 s, neutralización ≤1 s y recursos tras 100 reconexiones en `docs/compatibility.md` (SC-002/008).

## Phase 9: Convergencia y entrega completa

- [x] T046 Auditar FR-001–FR-026 y SC-001–SC-011 contra código y evidencia en `specs/001-wii-xbox-low-latency/acceptance.md`. Pendientes físicos identificados sin sustituirlos por simulaciones.
- [ ] T047 Ejecutar build/test/publish finales y prueba del paquete en equipo limpio; registrar comandos, versiones y resultados en `acceptance.md`.
- [ ] T048 Documentar límites/riesgos pendientes y comprobar que no se publican afirmaciones de latencia no medidas en `README.md` y `docs/performance.md`.
- [ ] T049 Actualizar tareas y estado de la spec solo con evidencia real; declarar producto completo únicamente si no quedan requisitos obligatorios pendientes.

## Dependencias y trazabilidad

### Ratón con barra IR

- [x] T050 Implementar activación por ACK, decodificación de puntos y sesión combinada con Xbox/Nunchuk (FR-023/024).
- [x] T051 Implementar cursor, clics y liberación con pérdida de seguimiento. Verificar con informes simulados (FR-024/025).
- [ ] T052 Verificar seguimiento, clics y uso combinado con una barra Wii física y un Wii Remote (SC-011).
- [x] T053 Calibrar el ratón IR mediante dos puntos y guardar el ajuste en el perfil del mando (FR-026). Prueba física incluida en T052.

### Identificación y distribución

- [x] T054 [US3] Asociar la columna **Xbox** y el LED físico con la ranura real. Verificados los cuatro comandos HID con un Wii Remote y la asignación de Xbox 2 con Xbox 1 ocupada (FR-005).
- [x] T055 [US6] Integrar el instalador firmado, verificar el recurso publicado y retirar el observador y comparador del ZIP. Cubrir detección en `DriverAvailabilityTests.cs`. La instalación en Windows limpio se verifica en T043 (FR-017/018).
- [ ] T056 [US3] Repetir la coincidencia entre el LED y la columna **Xbox** en cada ranura, tras reconexión y con varios Wii Remote físicos (SC-012).
- [ ] T057 [US6] Comparar XInput, Windows.Gaming.Input y ControllerTest en Edge con la instalación oficial integrada en Windows limpio. Registrar también la detección en un juego XInput (FR-022).
- [x] T058 [US2] Automatizar la búsqueda del asistente y cancelar su ciclo al cerrar. Retirar el acceso directo a la configuración Bluetooth de Windows (FR-002).
- [x] T059 [US1, US6] Activar los mandos disponibles al detectarlos, aplicar su salida al seleccionarla y comprobar el controlador Xbox al arrancar. Conservar la captura con sesiones activas para las pruebas de desarrollo (FR-004/015/017).
- [ ] T060 [US3] Verificar la activación automática de varios Wii Remote físicos al arrancar, conectar y volver a conectar, con cambios independientes de salida (FR-004/005).
- [x] T061 Renombrar el asistente a **Añadir mando** y simplificar la ventana a cuatro columnas. Reservar las mediciones para las pruebas de desarrollo (FR-002/015/016).

Setup → gate backend/transporte → US1 → US3/US2/US4 → medición completa US5 → US6 → convergencia. Las pruebas de pureza de protocolo y mapeo pueden avanzar sin hardware; los gates físicos no.

| Requisitos | Tareas principales |
| --- | --- |
| FR-001/005 | T016/T019/T023/T054/T056/T060 |
| FR-002/003/012 | T008/T018/T022–T026/T058 |
| FR-004/006/007 | T010–T014/T018/T030/T059/T060 |
| FR-008/009/010 | T027–T033 |
| FR-011/013/014 | T009/T014/T015/T020/T039/T041 |
| FR-015/016/020 | T034–T040/T059 |
| FR-017/018/019 | T006/T028/T036/T042–T044/T055/T059 |
| FR-021/022 | T008/T026/T031/T033/T044/T057 |

MVP demostrable: T001–T017. La entrega se completa con la aceptación de los criterios aplicables de `spec.md`.

## Incremento implementado

El paquete local ofrece asociación, hasta cuatro salidas independientes, indicación de ranura por LED, perfiles, Nunchuk, ratón IR e instalación del controlador Xbox desde Wii2Xbox. La [aceptación](acceptance.md) recoge la evidencia y las pruebas que faltan.
