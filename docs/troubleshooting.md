# Conexión Bluetooth

- Si Windows no encuentra un adaptador Bluetooth Classic, comprueba que Bluetooth esté activado, que el adaptador aparezca en el Administrador de dispositivos y que su controlador esté instalado.
- Si la lista está vacía, pulsa SYNC junto a las pilas y espera mientras parpadean las luces. El asistente sigue buscando mientras está abierto. Para usar 1+2, selecciona ese método antes de asociar.
- El asistente asocia Wii Remote que se anuncian como `Nintendo RVL-CNT-01` o `Nintendo RVL-CNT-01-TR`. Otros modelos Wii reconocibles aparecen como **no compatible**, incluida la Balance Board. El nombre puede coincidir en algunos mandos de terceros. Su funcionamiento queda por verificar. [Nombres de dispositivos en XWiimote](https://xwiimote.github.io/xwiimote/index.html)
- Si la asociación falla, vuelve a poner el mando en sincronización y elige el método correspondiente antes de asociarlo.
- Tras asociar el mando, la aplicación espera a que Windows publique su interfaz HID y lo muestra automáticamente. Si puede cotejar la dirección Bluetooth, selecciona el mando y muestra sus cuatro últimos dígitos. Si tarda en aparecer, comprueba que siga encendido.
- Si el juego carece de entrada, activa Xbox para ese mando y verifica que el controlador ViGEmBus esté instalado. Comprueba la salida en el panel de mandos con `joy.cpl`. Los mandos Xbox conectados al PC comparten las ranuras del sistema.
- Si XInput muestra correctamente el mando pero un juego no lo acepta, revisa la configuración de mandos y la compatibilidad con dispositivos virtuales que documente ese juego. La prueba de XInput verifica Windows. La aceptación de cada juego se registra por separado.

## Comprobar la salida Xbox

La columna **Xbox** muestra la ranura que Windows asigna al Wii Remote. Para comprobar los botones y ejes, abre `joy.cpl`, selecciona el mando virtual y pulsa **Propiedades**.

