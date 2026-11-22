# Wii Remote Button Viewer Plan

**Specification** [spec.md](spec.md)

## Technical Context

Wii2Xbox uses WinForms on .NET 10 and one HID reader per controller. The viewer reuses the Wii button mask that reader already decodes. The 120 ms display period keeps short presses visible. Xbox output keeps its own timing.

## Constitution Check

Readers retain ownership of their sessions. After delivering input, they publish a small state snapshot without accessing the UI. A window timer combines the snapshots and repaints on changes. Waiting modes use the same read path and honor Xbox and IR mouse capacity limits.

## Design and Verification

- `ControllerSession` publishes input to `WiiButtonState` and identifies sessions without output.
- `MainForm` places **Arrancar al iniciar Windows** (Start with Windows) below the list and above `WiiRemoteView`. The four column widths are recalculated when the available area changes.
- `WiiRemoteView` uses an embedded PNG and draws highlights over the physical button positions. It combines input per button and indicates when multiple sessions press the same button.
- Each row opens its **Salida** (Output) menu anchored to the cell. The menu retains the ID of the controller to which the choice applies.
- Test short press edges, simultaneous input, release, starting a session without output, and the list layout.
