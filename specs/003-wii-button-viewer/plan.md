# Plan del visor de botones Wii Remote

**Especificación** [spec.md](spec.md)

## Contexto técnico

Wii2Xbox usa WinForms sobre .NET 10 y un lector HID por mando. El visor reutiliza la máscara Wii que ese lector ya decodifica. El plazo de 120 ms para pulsaciones breves afecta a la pantalla. El envío Xbox conserva su propia cadencia.

## Comprobación de la constitución

Los lectores mantienen la propiedad de sus sesiones. Después de entregar la entrada, publican un estado pequeño sin acceso a la interfaz. Un temporizador de la ventana combina las lecturas y repinta si cambian. Los modos de espera usan la misma ruta de lectura y respetan la capacidad de Xbox y del ratón.

## Diseño y verificación

- ControllerSession publica la entrada a WiiButtonState y distingue la sesión sin salida.
- MainForm sitúa Arrancar al iniciar Windows debajo de la lista y encima de WiiRemoteView. El ancho de las cuatro columnas se recalcula al cambiar el área útil.
- WiiRemoteView usa un PNG incrustado y superpone la iluminación sobre las posiciones de los botones físicos. Combina entradas por botón y señala las pulsadas por varias sesiones.
- Cada fila abre el menú **Salida** anclado en su celda. El menú conserva el identificador del mando al que se aplica la elección.
- Probar flancos breves, entradas simultáneas, liberación, arranque de una sesión sin salida y la geometría de la lista.
