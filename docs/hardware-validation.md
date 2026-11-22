# Test with Physical Wii Remotes

For each run, record the Windows and Wii2Xbox versions, Bluetooth adapter, ViGEmBus version, controller models, and accessories. Keep the JSON reports and note the game used to check input.

Open **Añadir mando…** (Add controller…) in Wii2Xbox and press SYNC on the controller while the window searches. Pair the controller and verify it becomes active in the main list. Verify that closing the wizard stops discovery.

| Test | What to check | Evidence |
| --- | --- | --- |
| Initial pairing | Original Wii Remote and Wii Remote Plus. Discover the controller with SYNC, pair it, activate Xbox output, and check input. Repeat ten times per combination | Time from synchronization to input and result of each attempt |
| Independent sessions | Activate four controllers, move the stick and press different buttons, disconnect one, and continue with the others for one hour | Slots and profiles used, any interference, and video or a game log |
| Recovery | Remove the batteries while holding an input. Repeat by turning off Bluetooth, selecting **Salir** (Exit) from the Wii2Xbox icon, and resuming Windows | Time until XInput goes neutral or loses the slot, and reconnection log |
| Nunchuk | Check the center, full stick range, and C/Z buttons. Unplug the accessory while holding the stick at an extreme | XInput axes and buttons before and after removal |
| IR mouse | Calibrate with the bar in two positions. Test clicks, dragging, and removing the bar while holding a click. Repeat with Xbox active and test mouse-only mode without ViGEmBus on a suitable PC | Position, deviation, and click behavior when tracking is lost and recovered |
| Physical latency | Film the button press and display in the same frame during a game | Camera and game frame rates, and time from press to response |
