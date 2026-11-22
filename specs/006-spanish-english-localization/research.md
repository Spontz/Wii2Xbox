# Localization Decisions

- Resolve language once per process using Windows display language rather than regional formatting or keyboard language.
- Use an English base and Spanish satellite resources. The main window follows Windows directly.
- Build both WiX localizations and merge translated setup text into one MSI. Select setup text in the UI sequence. Windows Installer-owned text follows its own language rules.
- Use WinForms per-monitor DPI scaling and a scrollable layout. The heading, toolbar, controller list, viewer, and status rows keep separate space at enlarged text sizes.
