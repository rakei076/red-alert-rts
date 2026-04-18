import Phaser from 'phaser';

const ASSETS = [
  ['unit-rifleman', 'assets/unit-rifleman.svg'],
  ['unit-tank', 'assets/unit-tank.svg'],
  ['unit-harvester', 'assets/unit-harvester.svg'],
  ['building-command', 'assets/building-command.svg'],
  ['building-power', 'assets/building-power.svg'],
  ['building-refinery', 'assets/building-refinery.svg'],
  ['building-barracks', 'assets/building-barracks.svg'],
  ['building-factory', 'assets/building-factory.svg'],
  ['building-turret', 'assets/building-turret.svg'],
  ['ore', 'assets/ore.svg'],
  ['cursor-move', 'assets/cursor-move.svg']
] as const;

export class BootScene extends Phaser.Scene {
  constructor() {
    super('BootScene');
  }

  preload(): void {
    this.load.setPath('/');
    for (const [key, path] of ASSETS) {
      this.load.svg(key, path, { width: 96, height: 96 });
    }
  }

  create(): void {
    this.scene.start('MenuScene');
  }
}
