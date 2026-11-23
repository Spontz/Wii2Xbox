# Xbox Output and ViGEmBus

[Troubleshooting index](../troubleshooting.md)

## Test the controller in Windows

1. Open Wii2Xbox and leave it running. The Wii Remote should be visible in the controller list. If it is missing, follow [Bluetooth and pairing](bluetooth.md).
2. In that controller's **Output** (**Salida**) column, click the drop-down and select **Xbox** or **Xbox and IR mouse** (**Xbox y ratón IR**). The **Xbox** column should show the slot assigned by Windows.
3. Press **Windows + R** to open **Run**. Type `joy.cpl` and press **Enter** to open **Game Controllers**.
4. Select the virtual controller, usually listed as **Controller (Xbox 360 For Windows)**, and choose **Properties → Test**. If several Xbox controllers have the same name, test one Wii Remote at a time and check each entry for a response. The order in the Windows panel can differ from the slots shown in Wii2Xbox.
5. Press and release a mapped button on the Wii Remote. Its indicator in the **Test** tab should turn on and then off. If you use a Nunchuk, move the stick and check that the axes move and return to center. [Microsoft's game controller testing guide](https://learn.microsoft.com/en-us/windows-hardware/test/hlk/testref/game-controller-testing-prerequisites)

## The Xbox controller is missing from the Windows test

1. In Wii2Xbox, check that the remote uses **Xbox** or **Xbox and IR mouse**. The **IR mouse** output controls the pointer and mouse clicks.
2. If Wii2Xbox says ViGEmBus is missing or incompatible, select **Exit** (**Salir**) from its notification-area icon and reopen the app. Accept the driver installation when Wii2Xbox offers it, then allow it in the Windows prompt. Restart Windows if Wii2Xbox asks you to.
3. If all four XInput slots are occupied, disconnect an unused physical Xbox controller or close another app that creates virtual Xbox controllers. Check the Wii2Xbox **Xbox** column again, then reopen `joy.cpl`.

## The virtual controller appears, but a button does not respond

1. Press the same button on the Wii Remote and watch the Wii2Xbox preview. If the preview does not respond, return to [Bluetooth and pairing](bluetooth.md).
2. If the preview lights up, select the Wii Remote and open **Profile…** (**Perfil…**) to check its mapping. Choose **Save** (**Guardar**) after making changes, then repeat the Windows test.

## Windows shows input, but a game or browser does not

1. Leave the Wii Remote active in Wii2Xbox, close the game completely, and reopen it. In the game's input settings, enable the gamepad and choose the controller or player slot if the game offers that option.
2. If it fails in that game, try another game with Xbox controller support. Record the title and whether the Windows test and the second game respond.
3. If only a browser tester fails, reload its tab, click inside the page, and press a mapped Xbox face button. Try another browser if it remains blank. Use the result from `joy.cpl` to tell whether Windows received the input separately from what the site shows.
