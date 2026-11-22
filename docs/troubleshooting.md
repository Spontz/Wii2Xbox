# Bluetooth Connection

- If Windows cannot find a Bluetooth Classic adapter, check that Bluetooth is on, the adapter appears in Device Manager, and its driver is installed.
- If the list is empty, press SYNC next to the batteries and wait while the LEDs flash. The wizard continues searching while open. To use 1+2, select that method before pairing.
- The wizard pairs Wii Remotes that advertise as `Nintendo RVL-CNT-01` or `Nintendo RVL-CNT-01-TR`. Other recognized Wii devices, including the Balance Board, appear as **no compatible** (unsupported). Some third-party controllers may advertise the same name. [XWiimote device names](https://xwiimote.github.io/xwiimote/index.html)
- If pairing fails, put the controller back in sync mode and select the corresponding method before trying again.
- After pairing, the app waits for Windows to expose the HID interface and automatically lists the controller. If it can match the Bluetooth address, it selects the controller and shows the last four digits. If it takes a while to appear, check that the controller is still on.
- If the game receives no input, select Xbox output for that controller and check that ViGEmBus is installed. Check its output with `joy.cpl`. Xbox controllers already connected to the PC share the same system slots.
- If XInput sees the controller and the game does not, check the game's controller settings and its documented support for virtual devices. Record Windows XInput results and individual game results separately.

## Check Xbox Output

The **Xbox** column shows the slot Windows assigned to the Wii Remote. To check the buttons and axes, open `joy.cpl`, select the virtual controller, and choose **Properties**.
