# Verification Status

**Date** 2026-11-22.

## Initial Checks

- `specify init --here --integration codex --script ps --force --non-interactive` succeeded and installed Spec Kit 1.1.3, the integration, and scripts.
- `check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` resolved the feature to `specs/001-wii-xbox-low-latency` and found research, the data model, contracts, quickstart, and tasks.
- The document review found a complete specification with 22 functional requirements, 10 acceptance criteria, 6 stories, and 49 tasks. The requirements checklist was complete and no placeholders remained.
- `dotnet build src/Wii2Xbox/Wii2Xbox.csproj -c Release` with local SDK 8.0.428 passed with zero errors and warnings. This verified the desktop scaffold. The delivery runtime still needed migration at that point.
- The local Git repository was initialized on `main`. At the end of that session it had no commits or remote.

## Basic Conversion

- The project pinned .NET 10.0.404. Eleven Release tests passed for basic reports, button masks, orientation, and release.
- A self-contained Windows x64 package and `artifacts/Wii2Xbox-win-x64.zip` were generated. The published executable opened a Wii2Xbox window and closed normally on this PC. Testing on another PC without the SDK remains pending.
- The UI enumerates Nintendo HID paths and manages up to four sessions. Each session uses a dedicated reader and submits complete states to ViGEmBus without waiting for UI refresh.
- The local PnP inventory showed Bluetooth enabled and no Wii or ViGEmBus devices. Mapping a real controller to XInput and verifying game response remained pending at that point.

## Built-in Bluetooth Pairing

- The **Añadir mando** (Add controller) dialog discovers devices over the radio, lists supported Nintendo models, and lets the user choose a controller and the SYNC or 1+2 method. Authentication uses the six-byte PIN for the selected method. [Dolphin on Windows](https://github.com/dolphin-emu/dolphin/blob/master/Source/Core/Core/HW/WiimoteReal/IOWin.cpp) · [HID service API](https://learn.microsoft.com/en-us/windows/win32/api/bluetoothapis/nf-bluetoothapis-bluetoothsetservicestate)
- The native Bluetooth API completed a search with no controllers in 2,644 ms. A second search, with cancellation requested at 300 ms, ended 2,619 ms after it began. The existing 11 button tests still passed.

## Latency Instrumentation

- Optional per-session diagnostics capture monotonic timestamps for the last 131,072 submissions. JSON export records measurement type, conditions, cadence, window duration, samples, p50/p95/p99/maximum, and excluded samples. The first submission is excluded because it includes virtual output creation.
- For basic report 0x30, the session prepares button mappings before opening the device. Each report uses an indexed lookup. The test compares all 2,048 button combinations in both orientations against the general mapper.
- Seventeen Release tests passed. The synthetic capture test observed no per-report allocations after buffer allocation. The x64 package was rebuilt with this version.

## Per-Controller Profiles

- The editor maps digital buttons and D-pad directions, saves by HID path, and imports and exports version 1 JSON. Sessions apply a new mapping on the next report after sending neutral state.
- Twenty-three Release tests passed, including isolated persistence, rejection of an unknown version without overwriting the profile, invalid destinations, opposite directions, and correct release when inputs share a destination.

## Nunchuk

- Extension negotiation handles write ACK, identification, factory calibration reads, and a configurable dead zone. Normal Wii Remote reports continue in basic mode during negotiation. Removing the accessory releases the stick and C/Z while keeping Xbox output active.
- Twenty-seven Release tests passed, covering the command sequence, calibration and limits, unknown accessories, removal, and fallback calibration. No physical Nunchuk traffic was observed in that run.
- Interoperability references include [Wii Remote reports and registers](https://www.wiibrew.org/wiki/Wiimote), [extension identification](https://www.wiibrew.org/wiki/Wiimote/Extension_Controllers), and [Dolphin's Nunchuk calibration layout](https://github.com/dolphin-emu/dolphin/blob/master/Source/Core/Core/HW/WiimoteEmu/Extension/Nunchuk.h).

## XInput Observer

- A separate process polls XInput 1.4 and records Xbox A transitions and observation intervals. The comparison uses QPC timestamps from the app and observer, measures through the XInput read, and rejects mismatched sequences. [XInput reads](https://learn.microsoft.com/en-us/windows/win32/api/xinput/nf-xinput-xinputgetstate) · [QPC](https://learn.microsoft.com/en-us/windows/win32/sysinfo/acquiring-high-resolution-time-stamps)
- The published executable polled all four Windows slots, which were disconnected. With synthetic traces, the comparison returned p50 of 2 ms and p95 of 3 ms from the supplied values and rejected a mismatched release. Those values come from the calculation test.
- Twenty-eight automated tests passed. Real capture and the SC-006 p95 target remained in T038.

## IR Sensor Bar Mouse

- Mouse-only mode moves the pointer without ViGEmBus. Combined mode emits Xbox and mouse input from the same session. Both use the two points detected by the Wii Remote camera.
- Automated tests cover the ACK sequence, coordinate format, tracking loss, release on shutdown, and IR and Nunchuk data in the same report.
- Full-session tests exercise IR initialization and Nunchuk negotiation with simulated reports. Combined mode verified Xbox buttons and axes alongside click release when the bar was lost. Mouse-only mode verified that the Xbox output stayed closed and the accessory was left unconfigured.
- Calibration uses two points, can restore factory defaults, and is stored in the profile. Tests cover inverted-axis mapping, click suppression while capturing points, the press edge, and per-controller persistence.
- SC-011 requires hardware testing. [Wii Remote IR camera](https://www.wiibrew.org/wiki/Wiimote) · [Windows mouse coordinates](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-mouseinput)

## Recovery

- After transport failure or sleep, each controller retries after 0.5 s, 1 s, 2 s, and 4 s. It keeps its profile and mode. Stopping cancels the schedule. An Xbox output error leaves Bluetooth discovery idle.
- The Xbox output receives neutral state before closing in loss and cancellation tests with a blocked read. The inactivity deadline starts at the last valid report.
- A test that deliberately blocks HID close confirms that neutral Xbox state is sent and the output is released before the transport closes after a read failure.
- Four simulated sessions delivered 100,000 alternating transitions to their own outputs without loss. SC-002 still requires one hour with real hardware. SC-008 requires adapter loss, sleep, and shutdown with actual output.
- The session distinguishes incomplete HID reports from unknown formats. Development JSON reports count both separately. A test inserts both cases and a valid ACK between a press and release. Both valid states reach Xbox in order.
- Sleep and resume handling uses [WM_POWERBROADCAST](https://learn.microsoft.com/en-us/windows/win32/power/wm-powerbroadcast).

## Native Xbox Output Integration

- Driver checking leaves virtual slots free. Tests cover absence, incompatibility, and denied access. The development `driver` command verified the installed driver and left all four slots free. An earlier window test confirmed driver operation with four active native outputs.
- The self-test used the SDK to confirm an active ViGEmBus service. On this PC the service was named **Bigscreen Gamepad Emulation Service**, version 1.17.333.0. Its driver signature was valid. This check superseded the earlier inference from searching PnP IDs for ViGEm.
- The path shared by the app and diagnostics created one and four real outputs. XInput verified distinct slots, buttons, axes, and triggers. The test neutralized and disconnected the outputs. After 100 virtual output cycles all four slots were free.
- The four-output test ran from the extracted ZIP without elevation and with SDK lookup paths excluded. The package includes license notices and a guide. This test used the local PC and its existing driver.
- Bluetooth Classic discovery returned zero Wii Remotes during this run. Real controller input, game input, physical latency, and ViGEmBus 1.22.0 on another PC remained pending. [Compatibility matrix](../../docs/compatibility.md)

## Full Session with Real Xbox Output

- Thirty-eight regular and two opt-in native tests passed. One and four readers generated simulated Wii reports. `ControllerSession` processed them and sent them to real ViGEmBus outputs. An independent process matched all 40 Xbox A press and release transitions in each scenario.
- The p95 from report read to XInput observation was 1.007 ms with one session and 0.893 ms with four. In the four-session case, the highest local processing p95 and p99 were 0.165 ms and 0.317 ms. The independent observer ran at 993.3 Hz and 992.5 Hz, respectively, with maximum reading uncertainty of 1.239 ms and 1.341 ms.
- This brief run left sustained and physical trials pending at the time. Bluetooth Classic discovery returned zero Wii Remotes.

## Sustained Output Tests

- Four real Xbox outputs ran for at least 601 seconds each, with 38,732–38,734 simulated Wii reports per session. The highest processing p95 was 0.190 ms and the highest p99 was 0.336 ms. All four slots were free afterward and the buffer preserved every sample.
- One output ran for 600.997 seconds with 38,664 simulated reports. Processing p95 was 0.195 ms, p99 was 0.335 ms, and the maximum was 2.102 ms. The buffer preserved every sample and XInput verified release on exit.
- T038 has sustained evidence for one and four sessions with simulated input. Physical controller validation remains pending.

## Controller Identity after Pairing

- The app distinguishes Bluetooth authentication from the HID interface becoming available. After pairing, it waits for the matching interface and selects the identified controller. It uses the Bluetooth address to distinguish controllers and save profiles when the device tree exposes it. The HID path is a fallback and lets old profiles load while the path remains available.
- SetupAPI resolved all 19 HID interfaces on the PC. Configuration Manager confirmed the addresses of two Bluetooth Classic devices. Discovery still found no Wii Remote in that run, so pairing with a real Nintendo model remained pending. [Interface-to-devnode API](https://learn.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdigetdeviceinterfacedetailw) · [Parent lookup](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_get_parent)
- Forty-seven regular tests passed, including Bluetooth format resolution and legacy profile compatibility.
- The published `Wii2Xbox.Diagnostics.exe bluetooth 3` ran two Bluetooth Classic searches and checked the HID list. It found zero Wii Remotes and zero Wii HID interfaces. The physical protocol explains how to observe the transition from discovery to HID without changing pairings.

## Outstanding Physical Tests

Pairing and input with Wii Remote and Wii Remote Plus, profile changes and Nunchuk with real hardware, recovery trials, and latency measurements with physical controllers remained open at this stage. SC-004 is supported by the synthetic test required by the specification.

## First Test with a Physical Wii Remote

- Windows paired the `Nintendo RVL-CNT-01` ending in address DBB0 via SYNC and exposed its HID interface. A Wii2Xbox session opened the device by Bluetooth identity and activated Xbox 1.
- The independent XInput observer recorded 15 Xbox A presses and 15 releases from button 2 in the default profile, with actual polling at 993.3 Hz. The session stayed active, and all four slots were available after closing it.
- With Xbox 1 occupied by a test output, the physical Wii Remote session appeared as Xbox 2. The XInput observer found both outputs, and HID confirmed only LED 2 was active. All four LED commands were verified individually with the same physical controller.
- Wii2Xbox measured 1,505 reports in 15.26 s (98.54 Hz), with zero format or length drops. From HID read to return from ViGEmBus, p95 was 0.0593 ms, p99 was 0.0898 ms, and maximum was 0.2625 ms. This brief test excluded radio and game response times.
- The model matrix, game response, four physical devices, and the IR bar still require verification.

## Package with Embedded Driver Installer

- Windows x64 Release packaging completed. The ViGEmBus 1.22.0 installer had a valid Authenticode signature from Nefarius. The embedded resource's SHA-256 matched WinGet. The ZIP included the license, app, runtime, and guides.
- The XInput observer and comparison tool run from the repository. The ZIP was checked for their absence and had no separate driver installer. Sixty-seven automated tests passed, and four separately enabled native tests were skipped in this run.
- In-app installation, elevation, cancellation, and restart remain part of acceptance on clean Windows without the driver.

## ControllerTest in Edge

- The physical Wii Remote was active as Xbox 1. XInput recorded 34 A presses and 34 releases while the Xbox page in Edge showed **Not connected**. The general test page also showed **Not connected** with a second temporary virtual output.
- Windows had Bigscreen Gamepad Emulation Bus 1.17.333.0 registered. During the same check, XInput enumerated Xbox 1 and Windows.Gaming.Input listed zero gamepads and zero `RawGameController` devices. Repeat with the bundled official installer on a clean PC and check an XInput game. The cause of the Windows.Gaming.Input result remains open.

## Acceptance Traceability

The pairing-window test started searching when opened, kept discovery running for 10.5 s with one result, and canceled within 1.184 s of closing. The current main window removes Bluetooth settings, activation, stop, and manual check buttons. Automated tests cover switching latency capture on and off within one Xbox session. Automatic activation of multiple physical controllers remains in T060.

| Requirements | Existing evidence | Remaining verification |
| --- | --- | --- |
| FR-001, FR-018 | The Spanish app launched in an earlier build. The current package includes .NET and guides and excludes the development observer | Repeat on clean Windows 10 and Windows 11 installations |
| FR-002, FR-003 | Discovery and cancellation with the local radio. SetupAPI linked HID identity. Bluetooth format and legacy profile tests | Pair and reconnect physical Wii Remote and Wii Remote Plus devices and test a missing radio in another environment |
| FR-004, FR-005 | Independent XInput slots and Xbox column. Four LED commands confirmed on one physical Wii Remote. Physical session used Xbox 2 with Xbox 1 occupied | Four physical controllers, in-game isolation, and slot reassignment after reconnecting |
| FR-006, FR-007 | Decoding and masks tested. Button 2 → Xbox A verified on a physical Wii Remote. Horizontal and Nunchuk profiles included | Remaining buttons and profiles with hardware |
| FR-008, FR-010 | Nunchuk ACK, calibration, dead zone, removal, and mapping tested. XInput observed triggers and stick with simulated reports | Physical Nunchuk and hot removal |
| FR-009 | JSON persistence and exchange, legacy profile recovery, validated saves, and mapping editor | Change profiles during a game with two physical controllers |
| FR-011, FR-012 | Neutral state and shutdown tested. Bounded retries and cancellation. One hundred virtual output cycles | Power off a controller, cut radio access, and put Windows to sleep during a physical session |
| FR-013, FR-014 | Direct read-to-submission path. 100,000 synthetic transitions preserved. Separate counters for unknown and incomplete reports | Compare radio traffic and HID policy with physical controllers |
| FR-015, FR-016 | Exported windows and percentiles. Documented QPC and XInput observation. Measurements for one and four outputs | Physical latency through visual response |
| FR-017 | Simulated absence, incompatibility, and denied access. Verified installer signature and embedded resource SHA-256 | In-app offline installation on clean Windows, elevation, cancellation, restart, and first output |
| FR-019, FR-020 | Local settings, opt-in diagnostics and IR, and default Xbox mode without camera, telemetry, or global changes | Physical controllers and radio performance |
| FR-021, FR-022 | Verified unknown extension notice and basic mode. The wizard identifies out-of-catalog Wii devices and blocks pairing. Recorded the test matrix | Physical tests with models, accessories, adapters, and games |
| FR-023, FR-024, FR-025, FR-026 | Simulated full sessions with IR, Xbox, and Nunchuk. Verified mouse-only without Xbox backend, clicks, and persistent calibration | Physical bar, cursor accuracy, dragging, and two sensor bar positions |

| Criterion | Evidence tasks | Status |
| --- | --- | --- |
| SC-001 | T017/T026 | Physical test pending |
| SC-002 | T020/T021/T045 | Pending |
| SC-003 | T034/T038/T039 | Sustained target verified with one and four simulated sessions and real ViGEmBus. Repeat with Bluetooth input |
| SC-004 | T020, `RecoveryTests.FourReadersKeepAllOneHundredThousandTransitionsOnTheirOwnOutputs` | Verified. 100,000 synthetic transitions in order, without crossed input, finishing neutral |
| SC-005 | T038 | Within budget with four real outputs, simulated input, off-screen WinForms interaction, minimizing, and export. A game with physical controllers remains pending |
| SC-006 | T036/T038 | Target verified with real XInput and simulated input. Repeat with a physical Wii Remote |
| SC-007 | T040 | Physical test pending |
| SC-008 | T015/T041/T045 | Pending |
| SC-009 | T017/T027/T030–T033 | Pending |
| SC-010 | T026/T042–T044/T055 | ZIP and resource verified. Clean PC test pending |
| SC-011 | T052 | Physical sensor bar and Wii Remote test pending |
| SC-012 | T054/T056 | LEDs 1–4 verified individually and matched in an actual Xbox 2 session. Multiplayer and reconnection remain pending |

The enabled-versus-disabled capture comparison passed with four real ViGEmBus outputs and simulated input. The same external measurement instrument timed transport completion through ViGEmBus submission in both cases. The highest per-session p99 was 0.378 ms in the best capture-disabled round and 0.318 ms in the worst capture-enabled round. Short rounds on this PC do not establish an improvement from capture or performance with physical Bluetooth traffic.
