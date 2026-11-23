# Install Wii2Xbox

- **Portable ZIP**. Extract `Wii2Xbox-win-x64.zip` to a folder and run `Wii2Xbox.exe`. Keep the executable and its files together.
- **MSI installer**. Open `Wii2Xbox-win-x64.msi` and follow the setup wizard. When it confirms installation is complete, select **Finalizar** (Finish) and open **Wii2Xbox** from Start. Before updating or uninstalling, select **Salir** (Exit) from the notification-area icon. Uninstall from **Settings → Apps → Installed apps → Wii2Xbox**. User profiles and ViGEmBus are retained.

Both packages include .NET and run as a standard user on Windows x64. Turn on Bluetooth Classic and use **Añadir mando…** (Add controller…) to discover controllers.

**Xbox** and **Xbox y ratón IR** (Xbox and IR mouse) require ViGEmBus. Wii2Xbox embeds the signed 1.22.0 installer and can run it offline. **Ratón IR** (IR mouse) works without this driver. [Maintainer's release](https://github.com/nefarius/ViGEmBus/releases/tag/v1.22.0) · [End of support](https://docs.nefarius.at/projects/ViGEm/End-of-Life/)

## Check the Virtual Driver

Wii2Xbox checks the driver when it opens. If the driver is missing or incompatible, Wii2Xbox offers to install it. If you accept, allow the installation in the Windows prompt. Wii2Xbox checks the result when installation finishes. If the app requests a restart, restart Windows and reopen it.

When Wii2Xbox shows a slot in the **Xbox** column, follow the [Windows controller test](troubleshooting/xbox-output.md#test-the-controller-in-windows) to check its buttons and axes. For setup or update errors, see [Installation and updates](troubleshooting/install-and-update.md).

Both packages include `LICENSE`, `NOTICE`, and the ViGEmBus, HidSharp, ViGEm.NET, ViGEmClient, and .NET notices in `licenses`.
