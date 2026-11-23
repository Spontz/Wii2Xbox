# Tasks: Low-Latency Wii2Xbox

**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/pipeline.md](contracts/pipeline.md).

Each task lists its ID, story, and relevant file. `[x]` indicates that the task has evidence. See [acceptance](acceptance.md) for remaining physical checks.

## Phase 1: Setup

- [x] T001 Initialize Spec Kit 1.1.3 with Codex and PowerShell integration in `.specify/` and `.agents/skills/`.
- [x] T002 Establish the constitution, full specification, checklist, and active feature in `.specify/memory/constitution.md`, `.specify/feature.json`, and `specs/001-wii-xbox-low-latency/`.
- [x] T003 Create and verify the desktop scaffold, build-output exclusions, and root guide in `src/Wii2Xbox/`, `.gitignore`, and `README.md`. Release build passed with zero errors and warnings on 2026-11-22. This verifies the scaffold.
- [x] T004 Migrate to .NET 10 LTS and pin SDK and dependencies. Verified the Release build.
- [x] T005 Create the test project and reproducible build and test scripts. Eleven cases passed.

## Phase 2: Foundation and Feasibility

- [ ] T006 Test the signed backend on target Windows, review licensing and maintenance, and decide whether to adopt or replace it in `specs/001-wii-xbox-low-latency/research.md` and `docs/compatibility.md` (FR-017).
- [x] T007 Define HID transport, Xbox output, and session in `HidTransport.cs`, `XboxOutput.cs`, and `ControllerSession.cs`. Session tests exercise the same contracts with simulated transports.
- [ ] T008 Implement stable identity, model filtering, and the capability catalog in `src/Wii2Xbox/HidTransport.cs` (FR-003/021).
- [ ] T009 Introduce typed states and errors with idempotent cleanup in `src/Wii2Xbox/ControllerSession.cs` (FR-011).
- [ ] T010 Create real or referenced report, ACK, error, and truncation fixtures in `tests/Wii2Xbox.Tests/ProtocolTests.cs`. Keep control responses separate from input (FR-006).

## Phase 3: US1 — Immediate Response with One Real Controller (P1)

**Checkpoint**: One real Wii Remote controls a game through one output, which releases on stop.

- [ ] T011 [US1] Complete parsing with ID, length, and bit validation in `src/Wii2Xbox/Input.cs`. Test combinations and sensors in `tests/Wii2Xbox.Tests/ProtocolTests.cs` (FR-006).
- [ ] T012 [US1] Open and configure the Wii Remote, negotiate modes, and validate ACK with cancellable reads in `src/Wii2Xbox/HidTransport.cs` (FR-007).
- [x] T013 [US1] Submit complete state, query the actual slot, and explain dependency errors in `XboxOutput.cs`. Verified with one and four XInput outputs (FR-004/005/017).
- [x] T014 [US1] Implement reader → mapping → submission without waiting for the UI and distinguish dropped reports in `ControllerSession.cs`. The performance record contains synthetic and real-output evidence (FR-013/014).
- [ ] T015 [US1] Neutralize on stop, failure, or partial startup. Closing unblocks reads in `src/Wii2Xbox/ControllerSession.cs` and `SessionTests.cs` (FR-011, SC-008).
- [ ] T016 [US1] Show the list, connect and stop actions, actual status, and input snapshots in `src/Wii2Xbox/MainForm.cs` (FR-001/005).
- [ ] T017 [US1] Physically test all buttons, the first game, and stopping with held input. Record evidence in `docs/compatibility.md` (SC-001/009).

## Phase 4: US3 — Up to Four Independent Sessions (P1)

**Depends on**: US1 and lifecycle control.

- [ ] T018 [US3] Implement identity-based exclusivity, capacity, and output ownership in `src/Wii2Xbox/SessionManager.cs` (FR-003/004/005).
- [x] T019 [US3] Display up to four sessions and the Xbox slot assigned by Windows in `src/Wii2Xbox/MainForm.cs` (FR-005).
- [ ] T020 [US3] Test 100,000 transitions, concurrent starts, isolated failures, and 100 cycles in `tests/Wii2Xbox.Tests/MultiControllerTests.cs` (SC-002/004).
- [ ] T021 [US3] Test four controllers for 60 minutes, a fifth controller, and unrelated Xbox controllers in `docs/compatibility.md` (SC-002).

## Phase 5: US2 — Guided Bluetooth and Recovery (P1)

**Independent test**: Discover and pair a new controller, cancel discovery, and recover a known controller without duplicates.

- [x] T022 [US2] Implement radio inventory, discovery, and pairing for Nintendo models in `src/Wii2Xbox/BluetoothService.cs` (FR-002). Real pairing remains in T026.
- [ ] T023 [US2] Implement cancellation within 5 s and report radio, access, and pairing errors (FR-002). An active search stopped in about 2.3 s with the local adapter. Other adapters still need verification.
- [x] T024 [US2] Guide users through pairing with the red SYNC button in `src/Wii2Xbox/PairingDialog.cs` and `docs/troubleshooting/bluetooth.md` (FR-002/018).
- [x] T025 [US2] Implement manual reconnection, up to four retries, device events, and re-enumeration after sleep in `SessionRecovery.cs` and `MainForm.cs` (FR-012). Physical testing remains pending.
- [ ] T026 [US2] Test -01/-TR models, 10 attempts per combination, cancellation, and absent radios in `docs/compatibility.md` (SC-001/010).

## Phase 6: US4 — Complete Profiles and Nunchuk (P2)

- [x] T027 [US4] Version the schema and validate the catalog, opposite directions, and multiple sources in `Input.cs` and `ProfileTests.cs` (FR-009/010).
- [x] T028 [US4] Implement loading, atomic save, and import/export with validation in `ProfileStore.cs` and `ProfileDialog.cs` (FR-009/019).
- [x] T029 [US4] Add a per-controller editor and apply the compiled mapping after a neutral report in `MainForm.cs` and `ControllerSession.cs` (FR-009/010). Verification with two physical sessions remains in T033.
- [x] T030 [US4] Create and package the horizontal and Nunchuk profiles in `profiles/horizontal.json` and `profiles/nunchuk.json`. Verified rotation and release with simulated reports (FR-007).
- [x] T031 [US4] Implement Nunchuk negotiation, identification, write ACK, calibration, and hot removal in `NunchukProtocol.cs` and `HidTransport.cs` (FR-008/021). Physical accessory acceptance remains in T033.
- [x] T032 [US4] Test calibration extremes, center and dead zone, unknown accessory IDs, and invalid profile rejection in `NunchukTests.cs` and `ProfileTests.cs`. Physical testing remains in T033 (SC-009).
- [ ] T033 [US4] Verify accessories and profiles after restart and with two different controllers in `docs/compatibility.md` (SC-009).

## Phase 7: US5 — Measure and Optimize Latency (P1)

T014's architecture avoids waits from the start. This phase adds evidence and optimization.

- [x] T034 [US5] Capture monotonic timestamps in a bounded buffer and cadence snapshots in `src/Wii2Xbox/LatencyMetrics.cs` (FR-015).
- [x] T035 [US5] Export processing p50/p95/p99/max, schema, and conditions in `src/Wii2Xbox/LatencyReport.cs` for development tests (FR-016). End-to-end physical measurement remains in T040.
- [x] T036 [US5] Create an independent XInput observer and transition comparison with frequency and uncertainty. `scripts/test-native.ps1` prepares them outside the user package (FR-016/018, SC-006).
- [x] T037 [US5] Verify percentiles, buffer boundaries, allocations, and diagnostic overhead. Unit tests and the enabled/disabled comparison passed with actual ViGEmBus and simulated input. Physical measurement remains in T038 (FR-015/016).
- [ ] T038 [US5] Test one and four controllers, idle/used/minimized UI, and diagnostics off/on (SC-003/005/006). Short and sustained native cases passed with one and four outputs and simulated input. See [acceptance](acceptance.md) and the physical protocol in `docs/hardware-validation.md`.
- [ ] T039 [US5] Compare HID policy, reports, and priority against an equivalent baseline. Keep measured settings and record alternatives with the performance evidence (FR-013/014/020).
- [ ] T040 [US5] Run a physical press-to-response trial with a sample sufficient for percentiles and record instrument limits with the performance evidence (SC-007).

## Phase 8: US6 — Distribution, Errors, and Acceptance (P2)

- [ ] T041 [US6] Watchdog and shutdown with held input verified with simulated transports in `RecoveryTests.cs`. Physical radio loss and sleep trials remain pending (FR-011/012, SC-008).
- [x] T042 [US6] Publish a reproducible, self-contained Windows x64 package with licenses and versions in `scripts/publish.ps1` and `docs/installation.md` (FR-017/018).
- [ ] T043 [US6] Validate driver installation from Wii2Xbox and the first virtual controller on clean Windows without the SDK or Internet. Repeat with denied elevation and a required restart. Record results in `acceptance.md` (SC-010).
- [ ] T044 [US6] Document HID access, Bluetooth, XInput limits, unverified models, and restrictive games in `docs/troubleshooting.md` (FR-017/018/021/022).
- [ ] T045 [US6] Verify shutdown ≤3 s, neutralization ≤1 s, and resources after 100 reconnections in `docs/compatibility.md` (SC-002/008).

## Phase 9: Convergence and Final Delivery

- [x] T046 Audit FR-001–FR-026 and SC-001–SC-011 against code and evidence in `specs/001-wii-xbox-low-latency/acceptance.md`. Record outstanding physical checks separately from simulations.
- [ ] T047 Run the final build, test, and publish steps and test the package on a clean PC. Record commands, versions, and results in `acceptance.md`.
- [ ] T048 Document remaining limits and risks and verify that `README.md` and `docs/performance.md` avoid unmeasured latency claims.
- [ ] T049 Update tasks and spec status based on actual evidence. Declare completion only after all mandatory requirements are verified.

## Dependencies and Traceability

### IR Sensor Bar Mouse

- [x] T050 Implement ACK-based activation, point decoding, and combined Xbox/Nunchuk sessions (FR-023/024).
- [x] T051 Implement cursor movement, clicks, and release on tracking loss. Verify with simulated reports (FR-024/025).
- [ ] T052 Verify tracking, clicks, and combined use with a physical Wii sensor bar and Wii Remote (SC-011).
- [x] T053 Implement two-point IR mouse calibration and save it with the controller profile (FR-026). Physical testing is part of T052.

### Identification and Distribution

- [x] T054 [US3] Match the **Xbox** column and physical LED to the actual slot. Verified all four HID commands with one Wii Remote and Xbox 2 with Xbox 1 occupied (FR-005).
- [x] T055 [US6] Embed the signed installer, verify the published resource, and remove the observer and comparator from the ZIP. Cover detection in `DriverAvailabilityTests.cs`. Clean Windows installation is covered in T043 (FR-017/018).
- [ ] T056 [US3] Repeat the LED and **Xbox** column match in every slot, after reconnection, and with multiple physical Wii Remotes (SC-012).
- [ ] T057 [US6] Compare XInput, Windows.Gaming.Input, and ControllerTest in Edge with the embedded official installer on clean Windows. Record detection in an XInput game (FR-022).
- [x] T058 [US2] Start discovery automatically in the wizard and cancel it on close. Remove the Windows Bluetooth settings shortcut (FR-002).
- [x] T059 [US1, US6] Activate available controllers when detected, apply the selected output, and check the Xbox driver at startup. Retain capture with active sessions for development tests (FR-004/015/017).
- [ ] T060 [US3] Verify automatic activation of multiple physical Wii Remotes at startup, on connection, and on reconnection, with independent output changes (FR-004/005).
- [x] T061 Rename the wizard to **Añadir mando** (Add controller) and simplify the window to four columns. Keep measurements in development tests (FR-002/015/016).

Setup → backend and transport gate → US1 → US3/US2/US4 → full US5 measurement → US6 → convergence. Protocol and mapping tests can proceed without hardware. The physical gates require hardware.

| Requirements | Main tasks |
| --- | --- |
| FR-001/005 | T016/T019/T023/T054/T056/T060 |
| FR-002/003/012 | T008/T018/T022–T026/T058 |
| FR-004/006/007 | T010–T014/T018/T030/T059/T060 |
| FR-008/009/010 | T027–T033 |
| FR-011/013/014 | T009/T014/T015/T020/T039/T041 |
| FR-015/016/020 | T034–T040/T059 |
| FR-017/018/019 | T006/T028/T036/T042–T044/T055/T059 |
| FR-021/022 | T008/T026/T031/T033/T044/T057 |

The demonstrable MVP covers T001–T017. Delivery completes when the applicable acceptance criteria in `spec.md` pass.

## Implemented Increment

The local package supports pairing, up to four independent outputs, slot indication with LEDs, profiles, Nunchuk, IR mouse, and installation of the Xbox driver from Wii2Xbox. [Acceptance](acceptance.md) records evidence and remaining checks.
