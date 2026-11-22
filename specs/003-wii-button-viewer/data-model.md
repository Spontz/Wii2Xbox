# Viewer Data

- **Physical session buttons**. A mask of held Wii buttons and the monotonic timestamp of the last press of each button. Valid reports update the mask.
- **Visible input**. A button held or pressed within the last 120 ms. Each active session contributes at most once per button.
- **Combined view**. A count for each button in the illustration. When a session is removed, its state stops contributing.
