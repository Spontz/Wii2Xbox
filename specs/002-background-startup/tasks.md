# Background Operation and Windows Startup Tasks

- [x] T001 [US1] Keep sessions and checks active while the window is hidden. Add the icon with reopen and exit actions (FR-001/002).
- [x] T002 [US1] Coordinate launches in the same session and restore the window on a manual relaunch (FR-003).
- [x] T003 [US2] Add the checkbox and reversible per-user startup registration. Start hidden and show driver notices (FR-004/005).
- [x] T004 [US1, US2] Verify the build and the window and process lifecycle. See local evidence.
- [ ] T005 [US2] Verify the checkbox across two real sign-ins, first enabled and then disabled (SC-003).
- [ ] T006 [US1] Repeat the hide and reopen cycle with physical controllers in a game. Check slots and confirm that **Salir** (Exit) releases the outputs (SC-001/002).

## Local Evidence

- 68 automated tests passed. The Release package and the controller list's four columns were verified.
- A WinForms window started hidden, restored the same window handle on three attempts, and retained a session receiving simulated input. **Salir** (Exit) sent neutral state and released that output.
- The published executable requested window restoration when launched manually as a second instance. With `--background`, it exited without requesting that the window appear.
- An isolated registry key test verified the initial state, registration of the quoted executable path, and removal of the entry when disabled. Real sign-in remains pending in T005.
