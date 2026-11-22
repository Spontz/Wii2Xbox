# Research and Decisions

**Date** 2026-11-22  
**Status** Decisions implemented with partial validation.

## Runtime and UI

The published app uses WinForms and .NET 10 LTS. The package includes the runtime so Wii2Xbox can run without the SDK. [NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

WPF offers visual composition without reducing transport latency. C++ may offer more runtime control. Consider it only if profiling shows that the runtime prevents meeting the budget. Measure latency rather than inferring it from the programming language.

## Xbox Emulation

Wii2Xbox uses ViGEmBus and its .NET bindings for Xbox 360 output. The package embeds the signed 1.22.0 installer and includes its three-clause BSD license. Record acceptance separately for each Windows version and game combination. [1.22.0 release](https://github.com/nefarius/ViGEmBus/releases/tag/v1.22.0) · [License](https://github.com/nefarius/ViGEmBus/blob/v1.22.0/LICENSE) · [End of support](https://docs.nefarius.at/projects/ViGEm/End-of-Life/)

Publishing fetches the pinned version and verifies its SHA-256 and Authenticode signature. At runtime, the app installs from the embedded resource with `/exenoui /qn /norestart` from the WinGet manifest and requests Windows elevation after a user action. The output interface allows another backend to be evaluated if the matrix reveals incompatibilities. [WinGet manifest](https://github.com/microsoft/winget-pkgs/blob/master/manifests/v/ViGEm/ViGEmBus/1.22.0/ViGEm.ViGEmBus.installer.yaml)

## Transport

[HidSharp](https://github.com/IntergatedCircuits/HidSharp) is a candidate for HID enumeration and reading. Validate the source and test real Windows transport with RVL-CNT-01 and RVL-CNT-01-TR to determine which writes each model accepts. Compare change-only basic reporting against continuous reports. Continuous reports help detect silence but may increase radio traffic. Avoid presuming a 1,000 Hz rate or changing polling for a device that does not support it.

The archived [HID-Wiimote source and documentation](https://github.com/jloehr/HID-Wiimote) are the primary interoperability reference. Use them as a protocol reference. Nunchuk support requires identification, initialization, encryption handling, and calibration. A six-byte block alone is insufficient evidence of support.

## Output Observation

Games and observers use [XInput](https://learn.microsoft.com/en-us/windows/win32/xinput/xinput-game-controller-apis-portal) to read states. Design observations around sampled states. The independent observer records read frequency and aliasing. Local measurement excludes time before the read returns to the process, radio latency, and time to game rendering.

ControllerTest receives states through the browser Gamepad API. On Windows, Chromium has data fetchers based on Windows.Gaming.Input and GameInput. The result seen on the reference machine is documented in [acceptance](acceptance.md). [Chromium source](https://chromium.googlesource.com/chromium/src/+/lkgr/device/gamepad/gamepad_platform_data_fetcher.h) · [ControllerTest](https://controllertest.io/)

## Open Questions

1. Installation of the signed backend on target Windows systems and the effect of other XInput outputs.
2. Pairing, the HID write method by model and adapter, and cancellation within 5 seconds.
3. Reliable Nunchuk identification, calibration, and hot removal.
4. Optimal buffering and report timing for one and four controllers sharing a radio.
5. Actual latency distribution and instrumentation overhead. Choose settings based on equivalent trials.

See [acceptance](acceptance.md) for hardware results and the limits of each test.
