# Bluetooth and Pairing

[Troubleshooting index](../troubleshooting.md)

To remove a Wii Remote, click the red **X** at the end of its row in the main window. Wii2Xbox stops that controller and removes its saved Bluetooth pairing from Windows when present. The controller's profile is retained.

## Wii2Xbox cannot find a Bluetooth adapter

1. In Windows 11, open **Start → Settings → Bluetooth & devices** and turn on **Bluetooth**. In Windows 10, use **Start → Settings → Devices → Bluetooth & other devices**.
2. If Bluetooth is missing or cannot be enabled, right-click **Start**, choose **Device Manager**, and expand **Bluetooth**.
3. If the adapter is missing, check that an external adapter is plugged in and supports Bluetooth Classic. If the adapter appears with a warning icon, open its **Properties** and read the **Device status** message. Use the PC or adapter manufacturer's Bluetooth driver if Windows cannot supply a working one. [Microsoft's Bluetooth troubleshooting guide](https://support.microsoft.com/en-us/windows/hardware/bluetooth/fix-bluetooth-problems-in-windows)

## The Wii Remote does not appear or fails to pair

1. Put charged batteries in the Wii Remote and keep it near the PC.
2. Open Wii2Xbox and choose **Add controller…** (**Añadir mando…**). The window searches automatically while it is open.
3. Press the red **SYNC** button inside the battery compartment. If the LEDs stop flashing before the controller appears, press **SYNC** again.
4. Select the Wii Remote in the list and choose **Pair selected** (**Asociar seleccionado**).
5. If pairing fails, press the red **SYNC** button again and retry. If Windows still has a stale entry for that remote, remove that entry under **Settings → Bluetooth & devices → Devices** in Windows 11 or **Settings → Devices → Bluetooth & other devices** in Windows 10, then repeat from step 2.

The pairing window supports Wii Remotes that advertise as `Nintendo RVL-CNT-01` or `Nintendo RVL-CNT-01-TR`. Other detected Wii devices, including the Balance Board, can appear as unsupported. Some third-party remotes use the same names. [XWiimote device names](https://xwiimote.github.io/xwiimote/index.html)

## Windows remembers the Wii Remote, but the main window is empty

1. Keep the Wii Remote on while Windows makes it available to apps. **Add controller…** stays open until Wii2Xbox can see the selected remote in the main list.
2. If Windows still reports the selected remote as disconnected and its input interface has not appeared, Wii2Xbox can remove its old pairing and immediately try pairing the same remote again. Keep the wizard open during the retry. If the retry fails and the LEDs have stopped flashing, press **SYNC** and try again.
3. If the LEDs turn off and the remote stays missing, press a button to wake it and check the list again. Once the row appears, press a button and check whether it lights up in the Wii Remote preview.
4. If the preview responds, continue with [Xbox output](xbox-output.md) or the [IR mouse](ir-mouse.md).
