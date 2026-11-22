# Validación con Wii Remote físicos

Registra para cada ensayo la versión de Windows y Wii2Xbox, el adaptador Bluetooth, la versión de ViGEmBus, el modelo de cada mando y los accesorios. Conserva los informes JSON y apunta el juego en el que se comprueba la entrada.

Abre **Añadir mando…** en Wii2Xbox y pulsa SYNC en el mando mientras la ventana busca automáticamente. Asocia el mando y comprueba que se active en la lista principal. Verifica también que cerrar el asistente termine la búsqueda.

| Ensayo | Qué comprobar | Evidencia |
| --- | --- | --- |
| Asociación inicial | Wii Remote original y Wii Remote Plus. Encontrar el mando con SYNC, asociar, activar Xbox y comprobar entrada. Repetir diez veces por combinación | Tiempo desde la sincronización hasta la entrada y éxito de cada intento |
| Sesiones independientes | Activar cuatro mandos, mover stick y pulsar botones distintos, desconectar uno y continuar con los otros durante una hora | Ranuras y perfiles usados, anotación de interferencias y vídeo o registro del juego |
| Recuperación | Mientras mantienes una entrada, quita las pilas. Repite apagando Bluetooth, eligiendo **Salir** en el icono de Wii2Xbox y reanudando Windows | Tiempo hasta obtener neutro o desaparecer de XInput, y registro de las reconexiones |
| Nunchuk | Centro, recorrido completo y botones C/Z. Desenchufa el accesorio con el stick hacia un extremo | Ejes y botones en XInput antes y después de retirar el accesorio |
| Ratón IR | Calibrar con la barra en dos posiciones-objetivo, probar clic, arrastre y retirada de la barra con un clic sostenido. Repetir con Xbox activo y probar el modo exclusivo sin ViGEmBus si hay un equipo adecuado | Posición, desviación y comportamiento del clic al perder y recuperar la referencia |
| Latencia física | Filma la pulsación y la pantalla en el mismo encuadre durante una partida | Fotogramas por segundo de cámara y juego, y tiempo entre pulsación y respuesta |

