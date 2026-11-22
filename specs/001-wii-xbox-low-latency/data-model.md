# Modelo de datos y ciclo de vida

## PhysicalController

`Id` estable cuando transporte lo permita; `DevicePath` efímero; modelo; radio; capacidades; accesorio identificado; último informe válido monotónico; estado. No usar índice de lista como identidad. Si la identidad cambia, no asociar automáticamente perfiles de otro mando.

## ControllerSession

`SessionId`, `PhysicalId`, salida propia, perfil snapshot, estado, generación de conexión, diagnóstico acotado y cancelación. Restricciones: una sesión no finalizada por físico, una propietaria por salida, máximo cuatro activas y nunca dos lectores del mismo dispositivo.

Estados: Discovered → Connecting → Configuring → Active → Stopping → Disconnected. Fallo de cualquier etapa → limpieza → Faulted. Reintento crea nueva generación y no reutiliza callbacks de una anterior. Limpieza idempotente, incluso tras apertura parcial o lectura bloqueada.

## WiiState / XboxState

Wii: conjunto de botones, orientación del perfil, accesorio válido, stick calibrado C/Z cuando corresponda y secuencia local. Xbox: máscara de botones, dos pares de ejes con signo y dos gatillos 0–255. Los componentes no usados siempre quedan neutrales. Informes desconocidos/truncados no actualizan botones y no cuentan como datos válidos para el watchdog.

## ProfileDocument v1

`schemaVersion: 1`, `id`, `name`, orientación `horizontal|upright`, asignaciones fuente→destino, calibración centro/mínimo/máximo por eje Nunchuk y zona muerta. Fuentes/destinos de catálogo cerrado. Varias fuentes al mismo destino se combinan OR; liberar una no libera el destino si otra sigue activa. Direcciones opuestas se neutralizan. Valores fuera de rango, calibración degenerada y versión desconocida se rechazan antes de aplicar.

Guardado temporal y reemplazo atómico, conservando perfil válido. Migraciones explícitas. El tipo Profile exploratorio de Input.cs aún no implementa este contrato completo.

## ConfigurationDocument v1

Perfiles conocidos y última asociación físico→perfil; preferencias UI; diagnóstico habilitado. No persistir estado pulsado ni índice virtual garantizado. Archivos locales en directorio del usuario.

## PerformanceRun

Versión/build, SO/runtime, CPU, radio, mandos/accesorios, backend/driver, juego si aplica, modo de informes, condiciones, duración, calentamiento, tamaño de muestra, frecuencia del observador, incertidumbre, pérdidas, p50/p95/p99/max. Tipos separados `software-processing`, `virtual-observed`, `physical-to-response`. Muestras en microsegundos o ticks con frecuencia declarada. No combinar métricas incompatibles.
