import { BuildingType, UnitType } from '../entities/types';

export interface CampaignWave {
  at: number;
  units: UnitType[];
  message: string;
}

export interface CampaignLevel {
  id: string;
  title: string;
  briefing: string;
  map: {
    width: number;
    height: number;
  };
  startingResources: number;
  player: {
    units: Array<{ kind: UnitType; x: number; y: number }>;
    buildings: Array<{ kind: BuildingType; x: number; y: number }>;
  };
  enemy: {
    units: Array<{ kind: UnitType; x: number; y: number }>;
    buildings: Array<{ kind: BuildingType; x: number; y: number }>;
    waves: CampaignWave[];
  };
  ore: Array<{ x: number; y: number; amount: number }>;
  objective: string;
}

export const CAMPAIGN_LEVELS: CampaignLevel[] = [
  {
    id: 'm01',
    title: 'Mission 01: First Refinery',
    briefing: 'Establish the beachhead. Harvest ore, train a strike team, and destroy the hostile command center.',
    objective: 'Destroy the enemy Command Center.',
    map: { width: 40, height: 28 },
    startingResources: 850,
    player: {
      buildings: [
        { kind: 'command', x: 360, y: 430 },
        { kind: 'refinery', x: 510, y: 470 },
        { kind: 'barracks', x: 355, y: 560 }
      ],
      units: [
        { kind: 'harvester', x: 610, y: 510 },
        { kind: 'rifleman', x: 280, y: 360 },
        { kind: 'rifleman', x: 320, y: 350 },
        { kind: 'tank', x: 430, y: 350 }
      ]
    },
    enemy: {
      buildings: [
        { kind: 'command', x: 1530, y: 610 },
        { kind: 'barracks', x: 1390, y: 720 },
        { kind: 'turret', x: 1295, y: 610 }
      ],
      units: [
        { kind: 'rifleman', x: 1290, y: 705 },
        { kind: 'rifleman', x: 1330, y: 745 },
        { kind: 'tank', x: 1440, y: 550 }
      ],
      waves: [
        { at: 70, units: ['rifleman', 'rifleman', 'tank'], message: 'Enemy scouts are moving on your base.' },
        { at: 150, units: ['tank', 'tank', 'rifleman'], message: 'Enemy armor wave detected.' }
      ]
    },
    ore: [
      { x: 770, y: 455, amount: 1600 },
      { x: 830, y: 500, amount: 1400 },
      { x: 900, y: 435, amount: 1300 },
      { x: 1190, y: 850, amount: 1900 },
      { x: 1260, y: 905, amount: 1500 }
    ]
  },
  {
    id: 'm02',
    title: 'Mission 02: Armor Column',
    briefing: 'Enemy armor controls the central pass. Build a factory, hold the line, and push through before they mass up.',
    objective: 'Destroy every enemy production building.',
    map: { width: 44, height: 30 },
    startingResources: 1200,
    player: {
      buildings: [
        { kind: 'command', x: 340, y: 450 },
        { kind: 'refinery', x: 500, y: 505 },
        { kind: 'barracks', x: 335, y: 590 },
        { kind: 'power', x: 205, y: 515 }
      ],
      units: [
        { kind: 'harvester', x: 600, y: 535 },
        { kind: 'tank', x: 420, y: 350 },
        { kind: 'tank', x: 475, y: 370 },
        { kind: 'rifleman', x: 305, y: 350 },
        { kind: 'rifleman', x: 335, y: 335 }
      ]
    },
    enemy: {
      buildings: [
        { kind: 'command', x: 1650, y: 890 },
        { kind: 'factory', x: 1485, y: 760 },
        { kind: 'barracks', x: 1580, y: 1040 },
        { kind: 'turret', x: 1335, y: 760 },
        { kind: 'turret', x: 1415, y: 970 }
      ],
      units: [
        { kind: 'tank', x: 1270, y: 820 },
        { kind: 'tank', x: 1320, y: 860 },
        { kind: 'rifleman', x: 1390, y: 1010 },
        { kind: 'rifleman', x: 1420, y: 1035 }
      ],
      waves: [
        { at: 55, units: ['tank', 'rifleman', 'rifleman'], message: 'A probing attack is coming through the pass.' },
        { at: 120, units: ['tank', 'tank', 'tank'], message: 'Enemy factory output is accelerating.' },
        { at: 210, units: ['tank', 'tank', 'rifleman', 'rifleman'], message: 'Last known armor reserve is on the move.' }
      ]
    },
    ore: [
      { x: 730, y: 540, amount: 1800 },
      { x: 780, y: 605, amount: 1600 },
      { x: 885, y: 520, amount: 1500 },
      { x: 1000, y: 900, amount: 2100 },
      { x: 1100, y: 970, amount: 1800 },
      { x: 1190, y: 910, amount: 1300 }
    ]
  },
  {
    id: 'm03',
    title: 'Mission 03: Red Dawn',
    briefing: 'A fortified base is preparing a full assault. Expand, defend against waves, then crush the command network.',
    objective: 'Destroy the enemy Command Center.',
    map: { width: 48, height: 32 },
    startingResources: 1450,
    player: {
      buildings: [
        { kind: 'command', x: 360, y: 470 },
        { kind: 'refinery', x: 520, y: 540 },
        { kind: 'barracks', x: 315, y: 620 },
        { kind: 'factory', x: 505, y: 690 },
        { kind: 'power', x: 205, y: 530 }
      ],
      units: [
        { kind: 'harvester', x: 640, y: 565 },
        { kind: 'tank', x: 420, y: 360 },
        { kind: 'tank', x: 470, y: 380 },
        { kind: 'rifleman', x: 310, y: 350 },
        { kind: 'rifleman', x: 340, y: 335 },
        { kind: 'rifleman', x: 365, y: 365 }
      ]
    },
    enemy: {
      buildings: [
        { kind: 'command', x: 1880, y: 980 },
        { kind: 'factory', x: 1680, y: 850 },
        { kind: 'barracks', x: 1750, y: 1110 },
        { kind: 'refinery', x: 1975, y: 820 },
        { kind: 'turret', x: 1520, y: 900 },
        { kind: 'turret', x: 1645, y: 1030 },
        { kind: 'turret', x: 1810, y: 730 }
      ],
      units: [
        { kind: 'tank', x: 1510, y: 780 },
        { kind: 'tank', x: 1590, y: 815 },
        { kind: 'tank', x: 1700, y: 955 },
        { kind: 'rifleman', x: 1605, y: 1095 },
        { kind: 'rifleman', x: 1645, y: 1125 },
        { kind: 'rifleman', x: 1720, y: 760 }
      ],
      waves: [
        { at: 45, units: ['rifleman', 'rifleman', 'tank'], message: 'Enemy vanguard closing in.' },
        { at: 100, units: ['tank', 'tank', 'rifleman'], message: 'Second assault wave confirmed.' },
        { at: 165, units: ['tank', 'tank', 'tank', 'rifleman'], message: 'Heavy armor has left the enemy base.' },
        { at: 245, units: ['tank', 'tank', 'tank', 'tank'], message: 'Final enemy counterattack inbound.' }
      ]
    },
    ore: [
      { x: 760, y: 570, amount: 2200 },
      { x: 820, y: 635, amount: 1900 },
      { x: 920, y: 545, amount: 1700 },
      { x: 1050, y: 910, amount: 2400 },
      { x: 1120, y: 990, amount: 2100 },
      { x: 1255, y: 930, amount: 1900 },
      { x: 1970, y: 700, amount: 1800 },
      { x: 2070, y: 760, amount: 1600 }
    ]
  }
];
