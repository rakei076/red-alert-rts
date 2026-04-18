import { BuildingStats, UnitStats } from '../entities/types';

export const UNIT_STATS: Record<string, UnitStats> = {
  rifleman: {
    label: 'Rifleman',
    maxHp: 70,
    speed: 95,
    damage: 9,
    range: 145,
    fireMs: 650,
    cost: 100,
    asset: 'unit-rifleman',
    selectableRadius: 18
  },
  tank: {
    label: 'Light Tank',
    maxHp: 185,
    speed: 72,
    damage: 24,
    range: 185,
    fireMs: 950,
    cost: 250,
    asset: 'unit-tank',
    selectableRadius: 24
  },
  harvester: {
    label: 'Harvester',
    maxHp: 165,
    speed: 62,
    damage: 0,
    range: 0,
    fireMs: 0,
    cost: 300,
    asset: 'unit-harvester',
    selectableRadius: 25
  }
};

export const BUILDING_STATS: Record<string, BuildingStats> = {
  command: {
    label: 'Command Center',
    maxHp: 900,
    cost: 0,
    footprint: { x: 112, y: 96 } as Phaser.Math.Vector2,
    asset: 'building-command'
  },
  power: {
    label: 'Power Plant',
    maxHp: 380,
    cost: 200,
    footprint: { x: 88, y: 80 } as Phaser.Math.Vector2,
    asset: 'building-power'
  },
  refinery: {
    label: 'Ore Refinery',
    maxHp: 620,
    cost: 400,
    footprint: { x: 112, y: 88 } as Phaser.Math.Vector2,
    asset: 'building-refinery'
  },
  barracks: {
    label: 'Barracks',
    maxHp: 420,
    cost: 300,
    footprint: { x: 92, y: 76 } as Phaser.Math.Vector2,
    asset: 'building-barracks',
    produces: ['rifleman']
  },
  factory: {
    label: 'War Factory',
    maxHp: 580,
    cost: 500,
    footprint: { x: 122, y: 92 } as Phaser.Math.Vector2,
    asset: 'building-factory',
    produces: ['tank', 'harvester']
  },
  turret: {
    label: 'Gun Turret',
    maxHp: 300,
    cost: 350,
    footprint: { x: 58, y: 58 } as Phaser.Math.Vector2,
    asset: 'building-turret',
    damage: 18,
    range: 205,
    fireMs: 760
  }
};
