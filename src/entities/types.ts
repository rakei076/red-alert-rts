import Phaser from 'phaser';

export type Team = 'player' | 'enemy';
export type UnitType = 'rifleman' | 'tank' | 'harvester';
export type BuildingType = 'command' | 'power' | 'refinery' | 'barracks' | 'factory' | 'turret';

export interface UnitStats {
  label: string;
  maxHp: number;
  speed: number;
  damage: number;
  range: number;
  fireMs: number;
  cost: number;
  asset: string;
  selectableRadius: number;
}

export interface BuildingStats {
  label: string;
  maxHp: number;
  cost: number;
  footprint: Phaser.Math.Vector2;
  asset: string;
  produces?: UnitType[];
  damage?: number;
  range?: number;
  fireMs?: number;
}

export interface ResourceNode {
  id: number;
  sprite: Phaser.GameObjects.Image;
  amount: number;
}

export interface UnitEntity {
  id: number;
  kind: UnitType;
  team: Team;
  sprite: Phaser.GameObjects.Image;
  hp: number;
  maxHp: number;
  selected: boolean;
  moveTarget?: Phaser.Math.Vector2;
  attackTarget?: UnitEntity | BuildingEntity;
  harvestTarget?: ResourceNode;
  deliveringTo?: BuildingEntity;
  cargo: number;
  cooldown: number;
  gatherTimer: number;
  hpBar: Phaser.GameObjects.Graphics;
  selectionRing: Phaser.GameObjects.Graphics;
  lastOrderWasAttack: boolean;
}

export interface BuildingEntity {
  id: number;
  kind: BuildingType;
  team: Team;
  sprite: Phaser.GameObjects.Image;
  hp: number;
  maxHp: number;
  selected: boolean;
  cooldown: number;
  rallyPoint?: Phaser.Math.Vector2;
  hpBar: Phaser.GameObjects.Graphics;
  selectionRing: Phaser.GameObjects.Graphics;
}
