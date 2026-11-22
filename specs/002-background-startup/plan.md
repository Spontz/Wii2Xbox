# Background Operation and Windows Startup Plan

**Specification** [spec.md](spec.md)

## Technical Design

- `Program.cs` keeps the message loop running without requiring a visible window. A session-scoped mutex and event coordinate concurrent launches and restore the existing instance.
- `MainForm.cs` keeps the same detection and conversion loop running while hidden. A notification-area icon offers **Abrir Wii2Xbox** (Open Wii2Xbox) and **Salir** (Exit). Exiting uses the existing session neutralization and shutdown logic.
- `StartupRegistration.cs` adds or removes one startup entry for the current account. The command quotes the executable path and adds the `--background` argument.
- The engine initializes once whether the window starts visible or hidden. Driver notices are shown in the window and requested through the notification area while the window is hidden.

The process uses the interactive session for the IR mouse, UI, and notification-area icon. Windows runs a per-user startup entry when the account signs in. [Windows interactive services](https://learn.microsoft.com/en-us/windows/win32/services/interactive-services) · [Run and RunOnce registry keys](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys)

## Verification

- Build Release, verify the package with the embedded ViGEmBus installer, and run the test suite.
- Check the open, hide, restore, and exit cycle in a controlled process with an instrumented session, without changing machine startup.
- Check hidden startup and reopening from a second process. Verify real sign-in and use with a Wii Remote in a game during physical acceptance.
