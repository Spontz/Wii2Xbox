# Implementation Plan: Spanish and English Localization

**Created**: 2026-11-22

**Updated**: 2026-11-23

**Specification**: [spec.md](spec.md)

## Technical Context

Wii2Xbox uses WinForms on .NET 10 and one per-user WiX MSI for Windows x64. Both distributions use the same app binaries and language resources.

## Constitution Check

Resolve the UI language once at startup and keep localized lookups outside the controller report path. Build the portable ZIP and MSI from the same build. Record Windows matrix acceptance separately from local tests.

## Design

1. Resolve the primary Windows display language and use English for any non-Spanish value.
2. Use named English and Spanish resources. Keep profile names, mapping keys, and identities independent of the display language.
3. Use separate resource sets for the setup wizard within one MSI. Leave controller sessions and application settings untouched by localization.
4. Size desktop forms for per-monitor DPI, keep a minimum window size, and allow scroll overflow instead of collapsing rows.
5. Verify the UI and packages using the [quickstart](quickstart.md).
