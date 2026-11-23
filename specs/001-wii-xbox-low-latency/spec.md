# Feature Specification: Low-Latency Wii2Xbox

**Feature Directory**: `specs/001-wii-xbox-low-latency`

**Created**: 2026-11-22

**Status**: Implementation available and partially validated. See [evidence and pending checks](acceptance.md).

**Input**: A Windows app that connects one or more Wii Remotes over Bluetooth and turns them into independent Xbox controllers for gaming. The explicit priority is minimum latency. Initialize Spec Kit and specify the full development scope.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Play with immediate response (Priority: P1)

As a player, I want to use my Wii Remote as an Xbox controller with as little delay and variation as possible.

**Why this priority**: It is the core goal and the user's explicit priority.

**Independent Test**: Use one physical controller, one virtual output, and a compatible game. Check buttons and measure response with an independent consumer of the output.

**Acceptance Scenarios**:

1. **Given** the Xbox driver is available, **When** Wii2Xbox finds a Wii Remote at startup or on connection, **Then** it automatically creates an Xbox output and each button produces the expected input.
2. **Given** conversion is active, **When** rapid button presses arrive, **Then** changes are sent in order without intentional delay or spurious presses.
3. **Given** a game is active, **When** I minimize or interact with the UI, **Then** input stays within the latency budget.
4. **Given** a held button, **When** I change output or exit Wii2Xbox from the notification area, **Then** the old output is neutralized and released.

### User Story 2 - Connect over Bluetooth (Priority: P1)

As a player, I want to discover and connect my controllers through a guided flow that explains each state and error.

**Why this priority**: Reliable connection is essential to playing.

**Independent Test**: Use an unpaired controller to test synchronization, discovery, connection, and cancellation. Repeat without a radio.

**Acceptance Scenarios**:

1. **Given** a compatible radio, **When** I select **Añadir mando…** (Add controller…), **Then** the app searches automatically and lists controllers as it finds them.
2. **Given** the radio is absent or off, **When** I attempt to connect, **Then** the app explains the cause and gives a specific action instead of showing a false ready state.
3. **Given** a known controller is available again, **When** I reconnect, **Then** one session resumes without duplicates.
4. **Given** an active search, **When** I close **Añadir mando** (Add controller), **Then** it stops within 5 seconds and other sessions continue.

### User Story 3 - Support up to four players (Priority: P1)

As a group, we want each Wii Remote to control a different Xbox player.

**Why this priority**: The goal includes multiple controllers and independent outputs.

**Independent Test**: Use four controllers with different input combinations. Remove one and verify the others stay isolated.

**Acceptance Scenarios**:

1. **Given** four controllers are available, **When** conversion starts, **Then** there are four independent outputs with visible physical-to-virtual assignments.
2. **Given** the output limit is reached, **When** I add another controller, **Then** the app explains the limit and preserves the existing players.
3. **Given** other Xbox controllers in Windows, **When** conversion starts and XInput assigns a slot, **Then** the **Xbox** column shows that number and only the matching LED lights on the Wii Remote.
4. **Given** multiple sessions, **When** one fails, **Then** only that session's output is neutralized.

### User Story 4 - Use profiles and Nunchuk (Priority: P2)

As a player, I want to configure each controller for different games, either horizontally or with a Nunchuk.

**Why this priority**: Profiles adapt controls to games, and the Nunchuk adds analog input.

**Independent Test**: Use two different profiles on two controllers. Save, restart, and verify orientation, mappings, and the calibrated stick.

**Acceptance Scenarios**:

1. **Given** a horizontal profile, **When** I press the D-pad, **Then** the game receives the visually correct direction.
2. **Given** a compatible Nunchuk, **When** I move the stick or press C/Z, **Then** I get the mapped axes and inputs, and the calibrated center does not drift.
3. **Given** the editor is open, **When** I change a mapping, **Then** I can choose Xbox buttons, D-pad directions, bumpers, stick clicks, digital triggers, or disable the input.
4. **Given** held buttons, **When** I change profile, **Then** the previous state is neutralized before the new profile applies.
5. **Given** an invalid profile or unknown accessory, **When** I load or connect it, **Then** the app shows a useful error, retains valid configuration, and produces no spurious input.

### User Story 5 - Measure and optimize latency (Priority: P1)

As a player, I want measurements to distinguish the delay added by Wii2Xbox, Bluetooth, and the game's response.

**Why this priority**: Minimum latency requires evidence and valid comparisons.

**Independent Test**: Run reproducible trials for one and four inputs, export percentiles, use an independent observer, and measure physical latency.

**Acceptance Scenarios**:

1. **Given** a session in the test environment, **When** processing is captured, **Then** the report identifies cadence, window, samples, and measurement scope.
2. **Given** a performance trial, **When** its result is exported, **Then** it includes median, p95, p99, maximum, duration, samples, version, hardware, mode, and conditions.
3. **Given** diagnostics are off, **When** I play, **Then** measurement introduces no per-report logging or waits.
4. **Given** a candidate optimization, **When** I compare it with an equivalent baseline, **Then** retain it if it reduces delay or variation without losing transitions.

### User Story 6 - Install and recover from failures (Priority: P2)

As a user, I want to run without the SDK and recover from disconnections without stuck buttons.

**Why this priority**: It makes the app usable outside the development environment.

**Independent Test**: Use a clean PC and test an absent driver, turning off the controller, sleep/resume, and exiting with a held button.

**Acceptance Scenarios**:

1. **Given** Windows without the SDK, **When** I run the package, **Then** it opens and checks prerequisites.
2. **Given** Windows without the Xbox driver or Internet access, **When** I accept the installation offer shown on opening Wii2Xbox and authorize installation in Windows, **Then** Wii2Xbox installs the embedded driver, checks the result, and reports whether Windows requires a restart.
3. **Given** a held button, **When** I remove the batteries or turn off the radio, **Then** output is neutralized within 1 second of observable communication loss.
4. **Given** sleep and resume, **When** Bluetooth returns, **Then** I can recover without duplicate outputs or impact on unrelated devices.
5. **Given** the app is active, **When** I select **Salir** (Exit) from the notification area, **Then** discovery, readers, and outputs stop. See the [background operation specification](../002-background-startup/spec.md) for what happens when only the window closes.
6. **Given** Windows cancels elevation or the installer returns an error, **When** the attempt finishes, **Then** Wii2Xbox reports the outcome, allows retry where appropriate, and keeps **Ratón IR** (IR mouse) available.

### Edge Cases

- Power-off during discovery, negotiation, or configuration. Double-clicking Connect. The list changing during an action.
- Four outputs plus other XInput controllers. Slot reassignment by Windows. Physical identity versus slot.
- Incomplete or unknown reports. Status and write responses interleaved with buttons.
- Encrypted, uninitialized, or hot-unplugged Nunchuk, or an incompatible clone.
- Opposite directions, multiple inputs for one destination, and corrupt, empty, or future-version profiles.
- RVL-CNT-01 versus RVL-CNT-01-TR, unverified clones, low battery, and radio interference.
- Blocked read during shutdown, radio silence without notification, denied HID access, and virtual driver restart.
- Games can impose their own restrictions on virtual controllers. Compatibility depends on the game.
- An output that works in Windows may be missing in a browser. Verify game input and browser detection in their own environments.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Run on Windows 10/11 x64 with a Spanish UI and no required console.
- **FR-002**: Discover controllers automatically while the wizard is open. Illustrate the red SYNC button and use it as the sole pairing method. Pause discovery during pairing and cancel on close. Close the wizard on success only when the selected remote appears in the main list. If Windows still has a disconnected association for that remote and does not publish its HID interface in time, clear that association and retry pairing the same remote once without user intervention. Distinguish an absent radio, failed pairing, and denied access.
- **FR-003**: Enumerate connected controllers and deduplicate by physical identity.
- **FR-004**: Activate available Wii Remotes automatically, with up to four independent Xbox 360 outputs for XInput games. Default to Xbox output and apply an output change only to the selected controller.
- **FR-005**: Show each controller's physical connection, state, errors, and profile. When Windows assigns an Xbox output, show its slot 1–4 in **Xbox** and light only the matching LED on the Wii Remote. Use the actual assignment after reconnection.
- **FR-006**: Decode A/B/1/2/+/−/HOME/D-pad without treating sensor bits as buttons.
- **FR-007**: Include horizontal and Wii Remote + Nunchuk profiles. Request only necessary sensors in button mode.
- **FR-008**: Identify and initialize the Nunchuk, calibrate center, range, and dead zone, and neutralize its input on removal.
- **FR-009**: Edit, validate, save, import, export, and apply profiles per controller. Keep the last valid profile on error. Group Wii Remote bindings and Nunchuk bindings and dead zone separately. Fit the editor without vertical scrolling, display the app icon in its title bar, and close unsaved edits through the title-bar close control or Escape.
- **FR-010**: Map buttons, D-pad, bumpers, stick clicks, and digital triggers. Read analog axes from the Nunchuk stick.
- **FR-011**: Neutralize and release owned outputs when a session ends, communication fails, or the user selects **Salir** (Exit). Leave unrelated outputs independent.
- **FR-012**: Support manual reconnection and bounded retries without duplicate sessions.
- **FR-013**: Deliver input on arrival independently of UI, discovery, storage, and diagnostics, without an added periodic wait.
- **FR-014**: Preserve ordered transitions and avoid unbounded queues. Identify the reason for every dropped report.
- **FR-015**: Measure processing and cadence in the test environment with bounded samples, units, and a time window. The controller list displays **Mando** (Controller), **Xbox**, **Estado** (Status), and **Perfil** (Profile).
- **FR-016**: Export measurements with percentiles and conditions in development tests. Separate processing, virtual observation, and end-to-end measurement.
- **FR-017**: Check virtual driver availability at startup and report absence, incompatibility, or blocked access. If installation is available, ask once when the main window opens. Start installation only after the user accepts. Embed the signed official installer and support offline installation with Windows elevation. Recheck afterward and report cancellation, failure, or a required restart. Keep **Ratón IR** (IR mouse) available without the Xbox driver.
- **FR-018**: Ship a Windows package with the runtime, licenses, and guides. Keep the XInput observer and comparison tool in development tests.
- **FR-019**: Keep configuration and diagnostics local. Allow offline play without an account after installing prerequisites.
- **FR-020**: Leave sensors, rumble, and global changes disabled by default. Optional improvements must respect the latency budget.
- **FR-021**: Report unsupported models or accessories and retain safe basic input when possible.
- **FR-022**: Record a matrix of models, accessories, adapters, Windows versions, drivers, and games actually tested.

### Key Entities

- **Physical controller**: Identity, model, connection, accessory, capabilities, and last valid report.
- **Session**: One controller, its output, applied profile, lifecycle state, and error.
- **Xbox output**: Buttons, axes, triggers, system index, and owning session.
- **Profile**: Name, version, orientation, mappings, calibration, and dead zone.
- **Measurement**: Delay type, units, samples, duration, percentiles, and conditions.
- **Compatibility**: Hardware and software combination, cases run, result, and evidence.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: From synchronization to visible Xbox input in ≤60 s, excluding installation and user actions, in ≥9/10 attempts per supported combination.
- **SC-002**: Four controllers for 60 minutes without crossed inputs, duplicates, or stuck buttons. Complete 100 connect/disconnect cycles without leaking session resources.
- **SC-003**: From a complete report becoming available to output submission, processing has p95 ≤1 ms and p99 ≤2 ms, with one and four controllers for 10 minutes and ≥10,000 reports per controller. These are acceptance targets for software processing.
- **SC-004**: 100,000 synthetic press/release transitions reach the correct target in order, with no spurious events or active final states.
- **SC-005**: UI interaction or minimization and export increase processing p99 by ≤0.5 ms compared with the equivalent idle run.
- **SC-006**: On reference hardware, app receipt to independently observed virtual state has p95 ≤4 ms. State the observer's frequency and uncertainty. Radio latency is outside this interval.
- **SC-007**: Delivery includes a physical press-to-response trial with median, p95, and p99, video and game conditions, and a comparison of configurations. Set an absolute target only after measuring hardware.
- **SC-008**: Neutralize within 1 s of observable communication loss. On session stop or **Salir** (Exit), neutralize when processing the command and release resources within 3 s.
- **SC-009**: All buttons, diagonals, ranges and centers, Nunchuk, and hot removal pass in the supported matrix. An invalid profile never replaces a valid one.
- **SC-010**: On Windows without the SDK or Xbox driver, the package opens, installs the driver offline after user authorization, and permits checking Xbox output. Cover cancellation, required restart, radio off, and incompatible hardware without an unexpected exit. The ZIP excludes project-specific auxiliary executables and diagnostic scripts.
- **SC-011**: With a physical sensor bar, verify movement in all four directions, clicks and dragging, tracking loss and recovery, removing batteries while holding a click, and simultaneous Xbox output. Test the mouse without ViGEmBus and verify calibration for two bar positions.
- **SC-012**: For each slot 1–4, the **Xbox** column matches the only lit LED. Repeat with a slot already occupied and after reconnecting.

## Assumptions

- “Different Xbox controllers” means multiple independent players. The initial output is Xbox 360/XInput. Native Xbox One and Series protocols are outside scope.
- Initial delivery targets Windows 10/11 x64 and original Wii Remote and Wii Remote Plus, with or without Nunchuk. Each combination requires verification. Clone compatibility requires evidence.
- Up to four outputs share availability with other XInput controllers. Windows assigns the indices.
- Compatible Bluetooth Classic is required. Windows pairing is a guided fallback if the built-in flow fails.
- Installing the embedded driver uses Windows elevation after an explicit action. Normal Wii2Xbox use runs without elevation.
- Minimum latency means reducing measured added delay and variation within the limits of the controller, radio, Windows, and game. Changes to the initial numerical budgets must be explicit.
- This delivery excludes Classic Controller, Balance Board, guitars, keyboard, gestures, MotionPlus sensors, macros, and networking. Bidirectional rumble is a later improvement subject to measurement.

## Mouse Input with the Wii Sensor Bar

- **FR-023**: Choose Xbox, IR mouse, or both outputs for each Wii Remote. IR mouse-only mode must work without the virtual Xbox driver.
- **FR-024**: With a powered IR bar, point at the primary screen, move the cursor using the controller's camera, and use A/B for left/right clicks, including holding to drag.
- **FR-025**: At most one session controls the cursor. Losing tracking, stopping the session, or losing Bluetooth releases mouse buttons. Combined mode retains Xbox buttons and compatible Nunchuk input.
- **FR-026**: Calibrate the cursor by pointing at two on-screen targets. Save the result with the controller profile and allow cancellation or factory reset. Show at the bottom of the calibration screen that **Esc** cancels the calibration. Capturing points releases and blocks mouse clicks.
- **FR-027**: Show sensor bar instructions when the user activates IR output and the camera is still pending. Enable IR output for the selected controller if calibration needs it. If the camera responds promptly, open the calibration targets directly. During calibration setup, show the instructions only after the grace period. Close the instructions when the controller confirms its camera is ready, then continue to calibration if requested. Explain a delayed confirmation in the modal and allow the user to dismiss it.

## Delivery and Acceptance

Delivery includes source, initial profiles, tests, a package with the embedded installer, guides, the physical test matrix, and an acceptance report. Measurement tools are part of development tests.

Sequence: verifiable baseline → one real controller → four controllers → connection and recovery → profiles and Nunchuk → diagnostics and optimization → packaging and physical acceptance. Each success criterion needs evidence before the goal can be marked complete.
