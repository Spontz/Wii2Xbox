# Feature Specification: Spanish and English Localization

**Feature Directory**: `specs/006-spanish-english-localization`

**Created**: 2026-11-22

**Status**: Implemented and locally verified. Clean Windows acceptance is tracked in [tasks.md](tasks.md).

**Input**: Support Spanish and English in Wii2Xbox.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Open Wii2Xbox in my language (Priority: P1)

As a player, I want Wii2Xbox to use my Windows display language on first launch so I can connect a Wii Remote and understand its status without visiting settings.

**Independent Test**: Start the portable or installed app on a Windows account with no saved Wii2Xbox language preference. Repeat with Spanish, English, and an unsupported Windows display language.

**Acceptance Scenarios**:

1. **Given** Windows uses any regional variant of Spanish, **when** Wii2Xbox opens in System mode, **then** the window, pairing dialog, and notification-area menu appear in Spanish.
2. **Given** Windows uses English or a language other than Spanish, **when** Wii2Xbox opens in System mode, **then** the same surfaces appear in English.
3. **Given** Wii2Xbox starts hidden at sign-in, **when** it displays a notification or the user opens the window, **then** both use the same language as a manual launch with the same preferences.

### User Story 2 - Choose a language independently of Windows (Priority: P1)

As a player, I want to choose Spanish or English within Wii2Xbox and keep that choice across launches.

**Independent Test**: Choose the opposite language to the Windows display language, exit Wii2Xbox fully with its notification-area menu, and reopen it. Then return to System mode and repeat.

**Acceptance Scenarios**:

1. **Given** Wii2Xbox is running, **when** the user chooses System, Español, or English in the language control, **then** the setting is saved and the app explains how to apply it at the next full launch. Active controllers continue sending input.
2. **Given** an explicit preference is saved, **when** the app is relaunched, **then** it uses that language regardless of the Windows display language. Choosing System restores automatic selection on the next full launch.
3. **Given** a language change is pending and the window is closed to the notification area, **when** the user reopens the same running instance, **then** it keeps its current language and indicates that the change will apply at the next full launch.

### User Story 3 - Install Wii2Xbox in my language (Priority: P2)

As a player using the MSI, I want the Wii2Xbox setup wizard to appear in a language I can read so I can follow installation and recognize its outcome.

**Independent Test**: Open the same Windows x64 MSI on accounts with Spanish and English display languages. Verify the welcome screen, installation progress, completion, and cancellation. Repeat with an unsupported Windows display language to exercise the fallback.

**Acceptance Scenarios**:

1. **Given** Windows uses Spanish or English, **when** the MSI opens, **then** the Wii2Xbox setup wizard uses the corresponding language. An unsupported display language falls back to English.
2. **Given** the wizard completes installation, **when** Wii2Xbox first opens, **then** the app uses its own saved preference if it exists, or the system default for a new user.
3. **Given** the user updates an existing installation, **when** setup finishes, **then** the user's language choice is still available in the app.

### Edge Cases

- Existing installations with no language preference, and missing or unrecognized preference values, use System mode. Existing profiles and controller settings remain available.
- Windows display language and regional formats can differ. Automatic selection follows the primary Windows display language. A Spanish regional variant uses Spanish and an English regional variant uses English.
- Changing Windows display language while Wii2Xbox is running takes effect for System mode on the next full launch.
- System dialogs and the separate ViGEmBus installer follow their own language settings. Wii2Xbox's own messages about them follow the app's selected language.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**. Support Spanish and English for Wii2Xbox's own user-facing text in the portable and installed app, including the main window, controller list and output menus, pairing, profiles, IR calibration, notification-area controls, accessibility labels, and status, warning, and error messages.
- **FR-002**. Default to System mode when no valid language preference is saved. Use Spanish for a Windows display language in the Spanish language family and English for all other display languages.
- **FR-003**. Provide an application language control with System, Español, and English options. The Español and English option names must remain recognizable in either display language.
- **FR-004**. Store the chosen mode per Windows account. An explicit choice takes precedence over Windows display language and survives restarting or updating the app. System mode restores automatic selection. Apply changes at the next full launch and explain the immediate effect of closing to the notification area.
- **FR-005**. Use the same selected language for manual launches and background startup at sign-in, including notifications shown before the window is opened.
- **FR-006**. Localize Wii2Xbox's MSI setup wizard, including welcome, progress, completion, cancellation, and its own error messages, in Spanish and English. Select the language from Windows display language using the same regional matching and fallback as System mode. Ship one MSI and one portable ZIP per release.
- **FR-007**. Preserve existing user-created names, controller identities, button mappings, and profile import/export compatibility when changing the app language. Localize the app's labels and messages around that data.
- **FR-008**. Keep language selection available offline. Changing the preference must preserve active controller sessions and their outputs until the user explicitly exits.

### Key Entities

- **Language preference**. Saved per Windows account as System, Spanish, or English. The chosen mode determines the app language on the next full launch.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**. On supported Windows versions, each distribution passes the default-language matrix for Spanish, English, and an unsupported display language, and the Spanish and English cases each cover two regional variants. The pairing dialog and notification-area menu agree with the main window in every case.
- **SC-002**. Complete controller discovery, profile editing, output selection, IR calibration, and notification-area operation in both languages. Every Wii2Xbox-owned label, status, empty state, and error exercised in these flows is readable in the selected language without clipping in the supported window layouts.
- **SC-003**. Switching between System, Español, and English persists across full restarts and an app update. Background startup agrees with manual startup, and no active session is dropped when saving a new language preference.
- **SC-004**. One MSI shows the expected Spanish or English wizard, including the English fallback, across the three display-language cases. Verify completion and cancellation in both supported languages.
- **SC-005**. After changing language and reopening Wii2Xbox, an existing paired controller retains its profile and mappings, and an exported profile can be imported in either language with the same behavior.

## Assumptions

- This specification extends the Spanish UI baseline in [Low-Latency Wii2Xbox](../001-wii-xbox-low-latency/spec.md). It uses the background lifecycle defined in [Background Operation and Windows Startup](../002-background-startup/spec.md) and the distribution formats defined in [Portable ZIP and MSI Distribution](../005-portable-msi-distribution/spec.md).
- The app and MSI pick their language independently. Setup follows Windows display language, while the app can also use the saved per-user preference shared by the two distributions.
- This feature covers Wii2Xbox-owned interface text. Project documentation remains in English under the constitution. Windows components, third-party UI, legal texts, and user-entered content keep their own language.
