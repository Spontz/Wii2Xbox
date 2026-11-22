# Feature Specification: Automatic Updates

**Feature Directory**: `specs/007-automatic-updates`

**Created**: 2026-11-23

## User Scenarios & Testing

### US1. Receive a new version automatically (P1)

On startup and periodically while resident, check the official Spontz/Wii2Xbox GitHub releases. Download a newer stable build of the current distribution in the background. A current build, offline connection, missing package, or invalid checksum leaves the app running.

### US2. Continue playing while an update is prepared (P1)

A ready update waits while a controller session or dialog is active. When it is safe, Wii2Xbox closes cleanly, applies the update silently, and relaunches in the previous visible or background mode. Players can also choose Restart to update from the notification menu when ready.

### US3. Recover from an unsuccessful update (P1)

Installation errors leave an existing app usable, preserve settings, and present a localized status at the next launch. The app does not immediately attempt the same operation again. A Windows reboot request is reported without rebooting the computer.

## Functional Requirements

- **FR-001**. Select the highest stable version newer than the current version from the official GitHub repository. Match the existing installer or portable distribution.
- **FR-002**. Check without a user account. Verify the downloaded file size and SHA-256 against the official metadata before applying it. Use only HTTPS GitHub endpoints.
- **FR-003**. Keep downloading off the input path. Defer automatic restart until controller sessions and dialogs are idle. Explicit restart uses the usual neutralizing shutdown.
- **FR-004**. Apply the per-user MSI silently without triggering a Windows reboot. Update the portable copy in place and preserve files outside the app payload.
- **FR-005**. Keep the app's data, bindings, startup registration, user display language, and visible or resident startup mode.
- **FR-006**. Report failure or restart-required outcomes, preserve the previous portable files on failure, and release the updater lock on all normal exits.

## Success Criteria

- **SC-001**. Current, older, prerelease, wrong-distribution, corrupt, and unverified assets never trigger an install.
- **SC-002**. With a reachable official compatible release, the installed and portable distributions update and relaunch, while preserving profiles and startup behavior.
- **SC-003**. Active input continues until the user chooses to restart or all sessions end. A failed update leaves a usable launch and does not loop.

## Assumptions

- Automatic checks run in official self-contained packages. Local development builds remain suitable for running independently of releases.
- GitHub's release asset digests are the integrity source for the currently unsigned Wii2Xbox packages. The updater does not weaken Windows trust or security settings.
