# Viewer Decisions

- Reuse the HID reader for each session to obtain the decoded Wii buttons. Capture after the regular Xbox report submission to keep painting and aggregation off the input path.
- Retain the press-edge timestamp and held state. This makes short presses visible without storing a report queue.
- Use an original front-facing horizontal illustration with a transparent background. The D-pad follows the physical orientation, with the IR end on the left and a separate callout for B.
- Read controllers without available output through the same session type. This prevents opening the same HID device a second time.

The [WiiRemote.png](../../src/Wii2Xbox/Resources/WiiRemote.png) asset was created with the built-in image generator. The request was for a photorealistic cutout of an idle white Wii Remote from the front in horizontal orientation, with the IR end on the left, recognizable controls and labels, neutral lighting, and a transparent background.

A follow-up requested rotating only the HOME, power, −, 1, and 2 markings 90° counterclockwise while retaining the button positions and transparent canvas.

The final revision also requested rotating the A marking, giving the A button a translucent polycarbonate finish, and removing the first and last speaker rows to show three rows of four holes.
