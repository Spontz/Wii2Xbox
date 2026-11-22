# Verify Automatic Updates

1. Run `./scripts/test.ps1` and build self-contained packages with `./scripts/package.ps1 -Version 1.2.0`.
2. For an end-to-end GitHub test, start from an updater-capable package and publish a higher stable version of the same distribution in the official repository. In an isolated Windows environment, test checks at startup, active sessions, then restart on idle or through the tray action.
3. Check offline, corrupt and older assets. Interrupt portable replacement and verify backup restoration. Verify MSI rollback and reboot-request handling.
4. Confirm that the new app reopens in the previous mode and that settings, profiles, startup registration, and unrelated portable files persist.

## Local Verification

- 85 automated tests passed. Network fixtures cover both distributions, prerelease rejection, version ordering, digest and size failures, and redirect origin checks.
- The actual external worker replaced a portable fixture and relaunched it in the background. Corrupt package and locked-file cases preserved the original payload and unrelated data.
- The same worker performed a real per-user Windows Installer upgrade with isolated test MSI files and relaunched the fixture in the foreground.
- Automatic update between two official published versions and acceptance during physical controller use remain open.
