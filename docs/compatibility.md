# Estado de compatibilidad

| Componente | Verificación | Resultado |
| --- | --- | --- |
| Paquete Windows x64 | Arranque y cierre en el equipo de desarrollo | Correcto |
| ViGEmBus 1.22.0 | Instalador de la publicación oficial, firma Authenticode | Válida. Firmante Nefarius Software Solutions e.U. |
| ViGEmBus ausente o incompatible | Comprobación de la salida Xbox dentro de la aplicación | Mensajes verificados con errores simulados. Pendiente en un equipo sin controlador |
| ViGEmBus presente en el equipo | Windows 26300.10075, controlador 1.17.333.0, servicio Bigscreen Gamepad Emulation | Una y cuatro salidas correctas, con botones, ejes, gatillos, liberación y desconexión observados mediante XInput |
| Cien ciclos de salida virtual | 25 tandas de cuatro mandos virtuales | Correcto. Tras el ensayo, las cuatro ranuras quedaron libres |
| ZIP autónomo | Copia recién extraída, proceso con PATH de sistema y rutas .NET inexistentes, usuario sin elevación | Autoprueba de cuatro mandos correcta |
| Wii Remote y Wii Remote Plus | Asociación Bluetooth, juego y reconexión | Pendiente con mandos físicos |
| Nunchuk y barra IR | Simulación de informes | Correcto en pruebas automatizadas. Hardware pendiente |

El instalador verificado fue `ViGEmBus_1.22.0_x64_x86_arm64.exe`, SHA-256 `89220A7865076B342892F98865F3499FB7C4CFD673159E89D352C360FD014C6A`. La comprobación corresponde a la firma del instalador. La instalación y el funcionamiento del controlador quedan por verificar en los equipos de destino. [Publicación oficial](https://github.com/nefarius/ViGEmBus/releases/tag/v1.22.0)

La integración se ejecutó con el controlador ya presente. Su archivo `ViGEmBus.sys` tiene firma Authenticode válida de Microsoft Windows Hardware Compatibility Publisher. Queda pendiente repetir la prueba tras una instalación de 1.22.0 en un equipo limpio.
