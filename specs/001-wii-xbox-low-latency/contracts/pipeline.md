# Contratos internos previstos

Sin API de red. Los nombres expresan fronteras; se concretan al implementar.

## IControllerTransport

- Enumerar devuelve identidad/modelo/capacidades sin abrir doble lector.
- Abrir/configurar acepta cancelación y produce error tipado.
- Leer escribe en buffer reutilizable; devuelve longitud real y timestamp de disponibilidad, no texto ni UI.
- Respuestas ACK/estado/memoria se separan de informes de entrada.
- Cerrar cancela lectura pendiente y libera handle; repetición segura.

## IXboxOutput

- Conectar crea una única salida propia; devuelve índice real si está disponible.
- Submit recibe estado completo y realiza un envío, no uno por cada campo.
- Neutralize envía ceros; Disconnect libera solo esa salida incluso si Neutralize falla.
- Fallo se propaga a sesión; no continúa afirmando estar activa.

## SessionManager

- Start(physicalId, profile) es exclusivo por dispositivo y respeta capacidad disponible.
- Stop(sessionId) / StopAll son idempotentes.
- ApplyProfile valida primero; neutraliza estado anterior en frontera segura, sin trabajo de disco en lector.
- Estados y snapshots de UI no bloquean transporte. Un error de una sesión no cancela las demás.

## BluetoothDiscovery

- Search/Connect/Cancel exponen causa de fallo y progreso real.
- Cancelación completa ≤5 s y no toca otras sesiones ni asociaciones.
- Filtrar candidatos por modelo/identidad; nunca conectar automáticamente cualquier dispositivo encontrado.

## Diagnostics

- Registro de timestamp acotado, sin I/O por muestra; procesamiento separado del cálculo de percentiles/exportación.
- Diagnóstico apagado elimina la captura opcional; UI se limita a snapshots.
- Informe exportado incluye versión de esquema y condiciones, sin atribuir procesamiento a latencia física.

## Profile storage

- Load/Import valida esquema, catálogo y rangos antes de devolver snapshot.
- Save no deja archivo parcial; falla sin destruir configuración anterior.
- Archivo no válido devuelve causa/ubicación, conserva último perfil válido.
