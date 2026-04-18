# Red Alert RTS Unity

A Unity-based, Red Alert-inspired single-player RTS prototype.

The project is intentionally engine-first: open it in Unity, press Play, and the current mission scene generates the battlefield, units, buildings, resources, UI, and enemy waves at runtime.

## Requirements

- Unity 2022.3 LTS or newer
- macOS for local testing, with a future Windows build target planned

The repository does not include proprietary Red Alert art, names, audio, or missions.

## Open Locally

1. Install Unity Hub and Unity 2022.3 LTS or newer.
2. In Unity Hub, choose **Add project from disk**.
3. Select this folder: `/Users/rakel/claudecodeworkspace/red-alert-rts`.
4. Open `Assets/Scenes/Main.unity`.
5. Press Play.

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
