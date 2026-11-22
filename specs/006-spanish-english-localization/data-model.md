# Language Preference

| Stored value | Meaning |
| --- | --- |
| Missing or unrecognized | Follow the Windows display language |
| `system` | Follow the Windows display language |
| `es` | Always display Spanish |
| `en` | Always display English |

The preference belongs to the Windows account. The app resolves it at launch and keeps that language until it exits. Saving a different preference leaves the current instance and controller sessions running. Setup derives its own language from Windows and does not modify the app preference.
