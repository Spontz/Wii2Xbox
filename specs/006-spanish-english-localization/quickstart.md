# Verify Localization

Use Windows 10 or 11 x64, the .NET SDK from `global.json`, Visual Studio x64 C++ build tools, and the Windows SDK. Use an isolated account or VM for setup and display-language testing.

1. Run `./scripts/test.ps1` and build both packages with `./scripts/package.ps1 -Version 1.1.1`.
2. Launch the portable and installed builds with Spanish, English, and an unsupported Windows display language. Include regional variants and a case where regional formats differ from display language.
3. Verify manual and background startup select the same language. Confirm that the main window offers no language picker.
4. Inspect the main window, pairing, and profiles at the minimum size and under increased Windows scaling. Check that the controls remain readable and scrolling reveals overflow.
5. Open the same MSI in each display language. Verify welcome, progress, completion, cancellation, upgrade, and uninstall.
