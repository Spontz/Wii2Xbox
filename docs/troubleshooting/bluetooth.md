# Bluetooth and Pairing

[Troubleshooting index](../troubleshooting.md)

## Wii2Xbox cannot find a Bluetooth adapter

1. In Windows 11, open **Start → Settings → Bluetooth & devices** and turn on **Bluetooth**. In Windows 10, use **Start → Settings → Devices → Bluetooth & other devices**.
2. If Bluetooth is missing or cannot be enabled, right-click **Start**, choose **Device Manager**, and expand **Bluetooth**.
3. If the adapter is missing, check that an external adapter is plugged in and supports Bluetooth Classic. If the adapter appears with a warning icon, open its **Properties** and read the **Device status** message. Use the PC or adapter manufacturer's Bluetooth driver if Windows cannot supply a working one. [Microsoft's Bluetooth troubleshooting guide](https://support.microsoft.com/en-us/windows/hardware/bluetooth/fix-bluetooth-problems-in-windows)

## The Wii Remote does not appear or fails to pair

1. Put charged batteries in the Wii Remote and keep it near the PC.
2. Open Wii2Xbox and choose **Add controller…** (**Añadir mando…**). The window searches automatically while it is open.
3. Press the red **SYNC** button inside the battery compartment, or hold **1 + 2** until the LEDs flash. If they stop flashing before the controller appears, start pairing mode again.
4. Select the Wii Remote in the list. Set the method to **SYNC** if you pressed the red button, or **1+2** if you held those two buttons. Choose **Pair selected** (**Asociar seleccionado**).
5. If pairing fails, put the Wii Remote back into pairing mode and retry with the matching method. If Windows still has a stale entry for that remote, remove that entry under **Settings → Bluetooth & devices → Devices** in Windows 11 or **Settings → Devices → Bluetooth & other devices** in Windows 10, then repeat from step 2.

The pairing window supports Wii Remotes that advertise as `Nintendo RVL-CNT-01` or `Nintendo RVL-CNT-01-TR`. Other detected Wii devices, including the Balance Board, can appear as unsupported. Some third-party remotes use the same names. [XWiimote device names](https://xwiimote.github.io/xwiimote/index.html)

## Pairing succeeded, but the main window is empty

1. Keep the Wii Remote on while Windows makes it available to apps. Wii2Xbox adds it to the main list as soon as Windows exposes its HID interface.
2. If the LEDs turn off and the remote stays missing, press a button to wake it and check the list again.
3. Once a row appears, press a button and check whether it lights up in the Wii Remote preview. If the preview responds, continue with [Xbox output](xbox-output.md) or the [IR mouse](ir-mouse.md).
