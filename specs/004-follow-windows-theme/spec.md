# Windows Theme Specification

**Created** 2026-11-22

[Plan](plan.md) · [Tasks and evidence](tasks.md)

## User Scenarios and Tests

### US1. Follow Windows appearance (P1)

As a user, I want Wii2Xbox to open with the theme I chose for Windows apps.

1. With the light theme selected, opening Wii2Xbox displays the window and dialogs in light mode.
2. With the dark theme selected on Windows 11, opening Wii2Xbox displays the window, dialogs, and viewer in dark mode.
3. After changing the Windows theme, exiting and relaunching Wii2Xbox applies the new preference.

### Edge Cases

- Startup at sign-in and reopening from the notification-area icon use the existing process theme.
- On Windows 10 and with high contrast enabled, Windows controls retain the appearance provided by the system.

## Requirements

- **FR-001**. Read the application appearance preference when the process starts.
- **FR-002**. Apply that appearance to the window, dialogs, and menus. Keep idle and pressed buttons easy to distinguish in the viewer.
- **FR-003**. Apply the same appearance rules for manual and background launches.

## Success Criteria

- **SC-001**. On Windows 11, the window and dialogs follow the system preference when launched in light and dark modes.
- **SC-002**. Viewer labels and pressed buttons are distinguishable in both themes. High-contrast mode retains its system colors.
