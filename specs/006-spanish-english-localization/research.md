# Localization Decisions

- Use a per-process UI language. A full relaunch applies changes without stopping active sessions when the user saves.
- Use the Windows user UI language rather than regional formatting or the keyboard/input language. Match Spanish by its primary language and use English for the remaining languages.
- Use named resources with an English base and Spanish translations. Keep profile mapping values stable and localize their display labels.
- Select MSI text inside the UI sequence with the native Windows display-language result. Build the two WiX localizations and merge their formatted dialog text into one MSI. This retains WiX's existing setup flow and keeps the selector out of the installation execution sequence. Windows Installer-owned error text continues to follow its own language rules.
