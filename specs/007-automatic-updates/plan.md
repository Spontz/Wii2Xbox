# Implementation Plan: Automatic Updates

**Created**: 2026-11-23

## Technical Context

Use C# and WinForms on .NET 10 for asynchronous release discovery and staging. Use the Windows PowerShell runtime for an embedded handoff script that runs from outside the app installation. Use Windows Installer for per-user MSI upgrades and a file manifest for the portable distribution.

## Constitution Check

Keep checks and download I/O outside controller processing. Graceful shutdown neutralizes outputs. Use the public official GitHub repository without credentials or telemetry. Both distributions retain their format and established profile storage.

## Implementation

1. Recognize stable release tags and authenticate HTTPS origin. Validate the expected GitHub URL, published digest, and size.
2. Stage in a unique per-user directory. Defer restart while input or a dialog is active and offer a tray action for explicit restart.
3. Transfer to the helper with a process check, readiness signal, and update lock. Wait for the app to exit before writing application files.
4. Run the MSI silently. For portable, back up existing owned files, replace the staged payload, and restore the backup if replacement fails.
5. Relaunch with the previous visibility and a result marker. Verify eligibility, corruption, portable rollback, and MSI behavior.
