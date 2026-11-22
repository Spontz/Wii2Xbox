# Wii Remote Button Viewer Tasks

- [x] T001 Capture physical buttons and press edges per session, with tests for brief presses.
- [x] T002 Draw and combine session states in the horizontal viewer.
- [x] T003 Integrate the viewer, place the startup checkbox above it, and allow input reads when output is unavailable.
- [x] T004 Separate status and slot. Remove the search button and verify column width distribution.
- [x] T005 Build Release, test the viewer using simulated sessions, and verify the ZIP.
- [ ] T006 Verify simultaneous button input in the viewer with two physical Wii Remotes.
- [x] T007 Integrate the illustration and blue LEDs per controller. Move the **Salida** (Output) menu to each row and the coffee button to the top right. Verify rows without IDs, mouse and keyboard menu operation, and control placement.

WinForms tests confirmed checkbox alignment below the list, the separation between **Activo** (Active) and **Xbox 2**, and exact coverage of the list width at 690, 850, and 1200 pixels and with a scrollbar. Two simulated sessions displayed distinct buttons and one shared button. Closing one session left the other's input visible. Automated tests verified short presses and an active reader without Xbox output.

A Windows device notification triggered another enumeration in the window test. The 70 regular tests and both integration tests passed. The integration tests used one and four real XInput outputs with simulated Wii input.

The viewer review verified in light and dark mode that two LEDs could light simultaneously, a disconnected controller's LED went out, and all LEDs went out after all buttons were released. The **Salida** (Output) menu changed the clicked row even when a different row was selected. Synthetic rows without IDs could refresh without throwing a null-key exception.
