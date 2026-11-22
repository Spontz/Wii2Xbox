# Update Decisions

- Use the list of [GitHub releases](https://docs.github.com/en/rest/releases/releases#list-releases). This repository publishes separate installer and portable tags, so the latest-release endpoint alone cannot select both formats.
- Compare numeric stable three-part versions. Verify the asset digest documented by the [GitHub release assets API](https://docs.github.com/en/rest/releases/assets).
- Use per-user Windows Installer with [quiet and no-restart flags](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/msiexec). Handle Windows Installer's restart return code without requesting a reboot.
- Use an external helper for file replacement. Preserve user files in a portable folder and coordinate app relaunch with a separate mutex.
