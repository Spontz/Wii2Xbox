# Implementation Plan: Low-Latency Wii2Xbox

**Date**: 2026-11-22 | **Spec**: [spec.md](spec.md)

**Status**: Implementation packaged locally. Validation on a clean PC, in games, and with physical configurations is pending.

## Summary

Native Windows app that converts up to four Bluetooth Wii Remotes into independent Xbox outputs. Demonstrate one physical controller and one real output first, then cover isolation, recovery, and profiles. Optimize the HID-to-mapping-to-submission path independently of screen refresh. Product acceptance requires physical tests and reproducible percentiles.

## Technical Context

**Language/Version**: C# with .NET 10 LTS and SDK 10.0.404, Windows x64. T004 built in Release.

**Primary Dependencies**: WinForms, HidSharp 2.6.4, and Nefarius.ViGEm.Client 1.21.256. Packaging embeds the ViGEmBus 1.22.0 installer. Compatibility and maintenance risks for the archived driver remain subject to the acceptance matrix.

**Storage**: Versioned JSON in `%LOCALAPPDATA%/Wii2Xbox`, with temporary writes and atomic replacement. Export performance reports on demand.

**Testing**: xUnit for protocol, mapping, states, and isolation. Simulated transport and output, integration tests with a separate XInput observer, and a documented physical test setup.

**Target Platform**: Windows 10/11 x64 versions listed in the matrix. Verify actual runtime and Windows support for each listed version and build.

**Project Type**: Local desktop app with no backend.

**Performance Goals**: SC-003 targets local p95 ≤1 ms and p99 ≤2 ms. SC-006 targets p95 ≤4 ms to a virtual observer with documented uncertainty. These are acceptance targets.

**Constraints**: Four shared system XInput slots. Windows assigns slots. Compatible Bluetooth Classic and a signed driver are required. Run the game process without elevation. Default settings use no busy loop, real-time scheduling, or global power or timer changes.

**Scale/Scope**: One process, up to four sessions, original Wii Remote, Wii Remote Plus, Nunchuk, profiles, and bounded diagnostics.

## Constitution Check

| Principle | Planned design | Evidence gate |
| --- | --- | --- |
| Minimum latency | Per-device reader, synchronous mapping and submission, decoupled UI | SC-003/005/006/007 |
| Isolation | Session owns transport and output, idempotent cleanup | SC-002/004/008 |
| Evidence | Simulations tracked separately from Bluetooth and games | Matrix and physical acceptance |
| Transparency | Visible dependencies and explicit installation | SC-010 and package review |
| Specification first | Requirements traced to tasks and tests | Final FR/SC audit |

The design complies. Execution gates remain pending. The discontinued dependency is a recorded compatibility risk.

## Project Structure

```text
src/Wii2Xbox/
  Program.cs, MainForm.cs       # Startup and UI outside the input path
  Input.cs                     # State, decoding, and profile scaffolding
  HidTransport.cs               # Physical transport and protocol setup
  BluetoothService.cs           # Discovery, pairing, and cancellation
  XboxOutput.cs                 # Emulation adapter
  VigemBusSetup.cs              # Embedded installer and Windows elevation
  ControllerSession.cs          # Lifecycle and low-latency path
  SessionManager.cs             # Controller identity, capacity, and assignment
  ProfileStore.cs               # Persistence and validation
  LatencyMetrics.cs             # Bounded buffer and snapshots
tests/Wii2Xbox.Tests/           # Automated tests without hardware
tools/Wii2Xbox.Diagnostics/     # XInput observer for development tests
scripts/                       # Build, test, publish
profiles/                      # Versioned starter profiles
docs/                          # Guides, compatibility, and physical results
specs/001-wii-xbox-low-latency/  # Specification, decisions, contracts, and tasks
```

## Architecture and Latency Budget

1. Session control opens one HID interface and creates its virtual output before marking it Active. Configure the controller with a state machine that waits for ACK and validates accessory identity. Reject encrypted data as stick input.
2. A dedicated reader per device waits on a blocking or overlapped read. It uses no UI polling or `Task.Delay` between reports. Use tests to choose HID buffering and continuous-report policy based on the rate the controller actually produces.
3. After a full read: monotonic timestamp → validate ID and length → decode → apply immutable profile → update the full state → submit once to Xbox → final timestamp. Keep JSON, LINQ, per-event logs, and avoidable allocations out of this path.
4. Preserve press/release transitions. The UI refreshes state, slot, and profile at 5 Hz. Development tests independently sample latency.
5. Start with normal priority. Raise the reader to AboveNormal only if measurements improve without harming stability. Never use Realtime priority. Leave `timeBeginPeriod`, power plan, radio parameters, and affinity unchanged by default.
6. The watchdog detects silence in continuous reports and neutralizes output within 1 second of observable loss. At session end it cancels the read and disconnects the output even if neutral submission fails. Cleanup is idempotent and submission avoids locks shared with the UI.

Provisional local budget: decode and map within 0.15 ms p95. Submission takes the remainder of the 1 ms p95 budget. Measure each part. Record diagnostic cost, runtime pauses, HID cadence, and jitter. Account for reports lost in transport when interpreting software measurements.

## Bluetooth and Protocol

Enumerate Nintendo HID devices and recognize models. Use native Windows Bluetooth for radio access, discovery, and pairing. Cancellation within 5 seconds requires splitting or isolating blocking operations and stopping the underlying work. Preserve unrelated pairings.

The wizard repeats short Bluetooth searches in the background and preserves selection as results arrive. Pairing waits until the current search ends and then verifies that the matching HID interface is visible in the main list. If the selected remote's existing association is disconnected and no matching HID interface appears, remove that association and immediately authenticate the same remote again using its Bluetooth address. Keep errors visible if discovery resumes. Closing cancels the search loop before disposing of the window.

For each model, validate HID opening and write methods (`Write` versus control report) and report mode. Status, ACK, and memory belong to the control channel. Input belongs to the data channel. Nunchuk initialization requires negotiation, identification, and actual calibration. Hot removal neutralizes accessory input. Basic mode must work without an extension.

## Lifecycle and Error Handling

`Discovered → Connecting → Configuring → Active → Stopping → Disconnected`. Any stage can enter `Faulted` after cleanup. Allow one session per controller. Reconnect with bounded backoff outside the reader and always allow manual action. Sleep safely cancels sessions and resume re-enumerates identities. Roll back resources acquired by a partial startup.

Apply profile changes by atomically swapping a validated snapshot at a report boundary. Neutralize before using the new profile. Keep administrative disk writes from blocking the UI or reader.

Fit the profile editor to two groups for Wii Remote and Nunchuk controls. Read the selected controller's camera-ready state from the UI when showing the IR setup modal. Start calibration only after that session confirms camera readiness.

HID enumeration and driver checking enable automatic activation. The app defaults to one Xbox output for each available controller. Choosing a different output changes only that controller and waits for the old session to release its resources. Tests can change latency capture without restarting the output.

The session reports the slot assigned by XInput. After submitting the first Xbox state, it writes the corresponding LED through HID. If Windows takes time to assign the slot or the write fails, retry at spaced reports. The UI shows the slot on its normal refresh.

## Installation and Distribution

`scripts/publish.ps1` verifies the official installer's SHA-256 and Authenticode signature, embeds it in Wii2Xbox, and includes its license. When the driver is missing or incompatible, the app offers installation when the main window opens. After the user accepts, Wii2Xbox extracts and verifies the resource and asks Windows for elevation. It waits without blocking the UI, checks the driver afterward, and reports the result. The installer never forces a restart. `scripts/test-native.ps1` builds the XInput observer and runs comparisons in the test environment.

## Delivery Strategy

- Gate A. Demonstrate Wii transport and virtual output with hardware. Resolve driver maintenance, license, and signing. On failure, record the technical blocker and evaluate an equivalent signed backend without rewriting mapping or UI.
- Gate B. Robust session lifecycle, four controllers, watchdog, cancellation, and automated tests.
- Gate C. Complete profiles, Nunchuk calibration, reconnection, and sleep handling.
- Gate D. Independent observer, trials with one and four controllers, and percentile-based optimization.
- Gate E. Self-contained Windows x64 package with an embedded signed installer, guide, and validation on a PC without the SDK. Full acceptance for SC-001–SC-012.

A custom kernel driver is outside this scope. Update scope and risks if an alternative requires one.

## Verification Strategy

Protocol: fixtures for all supported IDs, truncated input, ACK errors, status, and sensor bits. Mapping: combinations and releases, horizontal rotation, dead zones and ranges, and multiple sources for one destination. Session: timeouts, loss, cancellation, output errors, partial startup, and concurrent shutdown. Isolation: four simulated transports and outputs, 100,000 transitions, and 100 reconnections.

Performance: Release without a debugger, 30-second warm-up, ≥10 minutes and ≥10,000 reports per controller, one and four controllers, UI idle, in use, and minimized, with diagnostics off and on. Report lost samples, GC, hardware, and variation. Run the observer in a separate process. XInput exposes sampled states, so the physical trial cannot establish that arbitrarily short presses are observed. Use identifiable markers and state reading frequency and aliasing. SC-004 synthetic testing verifies the submission path. Games can observe transitions at their own rate.

Physical trials: record adapter, controller model, accessory, Windows version, driver, and game. Identify the camera procedure or input instrument and ensure sufficient time resolution and sample size for p99. Record limits and report percentiles only with enough samples.

## Complexity Tracking

No approved constitutional deviations. Open risks include the discontinued backend, model-specific pairing differences, accessory calibration, and targets pending measurement. The initial scaffold is insufficient evidence of a completed story.
