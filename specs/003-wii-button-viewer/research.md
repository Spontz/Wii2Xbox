# Decisiones del visor

- Reutilizar el lector HID de cada sesión para obtener los botones Wii ya decodificados. Capturar después del envío Xbox habitual evita que el repintado o la agregación formen parte del camino de entrada.
- Conservar la hora del flanco de pulsación y el estado mantenido. Así se distinguen pulsaciones breves sin guardar una cola de informes.
- Usar una ilustración original de vista frontal horizontal con fondo transparente. La posición de la cruceta sigue la orientación física, con el extremo IR a la izquierda y una referencia aparte para B.
- Leer mandos sin salida disponible mediante el mismo tipo de sesión. Se evita una segunda apertura HID del mismo mando.

El recurso [WiiRemote.png](../../src/Wii2Xbox/Resources/WiiRemote.png) se creó con el generador de imágenes integrado. Se pidió un recorte fotorrealista de un Wii Remote blanco e inactivo, en vista frontal horizontal, con el extremo IR a la izquierda, controles y rótulos reconocibles, iluminación neutra y fondo transparente.

Después se pidió girar 90° en sentido antihorario solo las marcas de HOME, encendido, −, 1 y 2, conservando las posiciones de los botones y el lienzo transparente.

La revisión final pidió girar también la A, darle a su botón un acabado de policarbonato translúcido y retirar la primera y la última fila del altavoz para mostrar tres filas de cuatro orificios.
