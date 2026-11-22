# Tasks: Spanish and English Localization

**Input**: [Specification](spec.md), [plan](plan.md), [research](research.md), [data model](data-model.md), and [UI contract](contracts/interface.md).

## Phase 1: Setup

- [x] T001 Verify the active feature, the Spec Kit checklist, and project ignore rules in `.specify/feature.json`, `checklists/requirements.md`, and `.gitignore`.

## Phase 2: Foundation

- [x] T002 Implement display-language matching, saved language modes, and resource lookup in `src/Wii2Xbox/UiLanguage.cs` and `src/Wii2Xbox/Program.cs`.

## Phase 3: US1 — Launch in the Expected Language (P1)

**Independent test**: Launch without a saved preference and verify the selected language and the visible controls.

- [x] T003 [US1] Add parallel English and Spanish catalogs in `src/Wii2Xbox/Resources/UiText.resx` and `src/Wii2Xbox/Resources/UiText.es.resx`.
- [x] T004 [US1] Localize the main window, tray, pairing, profile editor, IR calibration, viewer, and application-owned statuses and errors in `src/Wii2Xbox/`. Keep localized status lookups out of report processing.

## Phase 4: US2 — Choose and Keep a Language (P1)

**Independent test**: Choose a language while the app is running, reopen the same window, and then fully relaunch.

- [x] T005 [US2] Add System, Español, and English choices with a pending restart message in `src/Wii2Xbox/MainForm.cs`. Save only the language preference and leave active sessions running.
- [x] T006 [US2] Test language matching, preference precedence, resource coverage, English controls, and user-entered profile names in `tests/Wii2Xbox.Tests/LocalizationTests.cs`.

## Phase 5: US3 — Use One Bilingual Installer (P2)

**Independent test**: Open the same MSI in Spanish, English, and an unsupported Windows display language.

- [x] T007 [US3] Implement bilingual UI selection and the localized setup wizard in `installer/` and `scripts/package.ps1`.
- [x] T008 [US3] Verify MSI language selection on the Spanish development account and ZIP/MSI payload parity with `scripts/package.ps1` and `quickstart.md`.

## Final Phase: Verification

- [x] T009 Run the automated tests and the local packaging checks. Record results and any remaining clean-Windows acceptance in `quickstart.md`.
- [ ] T010 Verify the full display-language and install/update/uninstall matrix on isolated Windows 10 and 11 accounts using `quickstart.md`.

## Dependencies and Parallel Work

T001 → T002 → T003/T004 → T005 → T006. The setup track T007 can run independently after T001. T008 requires both the localized app and installer. T009 follows T006 and T008. T010 covers release acceptance on clean Windows.
