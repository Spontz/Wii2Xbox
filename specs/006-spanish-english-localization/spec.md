# Feature Specification: Spanish and English Localization

**Feature Directory**: `specs/006-spanish-english-localization`

**Created**: 2026-11-22

**Status**: Implemented. The 2026-11-23 revision removes the in-app language picker and its restart notice.

## User Scenarios & Testing

### User Story 1 - Open Wii2Xbox in the Windows display language (Priority: P1)

As a player, I want Wii2Xbox to use the Windows display language automatically so I can connect a controller as soon as the window opens.

1. Spanish, including its regional variants, opens the app in Spanish. English and all other display languages open the app in English.
2. Manual and background launches choose the same language. Reopening a hidden window keeps the language of that running instance.
3. Changing the Windows display language takes effect on the next full app launch. The main window has no language picker or language-change notice.

### User Story 2 - Use the same bilingual MSI (Priority: P2)

As a player, I want installation and removal to use a setup wizard in Spanish or English so I can follow the result.

1. The same MSI shows setup in Spanish for Spanish display languages and English otherwise.
2. The installed app and the portable ZIP each use the current Windows display language at launch.

### Edge Cases

- Regional formats can differ from display language. The primary Windows display language drives the selection.
- An old in-app choice has no effect on a new launch. Existing profiles and controller settings remain available.
- System dialogs, third-party visual components, and legal texts follow their own language rules.

## Functional Requirements

- **FR-001**. Support Spanish and English for Wii2Xbox-owned labels, accessibility names, menus, dialogs, warnings, and errors in the portable and installed app.
- **FR-002**. Select Spanish for the Spanish Windows display language family and English for all other display languages.
- **FR-003**. Choose the display language on a full process launch without a language control or reminder in the interface.
- **FR-005**. Manual launch and background startup must use the same app language.
- **FR-006**. Localize Wii2Xbox-owned MSI wizard text in Spanish and English based on the Windows display language. Ship one MSI and one portable ZIP per release.
- **FR-007**. Preserve custom names, controller identities, mappings, and profile import/export compatibility.
- **FR-008**. Resolve the app and MSI languages offline.

## Success Criteria

- **SC-001**. Both distributions use the correct language for Spanish regional variants, English variants, and an unsupported Windows display language.
- **SC-002**. Main, pairing, profiles, IR calibration, and notification-area controls display readable localized text. At enlarged text sizes the main and pairing windows keep their rows separate and make overflow reachable.
- **SC-003**. Manual and background launch agree. Neither shows a language selection prompt or restart notice.
- **SC-004**. One MSI shows Spanish and English wizard text, including English fallback for unsupported display languages.
- **SC-005**. Profile import and export preserve existing names and mappings under both app languages.

## Assumptions

- This feature extends the [low-latency app](../001-wii-xbox-low-latency/spec.md) and [dual distribution](../005-portable-msi-distribution/spec.md). Project documentation remains in English.
