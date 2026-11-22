# Wii2Xbox

A Windows app that connects up to four Wii Remotes over Bluetooth and lets you use them as independent Xbox 360/XInput controllers. Designed for minimal latency.

## Downloads

- [MSI installer](https://github.com/Spontz/Wii2Xbox/releases/tag/v1.0.1-installer)
- [Portable ZIP](https://github.com/Spontz/Wii2Xbox/releases/tag/v1.0.1-portable)

## Project Documentation

Wii2Xbox lists Wii Remotes paired with Windows and sends input from each active session to an Xbox 360 output through ViGEmBus. It supports up to four sessions and per-controller profiles.

- [Full specification](specs/001-wii-xbox-low-latency/spec.md)
- [Technical plan](specs/001-wii-xbox-low-latency/plan.md)
- [Development tasks](specs/001-wii-xbox-low-latency/tasks.md)
- [Background operation and Windows startup](specs/002-background-startup/spec.md)
- [Wii Remote button viewer](specs/003-wii-button-viewer/spec.md)
- [Windows theme](specs/004-follow-windows-theme/spec.md)
- [Portable ZIP and MSI distribution](specs/005-portable-msi-distribution/spec.md)
- [Development quickstart](specs/001-wii-xbox-low-latency/quickstart.md)
- [Constitution](.specify/memory/constitution.md)

The Windows packages include the signed ViGEmBus 1.22.0 installer. [Maintainer's end-of-life notice](https://docs.nefarius.at/projects/ViGEm/End-of-Life/)

## Use Your Wii Remote as an Xbox Controller

See the [installation guide](docs/installation.md) for setup and input checks.

1. Open Wii2Xbox. If **Instalar controlador Xbox** (Install Xbox driver) appears, select it and authorize the installation in Windows.
2. Select **Añadir mando…** (Add controller…). The window searches until you close it. Put your Wii Remote into sync mode with SYNC or 1+2. When it appears, select it, choose the buttons you used, and select **Asociar seleccionado** (Pair selected).
3. Wii2Xbox automatically activates available controllers in **Xbox** mode. The **Xbox** column and the Wii Remote LED show the slot assigned by Windows.
4. Check input in an XInput-compatible game or in the Windows Game Controllers panel.

After Bluetooth disconnects or Windows resumes, Wii2Xbox tries to recover each active controller up to four times. If it runs out of retries, switch the controller on again so Windows can detect it. Windows may assign a different Xbox slot when the controller reconnects.

Closing the window keeps Wii2Xbox running in the notification area. Open its icon to restore the window or select **Salir** (Exit) to release the controllers. Enable **Arrancar al iniciar Windows** (Start with Windows) to run Wii2Xbox in the background whenever you sign in.

The horizontal Wii Remote below the list lights up buttons from all controllers sending input. A number over a button indicates how many controllers are pressing it at once. On Windows 11, Wii2Xbox follows the Windows app theme at startup.

To change a controller's output, open **Salida** (Output) in its row. Choose **Xbox**, **Ratón IR** (IR mouse), or **Xbox y ratón IR** (Xbox and IR mouse).

See the [troubleshooting guide](docs/troubleshooting.md) and [physical controller test protocol](docs/hardware-validation.md).

Select a controller and choose **Perfil…** (Profile…) to set its orientation, remap buttons, or import or export a JSON profile. **Guardar** (Save) identifies the controller by its Bluetooth address when available and falls back to its HID path. Active sessions apply the change on the next input report.

With a recognized Nunchuk connected, its stick controls the Xbox left stick and C/Z follow the profile mappings. **Zona muerta** (Dead zone) adjusts the tolerance around the center. The `profiles` folder contains example profiles for horizontal Wii Remote and Wii Remote with Nunchuk.

## Use the Wii Sensor Bar as a Mouse

1. Center the IR bar near your primary display and power it with a compatible source. You can leave an original bar connected to a powered-on Wii. Power USB or battery models as directed by their manufacturer.
2. Select the Wii Remote and choose **Ratón IR** (IR mouse) or **Xbox y ratón IR** (Xbox and IR mouse). The change applies to that controller. IR mouse-only mode works without ViGEmBus.
3. Hold the Wii Remote upright and point it at the display. It needs a clear view of both ends of the bar. **A** left-clicks and **B** right-clicks. To release mouse control, change the controller's output to **Xbox**.

The sensor bar emits infrared light. The Wii Remote contains the camera and connects over Bluetooth. One session can control the cursor at a time. To adjust for the bar's position or your distance from the display, select **Calibrar IR…** (Calibrate IR…) or press **Alt+C** with mouse mode active. Aim at each crosshair on the primary display and press **A**. The calibration is saved with the controller profile. **Esc** cancels and **R** resets to factory settings.

## License

Wii2Xbox source code is licensed under [Apache-2.0](LICENSE). [NOTICE](NOTICE) credits Iván Fernández. Both packages include third-party notices in the `licenses` folder.
