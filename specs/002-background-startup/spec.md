# Background Operation and Windows Startup Specification

**Created** 2026-11-22  
**Scope** Wii2Xbox for Windows 10/11 x64  
**Base feature** [Wii-to-Xbox conversion](../001-wii-xbox-low-latency/spec.md)

## User Scenarios and Tests

### US1. Keep controllers active after closing the window (P1)

As a player, I want to remove the window from my desktop and keep using my controllers.

**Independent test**. With an active session, close the window, check input in a game, and reopen Wii2Xbox from the notification-area icon.

1. Closing the window removes it from the desktop. The process keeps running with the same sessions.
2. Choosing **Abrir Wii2Xbox** (Open Wii2Xbox) from the icon or launching the app again restores the same window.
3. Choosing **Salir** (Exit) from the icon stops the app, neutralizes input, and releases its outputs. Signing out or shutting down Windows also stops the app.

### US2. Start with Windows (P1)

As a player, I want Wii2Xbox to start at Windows sign-in and make my controllers available without opening the app manually.

**Independent test**. Enable **Arrancar al iniciar Windows** (Start with Windows), sign in again, and verify that the icon appears while the window remains hidden. Clear the checkbox and sign in again.

1. The checkbox starts cleared on a new account. A change applies at the next sign-in.
2. When enabled, the app starts hidden, checks the Xbox driver, and searches for available Wii Remotes.
3. When disabled, Windows stops launching Wii2Xbox automatically.

### Edge Cases

- A second manual launch must restore the running instance's window. A second automatic launch leaves an existing background instance hidden.
- If the Xbox driver fails during background startup, the app requests a notification and keeps the explanation available in the window.
- If Windows denies a startup setting change, Wii2Xbox keeps the previous checkbox state and shows the error.

## Requirements

- **FR-001**. Keep controller detection, input, and reconnection active while the window is hidden.
- **FR-002**. Offer **Abrir Wii2Xbox** (Open Wii2Xbox) and **Salir** (Exit) from the notification area. Explicitly exiting releases inputs and outputs.
- **FR-003**. Allow one instance per Windows session and reuse its window when the app is reopened.
- **FR-004**. Keep **Arrancar al iniciar Windows** (Start with Windows) disabled by default. Save and apply the preference only for the account that changed it, without elevating the app.
- **FR-005**. When enabled, start in the background at sign-in with the same checks and detection behavior as a manual launch.

## Success Criteria

- **SC-001**. After closing and reopening the window three times, the same process and sessions remain active and receive input without slot reassignment.
- **SC-002**. A second manual launch restores the window. In normal conditions, **Salir** (Exit) removes the icon and stops the process within three seconds.
- **SC-003**. Across two sign-ins, one with the checkbox enabled and one after disabling it, Wii2Xbox starts only on the first. Its window remains hidden until the user opens it.

## Assumptions

- The background process belongs to the user's interactive session, where Windows presents the window, icon, and cursor. Automatic startup occurs at sign-in.
- Enabling the option records the executable path. If Wii2Xbox moves to another folder, clear and reselect the checkbox to update it.
