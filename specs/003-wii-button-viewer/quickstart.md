# Test the Viewer

1. Publish the app with `scripts/publish.ps1` and open Wii2Xbox.
2. Connect two controllers using **Añadir mando…** (Add controller…). Press A on the first and 2 on the second. Press A on both and check the count.
3. Release or turn off the first controller. Check that the second remains lit. Test all eleven inputs, change the profile, and resize the window.
4. Run `scripts/test.ps1` to verify simulated physical input and the rest of the test suite.
