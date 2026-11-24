# Wii2Xbox Constitution

## Core Principles

### I. Minimum latency

Minimizing added latency and variation is the product's priority. Process each report as it arrives, without waiting for the UI, storage, or timers. Compare every optimization against a reproducible baseline. Measure software timing and physical latency separately.

### II. Isolation and neutralization

Each physical controller maps to one virtual output. A failure, stop, profile change, or disconnect must neutralize its output without affecting other players. Prevent duplicate outputs and stuck buttons. Support up to four XInput outputs.

### III. Compatibility evidence

Record synthetic tests, virtual integration tests, and physical trials separately. A build or simulation cannot establish real Bluetooth behavior, game compatibility, or end-to-end latency. Each controller model and accessory requires its own evidence.

### IV. Transparent desktop behavior

Show the actual status of Bluetooth, controllers, and emulation. Install drivers explicitly from identified sources and run the application without elevation in normal use. Disclose any changes to Windows security, power, or global timer settings. Store settings locally and operate without accounts or external telemetry.

### V. Specification-driven development

The specification defines behavior, the plan explains the architecture, and the task list records implementation and evidence. Test protocol handling, isolation, neutralization, and latency regressions. Keep unverified requirements open.

All feature specifications and their supporting artifacts must be written and maintained in English. This includes plans, tasks, research, checklists, contracts, data models, quickstarts, and acceptance records. Contributors may describe requirements in Spanish. Translate them into English when creating or updating the project documents.

### VI. Portable ZIP and MSI distribution

Each Windows x64 release must produce both a portable ZIP and an MSI that supports installation and uninstallation. Both artifacts must come from the same Release build and include the project's license, attribution, and third-party notices.

Every version must have English release notes committed to `CHANGELOG.md` before
publication. Both GitHub releases for that version must include the same list of
user-facing changes. Include upgrade or compatibility instructions when relevant.

## Quality Gates

Require a Release build, meaningful protocol and mapping tests, four-output integration tests, reproducible latency percentiles, and physical acceptance tests. Before publishing a release, verify that the extracted ZIP launches and that the MSI installs, launches, and uninstalls on Windows without the SDK. Keep product acceptance open while physical tests are pending.

## Governance

Document each exception in the specification with its rationale, impact, evidence, and a corrective task. Update requirements and tasks before declaring scope changes delivered. Document any relaxation of a latency target explicitly.

Record constitution amendments with their rationale and effective date. Use a major version for incompatible changes to governance, a minor version for new or substantially expanded rules, and a patch version for clarifications. Review compliance when preparing feature plans and before publishing a release.

**Version**: 1.3.0 | **Ratified**: 2026-11-22 | **Last Amended**: 2026-11-24
