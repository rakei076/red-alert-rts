# Red Alert RTS Unity

A Unity-based, Red Alert-inspired single-player RTS prototype.

The project is intentionally engine-first: open it in Unity, press Play, and the current mission scene generates the battlefield, units, buildings, resources, UI, and enemy waves at runtime.

Repository: <https://github.com/rakei076/red-alert-rts>

## Requirements

- Unity 2022.3 LTS or newer
- Unity Hub
- 30 GB free disk space recommended, 50 GB is more comfortable
- 16 GB RAM recommended
- macOS or Windows for local testing

The repository does not include proprietary Red Alert art, names, audio, or missions.

## Quick Start From GitHub

### Option A: Download ZIP

1. Open <https://github.com/rakei076/red-alert-rts>.
2. Click **Code**.
3. Click **Download ZIP**.
4. Unzip the file.
5. Open Unity Hub.
6. Click **Add** or **Add project from disk**.
7. Select the unzipped `red-alert-rts` folder.
8. Unity may ask which Editor version to use. Choose **Unity 2022.3 LTS or newer**.
9. Wait for Unity to import the project. The first import can take several minutes.
10. Open `Assets/Scenes/Main.unity`.
11. Press the Play button at the top of the Unity editor.

### Option B: Git Clone

```bash
git clone https://github.com/rakei076/red-alert-rts.git
```

Then:

1. Install Unity Hub and Unity 2022.3 LTS or newer.
2. In Unity Hub, choose **Add project from disk**.
3. Select the cloned `red-alert-rts` folder.
4. Open `Assets/Scenes/Main.unity`.
5. Press Play.

## What Should Happen

After pressing Play:

- A campaign menu appears.
- Click **Start Mission**.
- A top-down RTS battlefield is generated.
- You can select units, harvest ore, build structures, train units, and attack the enemy base.

## If It Does Not Start

- **Unity says the project was created with another version:** open it with Unity 2022.3 LTS or newer.
- **Scene is empty:** open `Assets/Scenes/Main.unity`, then press Play again.
- **Script compile error appears:** copy the first error from Unity Console and open an issue on GitHub.
- **Unity spends a long time importing:** wait. The first import creates a local `Library` folder that is not included in Git.
- **Download ZIP looks too small:** that is normal. Unity itself is large; this project is currently lightweight.

## Build A Windows EXE Later

1. In Unity Hub, install **Windows Build Support** for your Unity version.
2. Open the project.
3. In Unity, choose **File > Build Settings**.
4. Select **Windows, Mac, Linux**.
5. Choose **Windows** as the target platform.
6. Click **Switch Platform** if needed.
7. Click **Build**.

## Current Features

- Unity project layout with `Assets`, `Packages`, and `ProjectSettings`
- Runtime-generated campaign mission
- RTS camera movement with WASD or arrow keys
- Drag selection for player units
- Right-click move, attack, and harvest commands
- Ore harvesting loop with refinery delivery
- Player building placement near existing base structures
- Train riflemen, tanks, and harvesters
- Build power plants, refineries, barracks, factories, and turrets
- Enemy base, defensive turret, autonomous combat, and timed attack waves
- Mission complete and mission failed states
- Mini-map with units, buildings, ore, and camera viewport
- Original CC0 procedural placeholder art

## Controls

| Action | Control |
| --- | --- |
| Move camera | WASD or arrow keys |
| Select unit or building | Left click |
| Box select units | Drag with left mouse |
| Move selected units | Right click ground |
| Attack enemy | Right click enemy |
| Harvest ore | Select harvester, right click ore |
| Set building rally point | Select building, right click ground |
| Cancel building placement | Esc |
| Select harvester | H |
| Jump to command center | Space |

## Roadmap

See `docs/ROADMAP.md`.

## License

Code is MIT licensed. Current procedural placeholder visuals are CC0. See `ASSET_LICENSE.md`.
