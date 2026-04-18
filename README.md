# Red Alert RTS

A browser-playable, Red Alert-inspired single-player RTS prototype built with Phaser and TypeScript.

The project starts as a small vertical slice and is meant to grow through regular GitHub updates. It does not include any proprietary Red Alert art, names, audio, or missions.

## Play Locally

```bash
npm install
npm run dev
```

Open the URL printed by Vite, usually `http://localhost:5173`.

## Current Features

- Single-player campaign menu with 3 playable missions
- RTS camera movement with WASD or arrow keys
- Drag selection for player units
- Right-click move, attack, and harvest commands
- Ore harvesting loop with refinery delivery
- Player building placement near existing base structures
- Train riflemen, tanks, and harvesters
- Build power plants, refineries, barracks, factories, and turrets
- Enemy bases, defensive turrets, patrol combat, and timed attack waves
- Mission complete and mission failed states
- Mini-map with units, structures, ore, and camera viewport
- Original open-source SVG placeholder art

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
| Quick train rifleman | 1 |
| Quick train tank | 2 |
| Quick train harvester | 3 |

## Development

```bash
npm run typecheck
npm run build
npm run preview
```

## Project Direction

The first version favors readable systems over deep optimization. The next milestones are listed in `docs/ROADMAP.md`.

## License

Code is MIT licensed. Current original SVG assets are CC0. See `ASSET_LICENSE.md`.
