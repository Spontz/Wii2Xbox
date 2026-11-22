# Appearance Decision

The native WinForms API in .NET 10 applies Windows 11 app mode before controls are created. Windows 10 retains classic styling, and high contrast uses the system palette. Windows does not automatically restyle existing controls when the user changes app mode. [Microsoft documentation](https://learn.microsoft.com/dotnet/api/system.windows.forms.application.setcolormode?view=windowsdesktop-10.0)
