# Implementation Plan: Wii2Xbox con latencia mínima

**Date**: 2026-11-22 | **Spec**: [spec.md](spec.md)

**Status**: Implementación publicada localmente. Validación en equipo limpio, juegos y configuraciones físicas pendiente.

## Summary

Aplicación nativa Windows que transforma hasta cuatro Wii Remote Bluetooth en salidas Xbox independientes. Primero demostrar un mando físico y una salida real; después aislamiento, recuperación y perfiles. Se optimiza el tramo HID→mapeo→envío sin depender del refresco de pantalla. El producto no se considera completo sin pruebas físicas y percentiles reproducibles.

## Technical Context

**Language/Version**: C# con .NET 10 LTS y SDK 10.0.404, Windows x64. T004 compilado en Release.

**Primary Dependencies**: WinForms, HidSharp 2.6.4 y Nefarius.ViGEm.Client 1.21.256. La publicación integra el instalador 1.22.0 de ViGEmBus. Compatibilidad y mantenimiento del controlador archivado siguen sujetos a la matriz de aceptación.

**Storage**: JSON versionado en `%LOCALAPPDATA%/Wii2Xbox`; escritura temporal y reemplazo atómico. Informes de rendimiento exportados a petición.

**Testing**: xUnit para protocolo, mapeo, estados y aislamiento; transporte/salida simulados; ensayo de integración con observador XInput separado; banco físico documentado.

**Target Platform**: Windows 10/11 x64 en versiones declaradas en matriz. Se revisa soporte efectivo del runtime y de cada versión de Windows, sin presuponer que todas las compilaciones sirven.

**Project Type**: Aplicación desktop local, sin backend.

**Performance Goals**: SC-003 p95 ≤1 ms/p99 ≤2 ms local; SC-006 p95 ≤4 ms hasta observador virtual con incertidumbre declarada. No son resultados obtenidos.

**Constraints**: Cuatro salidas compartidas con XInput del sistema; no control de ranuras absolutas; Bluetooth Classic y controlador firmado compatibles. Sin elevar el proceso de juego, sin bucle ocupado por defecto, sin tiempo real ni cambios globales de energía/temporizadores.

**Scale/Scope**: Un proceso, hasta cuatro sesiones; Wii Remote original/Plus y Nunchuk, perfiles y diagnóstico acotado.

## Constitution Check

| Principio | Diseño previsto | Gate de evidencia |
| --- | --- | --- |
| Latencia mínima | Lector por dispositivo, mapeo y envío síncronos, UI desacoplada | SC-003/005/006/007 |
| Independencia | Sesión propietaria de transporte y salida, limpieza idempotente | SC-002/004/008 |
| Evidencia | Simulación separada de Bluetooth y juegos | Matriz y aceptación física |
| Transparencia | Dependencias visibles; instalación explícita | SC-010 y revisión de paquete |
| Spec primero | Requisitos trazados a tareas y pruebas | Auditoría final de FR/SC |

Diseño conforme; gates de ejecución pendientes. La dependencia retirada es un riesgo explícito, no una exención para declarar compatibilidad.

## Project Structure

```text
src/Wii2Xbox/
  Program.cs, MainForm.cs       # Arranque y panel fuera del camino de entrada
  Input.cs                     # Esqueleto de estados, decodificación y perfil
  HidTransport.cs               # Transporte físico y configuración del protocolo
  BluetoothService.cs           # Descubrimiento/asociación y cancelación
  XboxOutput.cs                 # Adaptador de emulación
  VigemBusSetup.cs              # Instalador integrado y elevación de Windows
  ControllerSession.cs          # Ciclo de vida y camino de baja latencia
  SessionManager.cs             # Identidad, límite y asociación de mandos
  ProfileStore.cs               # Persistencia y validación
  LatencyMetrics.cs             # Buffer acotado y snapshots
tests/Wii2Xbox.Tests/           # Pruebas automatizadas sin hardware
tools/Wii2Xbox.Diagnostics/     # Observador XInput para pruebas de desarrollo
scripts/                       # build, test, publish
profiles/                      # Perfiles iniciales versionados
docs/                          # Guía, compatibilidad y resultados físicos
specs/001-wii-xbox-low-latency/  # Spec, decisiones, contratos y tareas
```

## Architecture and Latency Budget

1. El control de sesión abre un único HID y crea su salida virtual antes de marcar Active. Configuración del mando mediante máquina de estados que espera ACK y valida identificación de accesorio; no interpretar datos cifrados como stick.
2. Un lector dedicado por dispositivo espera una lectura bloqueante/overlapped. No usa sondeo de UI ni `Task.Delay` entre entradas. Se determina con pruebas la política de buffers HID y reportes continuos; no se inventa una frecuencia que el mando no produzca.
3. Tras lectura completa: timestamp monotónico → validar ID/longitud → decodificar → perfil inmutable → actualizar estado completo → un único envío Xbox → timestamp final. Sin JSON, LINQ, logs por evento ni asignaciones evitables en este tramo.
4. Las transiciones pulsar/soltar se conservan. La interfaz refresca estado, ranura y perfil a 5 Hz. Los ensayos de desarrollo toman muestras de latencia de forma independiente.
5. Prioridad normal como base. Solo elevar lector a AboveNormal si mejora distribución medida sin perjudicar estabilidad; nunca prioridad Realtime. No se cambia `timeBeginPeriod`, plan de energía, parámetros de radio ni afinidad por defecto.
6. El watchdog detecta silencio en los informes continuos y neutraliza en ≤1 s desde la pérdida observable. Al terminar la sesión, cancela la lectura y desconecta la salida incluso si falla el envío neutral. La limpieza es idempotente y el envío evita locks compartidos con la interfaz.

Presupuesto local provisional: decodificar/mapear ≤0,15 ms p95; envío ocupa el resto del presupuesto de 1 ms p95. Los subpresupuestos se miden, no se presentan como hechos. Registrar coste de diagnóstico y pausas de runtime, cadencia HID y jitter. No trasladar pérdidas de informe del transporte a una cifra favorable de software.

## Bluetooth and Protocol

Enumeración HID de Nintendo y reconocimiento de modelos; acceso nativo Bluetooth de Windows para radio/descubrimiento/asociación. Cancelación con presupuesto de 5 s requiere trocear operaciones bloqueantes o aislarlas; cancelar solo la pantalla no cumple. Nunca eliminar asociaciones ajenas.

El asistente repite búsquedas Bluetooth cortas en segundo plano y conserva la selección al recibir resultados. La asociación espera a que termine la búsqueda en curso y, si falla, la reanuda. El cierre cancela el bucle antes de descartar la ventana.

Validar por modelo los métodos de apertura y envío HID (`Write` frente a control report) y el modo de informe. Estado/ACK/memoria pertenecen al canal de control, entrada al canal de datos. Inicialización Nunchuk con negociación, identificación y calibración real; retirada en caliente vuelve a neutral del accesorio. Modo básico debe poder funcionar sin extensión.

## Lifecycle and Error Handling

`Discovered → Connecting → Configuring → Active → Stopping → Disconnected`; cualquier etapa puede ir a `Faulted` pasando por limpieza. Un mando solo puede tener una sesión. Reconexión con backoff limitado fuera del lector, acción manual siempre disponible. Suspensión cancela sesiones de manera segura y reanudación reenumera identidades. Inicio parcial revierte recursos adquiridos.

Cambios de perfil mediante snapshot validado e intercambio atómico en frontera de informe; la sesión neutraliza antes de usar el nuevo perfil. Las operaciones administrativas no bloquean la UI ni el lector con escritura a disco.

La enumeración HID y la comprobación del controlador habilitan la activación automática. La aplicación inicia por defecto una salida Xbox por mando disponible. Elegir otra salida cambia solo ese mando y espera a que la sesión anterior libere sus recursos. Las pruebas pueden cambiar la captura de latencia sin reiniciar la salida.

La sesión notifica la ranura que asigna XInput. Después de enviar el primer estado Xbox, escribe el LED correspondiente por HID. Si Windows tarda en asignar la ranura o falla la escritura, se reintenta en informes espaciados. La interfaz muestra la ranura en su actualización habitual.

## Instalación y distribución

`scripts/publish.ps1` verifica el SHA-256 y Authenticode del instalador oficial, lo incrusta en Wii2Xbox e incluye su licencia. Cuando el controlador falta o es incompatible, la app extrae y verifica el recurso, y solicita elevación a Windows solo al pulsar **Instalar controlador Xbox**. Espera a que termine el instalador sin bloquear la interfaz, comprueba el controlador y comunica el resultado. El instalador no fuerza el reinicio. `scripts/test-native.ps1` compila el observador XInput y ejecuta el comparador en el entorno de pruebas.

## Delivery Strategy

- Gate A: demostrar transporte Wii y salida virtual con hardware; resolver mantenimiento/licencia/firma del controlador. Si falla, registrar bloqueo técnico y evaluar un backend firmado equivalente sin reescribir mapeo/UI.
- Gate B: sesión robusta, cuatro mandos, watchdog, cancelación y pruebas automatizadas.
- Gate C: perfiles completos, calibración Nunchuk y reconexión/suspensión.
- Gate D: observador independiente, ensayos 1/4 mandos y optimización basada en percentiles.
- Gate E: paquete self-contained win-x64 con instalador firmado integrado, guía y validación en equipo sin SDK. Aceptación completa SC-001–SC-012.

No desarrollar un controlador kernel propio como atajo de este alcance. Una alternativa que exija ese trabajo necesita actualizar alcance/riesgos.

## Verification Strategy

Protocolo: fixtures de todos los IDs soportados, truncados, errores ACK, estados y bits de sensores. Mapeo: combinaciones y liberaciones, rotación horizontal, zonas muertas/rangos, varios orígenes a un destino. Sesión: timeout, pérdida, cancelación, error de salida, inicio parcial y cierre concurrente. Aislamiento: cuatro transportes y salidas falsas, 100.000 transiciones y 100 reconexiones.

Rendimiento: Release sin depurador, calentamiento de 30 s, ≥10 minutos y ≥10.000 informes/mando, 1 y 4 mandos, UI reposo/interacción/minimizada, diagnóstico apagado/encendido. Reportar muestras perdidas, GC, equipo y variación. Observador en otro proceso; XInput entrega estados, no una cola de eventos: el ensayo físico no afirma ver todas las pulsaciones arbitrariamente cortas. Comparaciones temporales usan marcas identificables y frecuencia/aliasing declarados. Prueba sintética de SC-004 valida el canal de envíos, no afirma que un juego observe cada transición.

Ensayo físico: adaptador/modelo/accesorio/Windows/driver/juego registrados, método de cámara o instrumento de entrada identificado, resolución temporal y tamaño de muestra suficientes para p99. Registrar limitaciones; nunca extrapolar percentiles sin muestras suficientes.

## Complexity Tracking

Sin desviaciones constitucionales aprobadas. Riesgos abiertos: backend retirado, variaciones de asociación por modelo, calibración de accesorios y objetivos aún no medidos. El esqueleto inicial no acredita ninguna historia completa.
