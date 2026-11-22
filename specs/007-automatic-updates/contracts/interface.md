# Update Interface

- Poll at startup and at most once per six hours under normal conditions. Re-evaluate a ready update independently of the polling interval.
- Show the tray action only while work is ready. Automatic application requires a safe idle point.
- Never change from portable ZIP to installed MSI or vice versa. Windows startup registration continues to reference the same app path.
- Close the current process before installation and relaunch the correct app with the previous visible or resident mode.
