# Planned Internal Contracts

The application provides no network API. These names identify boundaries to refine during implementation.

## IControllerTransport

- Enumeration returns identity, model, and capabilities without opening a second reader.
- Open and configure support cancellation and return typed errors.
- Read writes into a reusable buffer and returns the actual length and availability timestamp.
- ACK, status, and memory responses stay separate from input reports.
- Vibration changes serialize with other HID writes and preserve the rumble bit on ordinary commands.
- Close cancels pending reads and releases the handle. Repeated calls are safe.

## IXboxOutput

- Connect creates one owned output and returns the actual index when available.
- Submit accepts a complete state and sends one report.
- Feedback invokes the session's vibration callback when Xbox motor or LED feedback arrives. The Wii Remote motor is on when either Xbox motor is active.
- Neutralize sends zeros. Disconnect releases only that output, even if Neutralize fails.
- Failures propagate to the session, which stops reporting itself as active.

## SessionManager

- Start(physicalId, profile) is exclusive per device and respects available capacity.
- Stop(sessionId) and StopAll are idempotent.
- ApplyProfile validates first and neutralizes the old state at a safe boundary. The reader performs no disk I/O.
- UI snapshots and state do not block the transport. A session error leaves other sessions running.

## BluetoothDiscovery

- Search, Connect, and Cancel expose actual progress and failure causes.
- Cancellation completes within 5 seconds and preserves other sessions and pairings.
- Filter candidates by model and identity. Connect only to an identified controller.

## Diagnostics

- Record bounded timestamps without per-sample I/O. Calculate percentiles and export separately from processing.
- Disabling diagnostics stops optional capture. The UI consumes snapshots only.
- Exported reports include the schema version and test conditions. Report processing and physical latency separately.

## Profile Storage

- Load and Import validate the schema, catalog, and ranges before returning a snapshot.
- Save leaves no partial file and preserves the previous configuration on failure.
- For an invalid file, report the cause and location and keep the last valid profile.
