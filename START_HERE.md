# Start Here

If the Unity scene looks empty, do this first:

1. Open `Assets/Scenes/Main.unity`.
2. Look at the **Hierarchy** panel.
3. If you see `GameBootstrapper`, the scene is loaded correctly.
4. Press the Play button at the top of Unity.
5. Switch to the **Game** tab.

The editor Scene view can look empty before Play. That is normal for this prototype because the battlefield is generated at runtime.

After pressing Play, you should see:

- `RED ALERT RTS UNITY`
- a `Start Mission` button
- mission text

If the Game tab is still blank:

1. Open **Window > General > Console**.
2. Look for the first red error.
3. Copy that error into a GitHub issue here:
   <https://github.com/rakei076/red-alert-rts/issues>

The most likely next fix will be based on that first Console error.
