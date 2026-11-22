# Implementation Plan: Spanish and English Localization

**Date**: 2026-11-22 | **Spec**: [spec.md](spec.md)

## Summary

Resolve the app language before the first window, keep it fixed for the lifetime of the process, and give each Windows account a saved override. Use a single per-user MSI with both English and Spanish setup text.

## Technical Context

- **Platform**: C# and .NET 10 WinForms, Windows 10/11 x64, WiX 6 Windows Installer. Building the MSI requires the Visual Studio x64 C++ build tools and Windows SDK for its small native language selector.
- **Storage**: An optional per-user Windows setting containing a stable language code. Existing controller profiles keep their current schema and location.
- **Testing**: xUnit for language selection, translations, and settings behavior. Verify setup language selection and the contents of the portable ZIP and MSI on Windows.
- **Performance**: Resolve language once per process. Keep resource lookups, persistence, and formatting out of the input submission path.

## Constitution Check

Language changes preserve live sessions and take effect only after a full relaunch. Background startup and driver checks use the same app language. Both release formats are built from the same application files. Document simulated and physical verification separately.

## Project Structure

- `src/Wii2Xbox/` contains language selection, resources, and localized WinForms surfaces.
- `installer/` contains bilingual setup authoring.
- `scripts/package.ps1` builds the MSI and portable ZIP together.
- `tests/Wii2Xbox.Tests/` covers language policy, resources, and user-facing controls.

## Design and Verification

1. Resolve System from the native user display language. Resolve explicit preferences first. Save only changes requested by the user.
2. Replace Wii2Xbox-owned text with named English and Spanish resources. Keep profile names, mapping keys, and device identities independent of display text.
3. Localize setup from the Windows display language in one MSI. Keep the current per-user install, upgrade, and uninstall behavior.
4. Run the [quickstart](quickstart.md) and record acceptance that requires additional Windows language configurations separately.
