# Wii Remote Button Viewer Specification

**Created** 2026-11-22  
**Application** Wii2Xbox for Windows

[Plan](plan.md) · [Tasks and evidence](tasks.md)

## User Scenarios and Tests

### US1. Check physical controller input (P1)

As a player, I want to see which buttons Wii2Xbox recognizes before trying a game.

**Independent test**. Turn on a Wii Remote and press A, B, 1, 2, −, +, HOME, and each direction. Verify that the corresponding position lights up even with a profile that remaps the button.

### US2. Test several controllers at once (P1)

As a player, I want to press buttons on different controllers and see their inputs at the same time.

**Independent test**. Hold A on one controller and 2 on another. Press the same button on both. Release or disconnect one and confirm that the other controller's button remains lit.

### US3. Read the controller list clearly (P2)

As a player, I want to find the Xbox number in its own column and read each session's status separately.

**Independent test**. With a Wii Remote assigned to Xbox 2, verify that its basic status is **Activo** (Active) and the **Xbox** column shows **2**. Resize the window and verify that the four columns fill the list.

### Edge Cases

- A brief press must be visible even if the button is released between repaints.
- A disconnected Wii Remote stops contributing to the viewer. Other button presses retain their state.
- Power and SYNC are for turning on and connecting the controller. The viewer displays the inputs the controller reports to the app.

## Requirements

- **FR-001**. Show a realistic horizontal Wii Remote illustration at the bottom with A, B, 1, 2, −, +, HOME, and the four directions. Represent the rear B trigger beside the front view. Keep all labels and messages above or beside the illustration.
- **FR-002**. Highlight buttons based on physical input regardless of the profile or selected row.
- **FR-003**. Combine presses from all controllers sending reports. Indicate when several controllers hold the same button and clear a controller's contribution when it disconnects.
- **FR-004**. Keep button testing available for readable controllers waiting on the Xbox driver, a slot, or the IR mouse.
- **FR-005**. Update the list automatically and distribute its width across **Mando** (Controller), **Xbox**, **Estado** (Status), **Perfil** (Profile), and **Salida** (Output). Use **Activo** (Active) as the basic status and show the slot only in **Xbox**.
- **FR-006**. Place **Arrancar al iniciar Windows** (Start with Windows) left-aligned between the list and the viewer.
- **FR-007**. Open the output menu from the controller's row and apply the change only to that controller.
- **FR-008**. Place **Invítame a un café** (Buy me a coffee) above the list, aligned to the right.
- **FR-009**. Light the player indicator in blue while that controller presses a button. Show indicators from multiple controllers at once and assign free indicators to controllers without an Xbox slot. Omit the numbers beside the illustration's player indicators.

## Success Criteria

- **SC-001**. Each of the eleven inputs lights up when pressed and goes dark after release or disconnection.
- **SC-002**. Two controllers display distinct and shared buttons in the same view. Captured short presses remain visible long enough to distinguish them.
- **SC-003**. The viewer works when resized and with sessions in Xbox, IR mouse, or read-only mode. Status and slot remain separate.
