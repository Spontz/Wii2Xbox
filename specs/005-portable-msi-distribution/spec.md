# Wii2Xbox Portable ZIP and MSI Distribution

**Date** 2026-11-22  
**Status** In validation

## Scenarios and Tests

### P1. Run Wii2Xbox from the portable ZIP

As a player, I want to extract the ZIP and run Wii2Xbox from a folder of my choice.

1. On Windows x64 without the SDK, extracting the ZIP and running `Wii2Xbox.exe` opens the app without registering an installed product.
2. After I exit Wii2Xbox, I can move or delete the extracted folder.

### P1. Install and uninstall with Windows Installer

As a player, I want to run an MSI, open Wii2Xbox from Start, and uninstall it from Installed apps.

1. A successful installation creates the Start shortcut, registers the app for the current user, and installs the same files as the ZIP in the user's local programs folder.
2. After selecting **Salir** (Exit) from the notification-area icon and uninstalling Wii2Xbox, Windows removes the product registration, shortcut, installed files, and startup entry for that installation. Saved profiles and the shared ViGEmBus driver remain available.
3. Opening the MSI normally shows the setup wizard, installation progress, and a completion dialog. The installed app opens from the Start shortcut.

### P1. Receive both packages with attribution

As a release maintainer, I want to build and verify both formats from a single build, with the project license and credits in each.

1. Packaging produces `Wii2Xbox-win-x64.zip` and `Wii2Xbox-win-x64.msi` for the same version.
2. After extracting the ZIP or installing the MSI, I can read `LICENSE`, `NOTICE`, and the third-party notices in the app folder.

### Edge Cases

- When ViGEmBus is absent on a clean PC, Wii2Xbox continues to offer explicit installation of the driver for Xbox output. Both packages include the official installer embedded in the app.
- The portable and installed distributions can coexist. Automatic startup stays optional. Uninstalling the MSI preserves any startup entry for the portable copy.
- When Wii2Xbox is running in the background, the user can exit completely from the notification area before updating or uninstalling.

## Functional Requirements

- **FR-001**. Each Windows x64 release must generate a portable ZIP and a per-user MSI from the same self-contained Release build.
- **FR-002**. The MSI must support installation, upgrades to later versions, and uninstallation through Windows Installer. It must create a Start shortcut and an entry in Installed apps.
- **FR-003**. Uninstallation must remove the resources owned by the MSI and preserve user profiles, the extracted portable copy, and shared drivers.
- **FR-004**. Wii2Xbox's own code is licensed under **Apache-2.0**. Both packages must include `LICENSE` and `NOTICE` with attribution to **Iván Fernández**, along with notices for third-party components. Section 4 governs redistribution and retention of notices. [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)
- **FR-005**. The instructions must explain how to generate both artifacts, run the portable ZIP, and uninstall the MSI.

## Success Criteria

- **SC-001**. A single packaging run produces both artifacts. All application and license files have the same relative paths and SHA-256 hashes in the ZIP and the administrative extraction of the MSI.
- **SC-002**. On Windows x64 without the SDK, the extracted ZIP launches. The MSI installs, launches from Start, and uninstalls successfully, preserving profiles and the driver.
- **SC-003**. Upgrading to a later version leaves one entry in Installed apps and retains automatic startup if the user enabled it.
