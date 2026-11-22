# Data Model and Lifecycle

## PhysicalController

Stable `Id` when the transport permits it, ephemeral `DevicePath`, model, radio, capabilities, identified accessory, monotonic timestamp of the last valid report, and state. Use a device identifier for identity. If the identity changes, avoid automatically assigning profiles from another controller.

## ControllerSession

`SessionId`, `PhysicalId`, owned output, profile snapshot, state, connection generation, bounded diagnostics, and cancellation. Each physical controller has at most one unfinished session, each output has one owner, and up to four sessions can be active. A device has at most one reader.

States: Discovered → Connecting → Configuring → Active → Stopping → Disconnected. Failure at any stage → cleanup → Faulted. A retry creates a new generation and ignores callbacks from previous generations. Cleanup is idempotent, including after a partial open or a blocked read.

## WiiState / XboxState

Wii state contains the button set, profile orientation, valid accessory, calibrated stick and C/Z when applicable, and a local sequence number. Xbox state contains a button mask, two pairs of signed axes, and two triggers ranging from 0 to 255. Unused components remain neutral. Unknown or truncated reports leave button state unchanged and do not count as valid watchdog data.

## ProfileDocument v1

`schemaVersion: 1`, `id`, `name`, `horizontal|upright` orientation, source-to-target mappings, and per-axis Nunchuk center/minimum/maximum calibration and dead zone. Sources and targets come from closed catalogs. Multiple sources for one target are combined with OR. Releasing one leaves the target active while another is active. Opposite directions neutralize each other. Reject out-of-range values, degenerate calibration, and unknown versions before applying the profile.

Save to a temporary file and replace atomically, preserving the valid profile. Migrations are explicit. The exploratory Profile type in Input.cs does not yet implement the full contract.

## ConfigurationDocument v1

Known profiles, the last physical-controller-to-profile association, UI preferences, and whether diagnostics are enabled. Do not persist pressed state or assume a guaranteed virtual index. Store files in the user's local directory.

## PerformanceRun

Version and build, OS and runtime, CPU, radio, controllers and accessories, backend and driver, game if applicable, report mode, conditions, duration, warm-up, sample size, observer frequency, uncertainty, losses, and p50/p95/p99/max. Keep `software-processing`, `virtual-observed`, and `physical-to-response` as separate types. Record samples in microseconds or ticks with a declared frequency. Keep incompatible metrics separate.
