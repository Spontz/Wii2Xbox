# Compatibility

| Component | Test | Result |
| --- | --- | --- |
| Windows x64 package | Startup and shutdown on the development PC | Passed |
| ViGEmBus 1.22.0 | Authenticode signature of the official release installer | Valid. Signed by Nefarius Software Solutions e.U. |
| Missing or incompatible ViGEmBus | In-app Xbox output check | Messages verified with simulated errors |
| ViGEmBus on the test PC | Windows 26300.10075, driver 1.17.333.0, Bigscreen Gamepad Emulation Service | One and four outputs worked. XInput observed buttons, axes, triggers, release, and disconnection |
| 100 virtual output cycles | 25 batches of four virtual controllers | Passed. All four slots were available afterward |
| Self-contained ZIP | Fresh extraction, system PATH, no .NET paths, and no elevation | Four-controller self-test passed |
| Nunchuk and IR sensor bar | Simulated reports | Automated tests passed |

The verified installer was `ViGEmBus_1.22.0_x64_x86_arm64.exe`, SHA-256 `89220A7865076B342892F98865F3499FB7C4CFD673159E89D352C360FD014C6A`. [Official release](https://github.com/nefarius/ViGEmBus/releases/tag/v1.22.0)

Integration tests ran with the existing driver. Its `ViGEmBus.sys` file had a valid Authenticode signature from Microsoft Windows Hardware Compatibility Publisher.
