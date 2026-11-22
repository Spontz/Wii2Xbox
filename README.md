# Wii2Xbox

Aplicación Windows en desarrollo para conectar hasta cuatro Wii Remote por Bluetooth y utilizarlos como mandos Xbox 360/XInput independientes. Prioridad: minimizar demora añadida y variabilidad, con mediciones verificables.

## Estado

Wii2Xbox enumera Wii Remote asociados en Windows y dirige cada sesión activa a una salida Xbox 360 mediante ViGEmBus. Admite hasta cuatro sesiones y perfiles por mando. La primera prueba física sigue pendiente.

- [Especificación completa](specs/001-wii-xbox-low-latency/spec.md)
- [Plan técnico](specs/001-wii-xbox-low-latency/plan.md)
- [Desarrollo y tareas](specs/001-wii-xbox-low-latency/tasks.md)
- [Ejecución en segundo plano y arranque con Windows](specs/002-background-startup/spec.md)
- [Visor de botones Wii Remote](specs/003-wii-button-viewer/spec.md)
- [Tema de Windows](specs/004-follow-windows-theme/spec.md)
- [Distribución portable y MSI](specs/005-portable-msi-distribution/spec.md)
- [Guía de desarrollo](specs/001-wii-xbox-low-latency/quickstart.md)
- [Constitución](.specify/memory/constitution.md)

El objetivo inicial de procesamiento es p95 ≤1 ms y p99 ≤2 ms. Es un criterio pendiente de aceptación; no representa la latencia Bluetooth ni la respuesta de un juego.

La versión para Windows incluye el instalador firmado de ViGEmBus 1.22.0. [Aviso de fin de soporte del mantenedor](https://docs.nefarius.at/projects/ViGEm/End-of-Life/)

## Probar la conversión

Consulta la [guía de instalación](docs/installation.md) para verificar ViGEmBus y probar la entrada.

1. Abre Wii2Xbox. Si aparece **Instalar controlador Xbox**, púlsalo y autoriza la instalación en Windows.
2. Pulsa **Añadir mando…**. La ventana busca automáticamente hasta que la cierras. Pon el Wii Remote en sincronización con SYNC o 1+2. Cuando aparezca, selecciónalo, elige los botones que has usado y pulsa **Asociar seleccionado**.
3. Wii2Xbox activa automáticamente los mandos disponibles en modo **Xbox**. La columna **Xbox** y el LED del Wii Remote muestran la ranura que asigna Windows.
4. Comprueba la entrada en un juego compatible con XInput o en el panel de mandos de Windows.

Tras perder Bluetooth o reanudar Windows, Wii2Xbox intenta recuperar cada mando activo hasta cuatro veces. Si se agotan los reintentos, vuelve a encender el mando para que Windows lo detecte. Windows puede asignar otra ranura Xbox al reconectar.

Al cerrar la ventana, Wii2Xbox sigue funcionando en el área de notificación. Abre el icono para volver a la ventana o elige **Salir** para liberar los mandos. Marca **Arrancar al iniciar Windows** si quieres que Wii2Xbox arranque en segundo plano cada vez que inicies sesión.

Debajo de la lista, el Wii Remote horizontal ilumina los botones físicos de todos los mandos que están enviando entrada. Un número sobre un botón indica cuántos mandos lo han pulsado a la vez. En Windows 11, Wii2Xbox usa el tema de aplicaciones de Windows al arrancar.

Para cambiar la salida, abre el menú **Salida** en la fila de ese mando. Puedes elegir **Xbox**, **Ratón IR** o **Xbox y ratón IR**.

Consulta la [guía de solución de problemas](docs/troubleshooting.md) y el [protocolo de validación con mandos físicos](docs/hardware-validation.md).

Selecciona un mando y pulsa **Perfil…** para elegir orientación, reasignar sus botones e importar o exportar un perfil JSON. **Guardar** identifica el mando por su dirección Bluetooth cuando Windows la expone, o por su ruta HID como alternativa. Si hay una sesión activa, el cambio se aplica al recibir el siguiente informe de entrada.

Al conectar un Nunchuk reconocido, su stick controla el stick izquierdo de Xbox y C/Z usan las asignaciones del perfil. **Zona muerta** ajusta la tolerancia alrededor del centro. La carpeta `profiles` incluye ejemplos para Wii Remote horizontal y Wii Remote con Nunchuk.

## Usar la barra de Wii como ratón

1. Centra la barra IR cerca de la pantalla principal y aliméntala con su fuente compatible. Una barra original puede seguir conectada a una Wii encendida. Las barras USB y a pilas se alimentan según su modelo.
2. Selecciona el Wii Remote y elige **Ratón IR** o **Xbox y ratón IR**. El cambio se aplica a ese mando. Para usar solo el ratón puedes prescindir de ViGEmBus.
3. Sostén el mando en vertical y apunta a la pantalla. El mando necesita ver los dos extremos de la barra. **A** hace clic izquierdo y **B** hace clic derecho. Para liberar el ratón, cambia la salida del mando a **Xbox**.

La barra aporta luz infrarroja. Se conecta por Bluetooth el Wii Remote, que lleva la cámara. Solo una sesión controla el cursor a la vez. Para ajustar la posición de la barra o la distancia, pulsa **Calibrar IR…** o **Alt+C** con el modo ratón activo. Apunta a cada cruz de la pantalla principal y pulsa **A**. El ajuste se guarda con el perfil del mando. **Esc** cancela y **R** restaura los valores de fábrica.

La asociación se ha comprobado hasta la búsqueda con un adaptador real. El Nunchuk y el ratón IR se han probado con informes simulados. Quedan las pruebas con hardware real y las mediciones en juegos.

## Generar los paquetes

En Windows, con el SDK de `global.json`, ejecuta `./scripts/package.ps1 -Version 1.0.0`. Genera `artifacts/Wii2Xbox-win-x64.zip` y `artifacts/Wii2Xbox-win-x64.msi` desde la misma compilación. El proyecto del MSI restaura WiX desde NuGet. Usa `scripts/test.ps1` para ejecutar las pruebas.

## Licencia

El código de Wii2Xbox se distribuye bajo [Apache-2.0](LICENSE). La atribución a Iván Fernández figura en [NOTICE](NOTICE). Los avisos de componentes de terceros se entregan en la carpeta `licenses` de ambos paquetes.
