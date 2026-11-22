# Development and Acceptance Quickstart

See [acceptance](acceptance.md) for installation, hardware, and compatibility status.

## Tools

PowerShell, Git, and SDK 10.0.404 pinned in `global.json`. The local development SDK is in `%LOCALAPPDATA%/Wii2Xbox/dotnet10`.

## Spec Kit Workflow

Configuration is in `.specify/init-options.json`, the Codex integration is in `.agents/skills`, and the active feature is in `.specify/feature.json`. See `spec.md` for requirements, `plan.md` and `research.md` for decisions, and `tasks.md` for tasks. Run this check from the repository root.

```powershell
& ./.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks
```

## Build, Test, and Package

```powershell
$wiiSdk = "$env:LOCALAPPDATA/Wii2Xbox/dotnet10/dotnet.exe"
& ./scripts/build.ps1 -Dotnet $wiiSdk
& ./scripts/test.ps1 -Dotnet $wiiSdk
& ./scripts/package.ps1 -Dotnet $wiiSdk -Version 1.0.1
```

Packaging creates the portable ZIP and MSI from one Release build and validates and embeds the signed driver installer. For XInput tests from the repository, run `./scripts/test-native.ps1 -Dotnet $wiiSdk`. The observer and its reports are generated in `artifacts/native-test-…`.

## Real Hardware Test

1. Extract the ZIP on Windows without the SDK or driver, disconnect the Internet, and open Wii2Xbox. Choose **Instalar controlador Xbox** (Install Xbox driver), authorize the installation, and restart if required.
2. Connect a compatible radio and Wii Remote. Verify the model, identity, and real HID input.
3. Verify automatic activation and test buttons and axes in `joy.cpl` or an XInput game. Check that the LED matches the **Xbox** column, including with another slot occupied. Record browser detection separately.
4. Repeat with four controllers and test disconnection while holding input, cancellation, sleep, and reconnection.
5. Verify profiles, Nunchuk calibration, and hot removal.
6. Run the plan's performance matrix and record evidence for SC-001–SC-012.
7. Repeat installation and cancel the Windows prompt. Verify the message and that **Ratón IR** (IR mouse) remains available.

Keep simulated measurements out of the physical test matrix. If hardware or the driver is unavailable, record the pending item and continue with independent tests that can run.
