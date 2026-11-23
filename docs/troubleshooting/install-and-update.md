# Installation and Updates

[Troubleshooting index](../troubleshooting.md)

## The MSI or portable app does not open

1. Download the package again from the [official Wii2Xbox releases](https://github.com/Spontz/Wii2Xbox/releases). Use the MSI for an installed copy or the ZIP for a portable copy.
2. For the MSI, open **Wii2Xbox-win-x64.msi**, complete the setup wizard, and open **Wii2Xbox** from Start. If Windows Installer shows an error, note the full message or numeric code.
3. For the ZIP, right-click it, choose **Extract All**, and select a folder you can write to. Open that folder and run **Wii2Xbox.exe** with the extracted files alongside it.
4. If the app opens but reports a missing Xbox driver, follow [Xbox output and ViGEmBus](xbox-output.md#the-xbox-controller-is-missing-from-the-windows-test).

## An update did not finish

1. Check the version displayed in the bottom-right corner of the main window and compare it with the latest release for your package type. If Wii2Xbox reports that Windows needs to restart, save your work, restart Windows, and check the version again.
2. If **Restart to update** (**Reiniciar para actualizar**) is available in the notification-area menu, select it when you are ready for Wii2Xbox to close and reopen.
3. If you need to update manually, choose **Exit** (**Salir**) from that menu first. For an installed copy, run the newer MSI. For a portable copy, extract the new ZIP into a separate writable folder and run Wii2Xbox from there. Keep the existing folder until you have confirmed the new copy works.
4. If your portable copy was set to launch at sign-in, turn **Start with Windows** (**Arrancar al iniciar Windows**) off and back on in the new copy to update the startup path.
