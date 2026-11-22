# Verify Localization

Use Windows 10 or 11 x64 with the .NET SDK pinned in `global.json`, Visual Studio x64 C++ build tools, and a Windows SDK. Use an isolated Windows account or VM for setup and display-language testing.

1. Run `./scripts/test.ps1` and build the two packages with `./scripts/package.ps1 -Version 1.1.0`.
2. Extract the portable ZIP and start with no saved preference. Test a Spanish display language, an English display language, and an unsupported display language. Include two variants of Spanish and English. Set regional formats to a different language in one case.
3. Choose the opposite language while a Wii Remote is active. Check that input continues and that the current interface changes only after selecting Exit from the notification area and reopening. Repeat with System and with background launch.
4. In each language, exercise pairing, profiles and import/export, IR calibration, output selection, tray actions, and driver status. Check text at the minimum window sizes.
5. Open the same MSI in each Windows language. Verify welcome, progress, completion, cancellation, and uninstall. With an existing installation, verify upgrade retains the app language.

## Local Verification

- The ordinary suite passed 79 tests. Four opt-in native tests were skipped. Localization tests cover language precedence, regional variants, fallback, catalog completeness, format arguments, English WinForms controls, and preservation of a user-entered profile name.
- On the Spanish development account, the app's main window showed the System preference and Spanish controls. Windows Installer ran the display-language selector and displayed the Spanish welcome and cancellation dialogs.
- The Windows x64 MSI and ZIP have identical paths and SHA-256 hashes for all 290 payload files, including the Spanish satellite assembly.
- The full install, upgrade, and uninstall matrix on clean Windows accounts with different display languages and regional formats remains open.
