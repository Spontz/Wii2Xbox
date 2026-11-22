# Windows Theme Plan

**Specification** [spec.md](spec.md)

## Technical Context and Constitution

WinForms on .NET 10 provides native Windows 11 styling. Read the preference before creating controls. Apply theme changes on the next launch to keep sessions active during normal use.

## Design and Verification

- Apply System color mode after initializing WinForms and before creating the window or notification-area icon.
- Adapt the viewer palette and IR calibration target contrast to the process theme. Use system colors in high-contrast mode.
- Render the window and viewer in both themes in a test process. Retain classic styling on Windows 10.

[Windows Forms color mode](https://learn.microsoft.com/dotnet/api/system.windows.forms.application.setcolormode?view=windowsdesktop-10.0)
