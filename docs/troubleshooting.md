# Troubleshoot Wii2Xbox

Use this guide if you have trouble connecting a Wii Remote, using it as an Xbox controller or an IR mouse, or installing or updating Wii2Xbox. Start with the symptom that matches what you see.

| Symptom | Guide |
| --- | --- |
| Wii2Xbox cannot find, pair with, or reconnect to the Wii Remote | [Bluetooth and pairing](troubleshooting/bluetooth.md) |
| The Wii Remote is listed, but Windows, a game, or a browser tester does not respond | [Xbox output and ViGEmBus](troubleshooting/xbox-output.md) |
| The pointer does not move, jumps, or misses the edges of the screen | [IR mouse and sensor bar](troubleshooting/ir-mouse.md) |
| The MSI or portable app will not open, or an update did not finish | [Installation and updates](troubleshooting/install-and-update.md) |

## Check Xbox Output

To see whether Windows receives input from Wii2Xbox, press **Windows + R**, type `joy.cpl`, and press **Enter**. In **Game Controllers**, select the virtual Xbox 360 controller, open **Properties → Test**, and press buttons on the Wii Remote. [Full Windows test and what to do with the result](troubleshooting/xbox-output.md#test-the-controller-in-windows).

## Get More Help

If the problem continues, [open a GitHub issue](https://github.com/Spontz/Wii2Xbox/issues). Include the Wii2Xbox version shown at the bottom right of the main window, your Windows version, whether you use the MSI or portable ZIP, and the exact steps and error message. For input problems, report whether the Wii Remote preview lights up and whether the input appears in the Windows test.
