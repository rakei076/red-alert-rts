import Phaser from 'phaser';
import { CAMPAIGN_LEVELS, CampaignLevel } from '../data/campaign';
import { BUILDING_STATS, UNIT_STATS } from '../data/stats';
import { BuildingEntity, BuildingType, ResourceNode, Team, UnitEntity, UnitType } from '../entities/types';

interface UiButton {
  rect: Phaser.GameObjects.Rectangle;
  title: Phaser.GameObjects.Text;
  detail: Phaser.GameObjects.Text;
  canUse: () => boolean;
}

interface SceneInit {
  levelIndex?: number;
}

const TILE = 48;
const UI_HEIGHT = 116;
const ORE_CARGO = 120;
const PLAYER_COLOR = 0x7abf75;
const ENEMY_COLOR = 0xc84f4a;
const NEUTRAL_COLOR = 0xc9b458;

export class GameScene extends Phaser.Scene {
  private levelIndex = 0;
  private level!: CampaignLevel;
  private resources = 0;
  private gameSeconds = 0;
  private entityId = 1;
  private resourceId = 1;
  private units: UnitEntity[] = [];
  private buildings: BuildingEntity[] = [];
  private resourcesNodes: ResourceNode[] = [];
  private selectedUnits = new Set<UnitEntity>();
  private selectedBuilding?: BuildingEntity;
  private cursors?: Phaser.Types.Input.Keyboard.CursorKeys;
  private keys!: Record<string, Phaser.Input.Keyboard.Key>;
  private selectionGraphics!: Phaser.GameObjects.Graphics;
  private effectsGraphics!: Phaser.GameObjects.Graphics;
  private gridGraphics!: Phaser.GameObjects.Graphics;
  private minimapGraphics!: Phaser.GameObjects.Graphics;
  private dragStart?: Phaser.Math.Vector2;
  private isDragging = false;
  private pendingBuilding?: BuildingType;
  private placementGhost?: Phaser.GameObjects.Image;
  private resourceText!: Phaser.GameObjects.Text;
  private objectiveText!: Phaser.GameObjects.Text;
  private timerText!: Phaser.GameObjects.Text;
  private hintText!: Phaser.GameObjects.Text;
  private messageText!: Phaser.GameObjects.Text;
  private buttons: UiButton[] = [];
  private waveIndex = 0;
  private ended = false;
  private updateMinimapAt = 0;

  constructor() {
    super('GameScene');
  }

  init(data: SceneInit): void {
    this.levelIndex = data.levelIndex ?? 0;
    this.level = CAMPAIGN_LEVELS[this.levelIndex] ?? CAMPAIGN_LEVELS[0];
  }

  create(): void {
    this.resetState();
    this.input.mouse?.disableContextMenu();
    this.cameras.main.setBounds(0, 0, this.level.map.width * TILE, this.level.map.height * TILE);
    this.cameras.main.setZoom(1);
    this.cameras.main.centerOn(520, 480);
    this.cursors = this.input.keyboard?.createCursorKeys();
    this.keys = this.input.keyboard?.addKeys('W,A,S,D,ESC,Q,E,R,T,F,H,ONE,TWO,THREE,FOUR,FIVE,SIX,SPACE') as Record<string, Phaser.Input.Keyboard.Key>;

    this.drawTerrain();
    this.createWorld();
    this.createUi();
    this.bindInput();
    this.time.addEvent({
      delay: 1000,
      loop: true,
      callback: () => {
        if (!this.ended) {
          this.gameSeconds += 1;
          this.checkWaves();
        }
      }
    });
    this.showMessage(this.level.briefing, 5600);
  }

  update(_time: number, deltaMs: number): void {
    if (this.ended) return;
    const delta = deltaMs / 1000;
    this.updateCamera(delta);
    this.updatePlacementGhost();
    this.updateUnits(deltaMs, delta);
    this.updateBuildings(deltaMs);
    this.cleanupDeadEntities();
    this.updateUi();
    this.checkVictory();
    if (this.time.now > this.updateMinimapAt) {
      this.updateMinimapAt = this.time.now + 200;
      this.drawMinimap();
    }
  }

  private resetState(): void {
    this.resources = this.level.startingResources;
    this.gameSeconds = 0;
    this.entityId = 1;
    this.resourceId = 1;
    this.units = [];
    this.buildings = [];
    this.resourcesNodes = [];
    this.selectedUnits.clear();
    this.selectedBuilding = undefined;
    this.pendingBuilding = undefined;
    this.waveIndex = 0;
    this.ended = false;
    this.buttons = [];
  }

  private drawTerrain(): void {
    const width = this.level.map.width * TILE;
    const height = this.level.map.height * TILE;
    this.add.rectangle(width / 2, height / 2, width, height, 0x172019);

    this.gridGraphics = this.add.graphics();
    this.gridGraphics.lineStyle(1, 0x27332b, 0.55);
    for (let x = 0; x <= width; x += TILE) {
      this.gridGraphics.lineBetween(x, 0, x, height);
    }
    for (let y = 0; y <= height; y += TILE) {
      this.gridGraphics.lineBetween(0, y, width, y);
    }

    const patches = [
      [420, 720, 360, 130],
      [1180, 450, 460, 150],
      [1560, 920, 620, 170],
      [870, 1060, 500, 135]
    ];
    for (const [x, y, w, h] of patches) {
      this.add.ellipse(x, y, w, h, 0x233527, 0.55).setAngle(Phaser.Math.Between(-10, 10));
    }

    this.selectionGraphics = this.add.graphics().setDepth(8000);
    this.effectsGraphics = this.add.graphics().setDepth(8050);
  }

  private createWorld(): void {
    for (const ore of this.level.ore) {
      this.createResourceNode(ore.x, ore.y, ore.amount);
    }
    for (const building of this.level.player.buildings) {
      this.createBuilding(building.kind, 'player', building.x, building.y);
    }
    for (const building of this.level.enemy.buildings) {
      this.createBuilding(building.kind, 'enemy', building.x, building.y);
    }
    for (const unit of this.level.player.units) {
      this.createUnit(unit.kind, 'player', unit.x, unit.y);
    }
    for (const unit of this.level.enemy.units) {
      this.createUnit(unit.kind, 'enemy', unit.x, unit.y);
    }
    this.autoAssignHarvesters();
  }

  private createResourceNode(x: number, y: number, amount: number): ResourceNode {
    const sprite = this.add.image(x, y, 'ore').setDepth(y).setScale(0.74 + Math.min(amount, 2000) / 7000);
    sprite.setTint(0xd1bb4d);
    const node: ResourceNode = { id: this.resourceId++, sprite, amount };
    this.resourcesNodes.push(node);
    return node;
  }

  private createUnit(kind: UnitType, team: Team, x: number, y: number): UnitEntity {
    const stats = UNIT_STATS[kind];
    const sprite = this.add.image(x, y, stats.asset).setDepth(y + 6);
    sprite.setScale(kind === 'rifleman' ? 0.46 : 0.64);
    sprite.setTint(team === 'player' ? PLAYER_COLOR : ENEMY_COLOR);
    sprite.setData('entityType', 'unit');
    sprite.setData('id', this.entityId);
    const unit: UnitEntity = {
      id: this.entityId++,
      kind,
      team,
      sprite,
      hp: stats.maxHp,
      maxHp: stats.maxHp,
      selected: false,
      cargo: 0,
      cooldown: 0,
      gatherTimer: 0,
      hpBar: this.add.graphics().setDepth(8100),
      selectionRing: this.add.graphics().setDepth(7990),
      lastOrderWasAttack: false
    };
    this.units.push(unit);
    this.drawEntityDecorations(unit);
    return unit;
  }

  private createBuilding(kind: BuildingType, team: Team, x: number, y: number): BuildingEntity {
    const stats = BUILDING_STATS[kind];
    const sprite = this.add.image(x, y, stats.asset).setDepth(y);
    sprite.setTint(team === 'player' ? PLAYER_COLOR : ENEMY_COLOR);
    sprite.setDisplaySize(stats.footprint.x, stats.footprint.y);
    sprite.setData('entityType', 'building');
    sprite.setData('id', this.entityId);
    const building: BuildingEntity = {
      id: this.entityId++,
      kind,
      team,
      sprite,
      hp: stats.maxHp,
      maxHp: stats.maxHp,
      selected: false,
      cooldown: 0,
      hpBar: this.add.graphics().setDepth(8100),
      selectionRing: this.add.graphics().setDepth(7980)
    };
    this.buildings.push(building);
    this.drawEntityDecorations(building);
    return building;
  }

  private createUi(): void {
    const { width, height } = this.scale;
    this.add.rectangle(width / 2, 28, width, 56, 0x121915, 0.92).setScrollFactor(0).setDepth(9000);
    this.add.rectangle(width / 2, height - UI_HEIGHT / 2, width, UI_HEIGHT, 0x141b17, 0.96).setScrollFactor(0).setDepth(9000);
    this.add.rectangle(width / 2, height - UI_HEIGHT, width, 2, 0x566450).setScrollFactor(0).setDepth(9001);

    this.resourceText = this.add.text(22, 15, '', { color: '#f1f5df', fontSize: '18px', fontStyle: '700' }).setScrollFactor(0).setDepth(9002);
    this.objectiveText = this.add.text(220, 15, '', { color: '#c6d3bf', fontSize: '16px' }).setScrollFactor(0).setDepth(9002);
    this.timerText = this.add.text(width - 150, 15, '', { color: '#f1f5df', fontSize: '16px', fontStyle: '700' }).setScrollFactor(0).setDepth(9002);
    this.hintText = this.add.text(22, height - 29, 'Drag select | Right-click move/attack/harvest | WASD camera | Esc cancel build', {
      color: '#b9c6b1',
      fontSize: '14px'
    }).setScrollFactor(0).setDepth(9002);
    this.messageText = this.add.text(width / 2, 74, '', {
      color: '#fff8ee',
      fontSize: '18px',
      fontStyle: '700',
      align: 'center',
      backgroundColor: '#283226'
    }).setOrigin(0.5, 0).setPadding(12, 8, 12, 8).setScrollFactor(0).setDepth(9100).setVisible(false);

    const y = height - 96;
    let x = 22;
    this.addButton(x, y, 'Rifleman', '$100', () => this.canTrain('rifleman'), () => this.trainUnit('rifleman'));
    x += 120;
    this.addButton(x, y, 'Tank', '$250', () => this.canTrain('tank'), () => this.trainUnit('tank'));
    x += 120;
    this.addButton(x, y, 'Harvester', '$300', () => this.canTrain('harvester'), () => this.trainUnit('harvester'));
    x += 138;
    this.addButton(x, y, 'Power', '$200', () => this.canStartBuilding('power'), () => this.beginBuildingPlacement('power'));
    x += 112;
    this.addButton(x, y, 'Refinery', '$400', () => this.canStartBuilding('refinery'), () => this.beginBuildingPlacement('refinery'));
    x += 118;
    this.addButton(x, y, 'Barracks', '$300', () => this.canStartBuilding('barracks'), () => this.beginBuildingPlacement('barracks'));
    x += 122;
    this.addButton(x, y, 'Factory', '$500', () => this.canStartBuilding('factory'), () => this.beginBuildingPlacement('factory'));
    x += 118;
    this.addButton(x, y, 'Turret', '$350', () => this.canStartBuilding('turret'), () => this.beginBuildingPlacement('turret'));

    this.minimapGraphics = this.add.graphics().setScrollFactor(0).setDepth(9100);
    this.drawMinimap();
  }

  private addButton(x: number, y: number, label: string, detail: string, canUse: () => boolean, action: () => void): void {
    const rect = this.add.rectangle(x, y, 104, 56, 0x263025).setOrigin(0, 0).setScrollFactor(0).setDepth(9002);
    rect.setStrokeStyle(2, 0x6c7e67);
    const title = this.add.text(x + 10, y + 9, label, { color: '#f1f5df', fontSize: '14px', fontStyle: '700' }).setScrollFactor(0).setDepth(9003);
    const sub = this.add.text(x + 10, y + 31, detail, { color: '#c1cdb6', fontSize: '13px' }).setScrollFactor(0).setDepth(9003);
    rect.setInteractive({ useHandCursor: true });
    rect.on('pointerup', () => {
      if (!this.ended && canUse()) action();
    });
    title.setInteractive({ useHandCursor: true });
    title.on('pointerup', () => {
      if (!this.ended && canUse()) action();
    });
    sub.setInteractive({ useHandCursor: true });
    sub.on('pointerup', () => {
      if (!this.ended && canUse()) action();
    });
    this.buttons.push({ rect, title, detail: sub, canUse });
  }

  private bindInput(): void {
    this.input.on('pointerdown', (pointer: Phaser.Input.Pointer) => {
      if (this.ended) return;
      if (pointer.rightButtonDown()) {
        this.handleRightClick(pointer);
        return;
      }
      if (pointer.y >= this.scale.height - UI_HEIGHT || pointer.y <= 56) return;
      if (this.pendingBuilding) {
        this.tryPlacePendingBuilding(pointer.worldX, pointer.worldY);
        return;
      }
      this.dragStart = new Phaser.Math.Vector2(pointer.worldX, pointer.worldY);
      this.isDragging = false;
    });

    this.input.on('pointermove', (pointer: Phaser.Input.Pointer) => {
      if (!this.dragStart || pointer.rightButtonDown()) return;
      const dist = Phaser.Math.Distance.Between(this.dragStart.x, this.dragStart.y, pointer.worldX, pointer.worldY);
      if (dist > 8) {
        this.isDragging = true;
        this.drawSelectionBox(this.dragStart.x, this.dragStart.y, pointer.worldX, pointer.worldY);
      }
    });

    this.input.on('pointerup', (pointer: Phaser.Input.Pointer) => {
      if (this.ended || pointer.button === 2) return;
      if (!this.dragStart) return;
      if (this.isDragging) {
        this.selectUnitsInBox(this.dragStart.x, this.dragStart.y, pointer.worldX, pointer.worldY);
      } else {
        this.selectSingleAt(pointer.worldX, pointer.worldY);
      }
      this.dragStart = undefined;
      this.isDragging = false;
      this.selectionGraphics.clear();
    });

    this.input.keyboard?.on('keydown-ESC', () => this.cancelPlacement());
    this.input.keyboard?.on('keydown-H', () => this.selectIdleHarvester());
    this.input.keyboard?.on('keydown-SPACE', () => {
      const command = this.findPlayerBuilding('command');
      if (command) this.cameras.main.pan(command.sprite.x, command.sprite.y, 260, 'Sine.easeInOut');
    });
    this.input.keyboard?.on('keydown-ONE', () => this.trainUnit('rifleman'));
    this.input.keyboard?.on('keydown-TWO', () => this.trainUnit('tank'));
    this.input.keyboard?.on('keydown-THREE', () => this.trainUnit('harvester'));
  }

  private updateCamera(delta: number): void {
    const camera = this.cameras.main;
    const speed = 650 * delta;
    if (this.cursors?.left.isDown || this.keys.A?.isDown) camera.scrollX -= speed;
    if (this.cursors?.right.isDown || this.keys.D?.isDown) camera.scrollX += speed;
    if (this.cursors?.up.isDown || this.keys.W?.isDown) camera.scrollY -= speed;
    if (this.cursors?.down.isDown || this.keys.S?.isDown) camera.scrollY += speed;
    camera.scrollX = Phaser.Math.Clamp(camera.scrollX, 0, this.level.map.width * TILE - camera.width);
    camera.scrollY = Phaser.Math.Clamp(camera.scrollY, 0, this.level.map.height * TILE - camera.height);
  }

  private drawSelectionBox(x1: number, y1: number, x2: number, y2: number): void {
    const x = Math.min(x1, x2);
    const y = Math.min(y1, y2);
    const w = Math.abs(x1 - x2);
    const h = Math.abs(y1 - y2);
    this.selectionGraphics.clear();
    this.selectionGraphics.fillStyle(0x9cc982, 0.12);
    this.selectionGraphics.fillRect(x, y, w, h);
    this.selectionGraphics.lineStyle(2, 0xb3e38d, 0.8);
    this.selectionGraphics.strokeRect(x, y, w, h);
  }

  private selectUnitsInBox(x1: number, y1: number, x2: number, y2: number): void {
    this.clearSelection();
    const rect = new Phaser.Geom.Rectangle(Math.min(x1, x2), Math.min(y1, y2), Math.abs(x1 - x2), Math.abs(y1 - y2));
    for (const unit of this.units) {
      if (unit.team === 'player' && Phaser.Geom.Rectangle.Contains(rect, unit.sprite.x, unit.sprite.y)) {
        this.setUnitSelected(unit, true);
      }
    }
    this.showMessage(`${this.selectedUnits.size} unit${this.selectedUnits.size === 1 ? '' : 's'} selected.`, 1100);
  }

  private selectSingleAt(x: number, y: number): void {
    this.clearSelection();
    const unit = this.findUnitAt(x, y, 'player');
    if (unit) {
      this.setUnitSelected(unit, true);
      return;
    }
    const building = this.findBuildingAt(x, y, 'player');
    if (building) {
      this.selectedBuilding = building;
      building.selected = true;
      this.drawEntityDecorations(building);
      this.showMessage(BUILDING_STATS[building.kind].label, 1200);
    }
  }

  private clearSelection(): void {
    for (const unit of this.selectedUnits) {
      this.setUnitSelected(unit, false);
    }
    this.selectedUnits.clear();
    if (this.selectedBuilding) {
      this.selectedBuilding.selected = false;
      this.drawEntityDecorations(this.selectedBuilding);
      this.selectedBuilding = undefined;
    }
  }

  private setUnitSelected(unit: UnitEntity, selected: boolean): void {
    unit.selected = selected;
    if (selected) {
      this.selectedUnits.add(unit);
    } else {
      this.selectedUnits.delete(unit);
    }
    this.drawEntityDecorations(unit);
  }

  private handleRightClick(pointer: Phaser.Input.Pointer): void {
    if (this.pendingBuilding) {
      this.cancelPlacement();
      return;
    }
    const x = pointer.worldX;
    const y = pointer.worldY;
    const enemy = this.findEntityAt(x, y, 'enemy');
    const ore = this.findResourceAt(x, y);

    if (this.selectedBuilding && this.selectedUnits.size === 0) {
      this.selectedBuilding.rallyPoint = new Phaser.Math.Vector2(x, y);
      this.showMovePing(x, y);
      this.showMessage('Rally point set.', 1200);
      return;
    }

    if (this.selectedUnits.size === 0) return;
    const units = [...this.selectedUnits].filter((unit) => unit.team === 'player' && unit.hp > 0);
    if (enemy) {
      for (const unit of units) {
        unit.attackTarget = enemy;
        unit.harvestTarget = undefined;
        unit.deliveringTo = undefined;
        unit.lastOrderWasAttack = true;
      }
      this.showMovePing(x, y, 0xd94843);
      return;
    }

    if (ore) {
      for (const unit of units) {
        if (unit.kind === 'harvester') {
          unit.harvestTarget = ore;
          unit.deliveringTo = undefined;
          unit.attackTarget = undefined;
          unit.moveTarget = new Phaser.Math.Vector2(ore.sprite.x, ore.sprite.y);
          unit.lastOrderWasAttack = false;
        }
      }
      this.showMovePing(ore.sprite.x, ore.sprite.y, 0xc9b458);
      return;
    }

    this.issueMoveOrder(units, x, y);
  }

  private issueMoveOrder(units: UnitEntity[], x: number, y: number): void {
    const columns = Math.ceil(Math.sqrt(units.length));
    const spacing = 42;
    units.forEach((unit, index) => {
      const col = index % columns;
      const row = Math.floor(index / columns);
      const ox = (col - (columns - 1) / 2) * spacing;
      const oy = row * spacing;
      unit.moveTarget = new Phaser.Math.Vector2(x + ox, y + oy);
      unit.attackTarget = undefined;
      unit.harvestTarget = undefined;
      unit.deliveringTo = undefined;
      unit.lastOrderWasAttack = false;
    });
    this.showMovePing(x, y);
  }

  private showMovePing(x: number, y: number, color = 0x9fe07f): void {
    const ring = this.add.circle(x, y, 8, color, 0.1).setStrokeStyle(2, color).setDepth(8060);
    this.tweens.add({
      targets: ring,
      radius: 28,
      alpha: 0,
      duration: 420,
      onComplete: () => ring.destroy()
    });
  }

  private updateUnits(deltaMs: number, delta: number): void {
    for (const unit of this.units) {
      if (unit.hp <= 0) continue;
      unit.cooldown = Math.max(0, unit.cooldown - deltaMs);
      unit.gatherTimer = Math.max(0, unit.gatherTimer - deltaMs);
      if (unit.kind === 'harvester') {
        this.updateHarvester(unit, deltaMs);
      } else {
        this.updateCombatUnit(unit);
      }
      this.moveUnit(unit, delta);
      this.drawEntityDecorations(unit);
    }
  }

  private updateCombatUnit(unit: UnitEntity): void {
    const stats = UNIT_STATS[unit.kind];
    if (!unit.attackTarget || unit.attackTarget.hp <= 0) {
      unit.attackTarget = this.findNearestEnemy(unit.sprite.x, unit.sprite.y, unit.team, unit.lastOrderWasAttack ? 260 : 190);
    }
    if (!unit.attackTarget) return;
    const target = unit.attackTarget;
    const dist = Phaser.Math.Distance.Between(unit.sprite.x, unit.sprite.y, target.sprite.x, target.sprite.y);
    if (dist <= stats.range) {
      unit.moveTarget = undefined;
      if (unit.cooldown <= 0) {
        this.dealDamage(target, stats.damage);
        this.drawShot(unit.sprite.x, unit.sprite.y, target.sprite.x, target.sprite.y, unit.kind === 'tank' ? 0xf8d56b : 0xdce6ca);
        unit.cooldown = stats.fireMs;
      }
    } else if (unit.lastOrderWasAttack) {
      unit.moveTarget = new Phaser.Math.Vector2(target.sprite.x, target.sprite.y);
    }
  }

  private updateHarvester(unit: UnitEntity, deltaMs: number): void {
    if (unit.hp <= 0) return;
    if (unit.cargo >= ORE_CARGO) {
      const refinery = this.findNearestBuildingOfType(unit.sprite.x, unit.sprite.y, 'player', 'refinery');
      if (refinery) {
        unit.deliveringTo = refinery;
        unit.moveTarget = new Phaser.Math.Vector2(refinery.sprite.x, refinery.sprite.y);
        const dist = Phaser.Math.Distance.Between(unit.sprite.x, unit.sprite.y, refinery.sprite.x, refinery.sprite.y);
        if (dist < 70) {
          this.resources += unit.cargo;
          unit.cargo = 0;
          unit.deliveringTo = undefined;
          if (unit.harvestTarget && unit.harvestTarget.amount > 0) {
            unit.moveTarget = new Phaser.Math.Vector2(unit.harvestTarget.sprite.x, unit.harvestTarget.sprite.y);
          }
          this.showMessage('Ore delivered.', 900);
        }
      }
      return;
    }
    if (!unit.harvestTarget || unit.harvestTarget.amount <= 0) {
      unit.harvestTarget = this.findNearestResource(unit.sprite.x, unit.sprite.y);
      if (unit.harvestTarget) unit.moveTarget = new Phaser.Math.Vector2(unit.harvestTarget.sprite.x, unit.harvestTarget.sprite.y);
    }
    if (!unit.harvestTarget) return;
    const target = unit.harvestTarget;
    const dist = Phaser.Math.Distance.Between(unit.sprite.x, unit.sprite.y, target.sprite.x, target.sprite.y);
    if (dist > 46) return;
    unit.moveTarget = undefined;
    if (unit.gatherTimer <= 0) {
      const gathered = Math.min(30, target.amount, ORE_CARGO - unit.cargo);
      unit.cargo += gathered;
      target.amount -= gathered;
      unit.gatherTimer = 650;
      target.sprite.setAlpha(0.35 + Math.min(1, target.amount / 1400) * 0.65);
      if (target.amount <= 0) {
        target.sprite.destroy();
      }
      if (unit.cargo >= ORE_CARGO) {
        this.updateHarvester(unit, deltaMs);
      }
    }
  }

  private moveUnit(unit: UnitEntity, delta: number): void {
    if (!unit.moveTarget) return;
    const stats = UNIT_STATS[unit.kind];
    const dist = Phaser.Math.Distance.Between(unit.sprite.x, unit.sprite.y, unit.moveTarget.x, unit.moveTarget.y);
    if (dist < 6) {
      unit.moveTarget = undefined;
      return;
    }
    const angle = Phaser.Math.Angle.Between(unit.sprite.x, unit.sprite.y, unit.moveTarget.x, unit.moveTarget.y);
    const step = Math.min(dist, stats.speed * delta);
    unit.sprite.x += Math.cos(angle) * step;
    unit.sprite.y += Math.sin(angle) * step;
    unit.sprite.rotation = angle + Math.PI / 2;
    unit.sprite.depth = unit.sprite.y + 6;
  }

  private updateBuildings(deltaMs: number): void {
    for (const building of this.buildings) {
      if (building.hp <= 0) continue;
      building.cooldown = Math.max(0, building.cooldown - deltaMs);
      const stats = BUILDING_STATS[building.kind];
      if (stats.damage && stats.range && building.cooldown <= 0) {
        const target = this.findNearestEnemy(building.sprite.x, building.sprite.y, building.team, stats.range);
        if (target) {
          this.dealDamage(target, stats.damage);
          this.drawShot(building.sprite.x, building.sprite.y, target.sprite.x, target.sprite.y, 0xffc557);
          building.cooldown = stats.fireMs ?? 800;
        }
      }
      this.drawEntityDecorations(building);
    }
  }

  private dealDamage(target: UnitEntity | BuildingEntity, amount: number): void {
    target.hp = Math.max(0, target.hp - amount);
    if (target.hp <= 0) {
      this.addExplosion(target.sprite.x, target.sprite.y);
    }
  }

  private drawShot(x1: number, y1: number, x2: number, y2: number, color: number): void {
    const line = this.add.line(0, 0, x1, y1, x2, y2, color, 0.95).setOrigin(0, 0).setDepth(8070);
    line.setLineWidth(2);
    this.tweens.add({ targets: line, alpha: 0, duration: 120, onComplete: () => line.destroy() });
  }

  private addExplosion(x: number, y: number): void {
    const burst = this.add.circle(x, y, 14, 0xf4c261, 0.9).setDepth(8075);
    this.tweens.add({
      targets: burst,
      radius: 46,
      alpha: 0,
      duration: 320,
      ease: 'Sine.easeOut',
      onComplete: () => burst.destroy()
    });
  }

  private cleanupDeadEntities(): void {
    const deadUnits = this.units.filter((unit) => unit.hp <= 0);
    for (const unit of deadUnits) {
      this.selectedUnits.delete(unit);
      unit.sprite.destroy();
      unit.hpBar.destroy();
      unit.selectionRing.destroy();
    }
    this.units = this.units.filter((unit) => unit.hp > 0);

    const deadBuildings = this.buildings.filter((building) => building.hp <= 0);
    for (const building of deadBuildings) {
      if (this.selectedBuilding === building) this.selectedBuilding = undefined;
      building.sprite.destroy();
      building.hpBar.destroy();
      building.selectionRing.destroy();
    }
    this.buildings = this.buildings.filter((building) => building.hp > 0);
    this.resourcesNodes = this.resourcesNodes.filter((node) => node.amount > 0);
  }

  private drawEntityDecorations(entity: UnitEntity | BuildingEntity): void {
    const isUnit = 'kind' in entity && ['rifleman', 'tank', 'harvester'].includes(entity.kind);
    const width = isUnit ? 42 : BUILDING_STATS[entity.kind as BuildingType].footprint.x;
    const yOffset = isUnit ? 34 : BUILDING_STATS[entity.kind as BuildingType].footprint.y / 2 + 14;
    entity.hpBar.clear();
    entity.selectionRing.clear();

    if (entity.selected || entity.hp < entity.maxHp) {
      const x = entity.sprite.x - width / 2;
      const y = entity.sprite.y - yOffset;
      entity.hpBar.fillStyle(0x202620, 0.92);
      entity.hpBar.fillRect(x, y, width, 5);
      entity.hpBar.fillStyle(entity.team === 'player' ? PLAYER_COLOR : ENEMY_COLOR, 1);
      entity.hpBar.fillRect(x, y, width * (entity.hp / entity.maxHp), 5);
    }

    if (entity.selected) {
      const color = entity.team === 'player' ? 0xb3e38d : 0xff8a82;
      entity.selectionRing.lineStyle(2, color, 0.9);
      if (isUnit) {
        const radius = UNIT_STATS[entity.kind as UnitType].selectableRadius + 6;
        entity.selectionRing.strokeEllipse(entity.sprite.x, entity.sprite.y + 8, radius * 2, radius * 1.15);
      } else {
        const footprint = BUILDING_STATS[entity.kind as BuildingType].footprint;
        entity.selectionRing.strokeRect(entity.sprite.x - footprint.x / 2 - 5, entity.sprite.y - footprint.y / 2 - 5, footprint.x + 10, footprint.y + 10);
      }
    }
  }

  private canTrain(kind: UnitType): boolean {
    if (this.resources < UNIT_STATS[kind].cost) return false;
    const producer = kind === 'rifleman' ? 'barracks' : 'factory';
    return this.buildings.some((building) => building.team === 'player' && building.kind === producer && building.hp > 0);
  }

  private trainUnit(kind: UnitType): void {
    if (!this.canTrain(kind)) return;
    const producerKind: BuildingType = kind === 'rifleman' ? 'barracks' : 'factory';
    const producer = this.selectedBuilding?.kind === producerKind ? this.selectedBuilding : this.findPlayerBuilding(producerKind);
    if (!producer) return;
    this.resources -= UNIT_STATS[kind].cost;
    const spawn = this.findSpawnPointNear(producer.sprite.x, producer.sprite.y);
    const unit = this.createUnit(kind, 'player', spawn.x, spawn.y);
    if (producer.rallyPoint) {
      unit.moveTarget = producer.rallyPoint.clone();
    }
    if (kind === 'harvester') this.assignHarvester(unit);
    this.showMessage(`${UNIT_STATS[kind].label} ready.`, 1100);
  }

  private canStartBuilding(kind: BuildingType): boolean {
    if (this.resources < BUILDING_STATS[kind].cost) return false;
    return this.buildings.some((building) => building.team === 'player' && building.kind === 'command' && building.hp > 0);
  }

  private beginBuildingPlacement(kind: BuildingType): void {
    if (!this.canStartBuilding(kind)) return;
    this.cancelPlacement();
    this.pendingBuilding = kind;
    const stats = BUILDING_STATS[kind];
    this.placementGhost = this.add.image(0, 0, stats.asset).setDisplaySize(stats.footprint.x, stats.footprint.y).setAlpha(0.65).setDepth(8120);
    this.showMessage(`Place ${stats.label}.`, 1600);
  }

  private updatePlacementGhost(): void {
    if (!this.pendingBuilding || !this.placementGhost) return;
    const pointer = this.input.activePointer;
    this.placementGhost.setPosition(pointer.worldX, pointer.worldY);
    const canPlace = this.canPlaceBuilding(this.pendingBuilding, pointer.worldX, pointer.worldY);
    this.placementGhost.setTint(canPlace ? 0x9fe07f : 0xdf5a50);
  }

  private tryPlacePendingBuilding(x: number, y: number): void {
    if (!this.pendingBuilding) return;
    const kind = this.pendingBuilding;
    if (!this.canPlaceBuilding(kind, x, y)) {
      this.showMessage('Cannot build there.', 1200);
      return;
    }
    this.resources -= BUILDING_STATS[kind].cost;
    const building = this.createBuilding(kind, 'player', x, y);
    if (kind === 'refinery') {
      const spawn = this.findSpawnPointNear(x, y);
      const harvester = this.createUnit('harvester', 'player', spawn.x, spawn.y);
      this.assignHarvester(harvester);
    }
    this.cancelPlacement();
    this.clearSelection();
    this.selectedBuilding = building;
    building.selected = true;
    this.drawEntityDecorations(building);
    this.showMessage(`${BUILDING_STATS[kind].label} online.`, 1300);
  }

  private cancelPlacement(): void {
    this.pendingBuilding = undefined;
    this.placementGhost?.destroy();
    this.placementGhost = undefined;
  }

  private canPlaceBuilding(kind: BuildingType, x: number, y: number): boolean {
    const stats = BUILDING_STATS[kind];
    const mapWidth = this.level.map.width * TILE;
    const mapHeight = this.level.map.height * TILE;
    if (x < stats.footprint.x / 2 || y < stats.footprint.y / 2 || x > mapWidth - stats.footprint.x / 2 || y > mapHeight - stats.footprint.y / 2) return false;
    const nearFriendly = this.buildings.some((building) => building.team === 'player' && Phaser.Math.Distance.Between(x, y, building.sprite.x, building.sprite.y) < 260);
    if (!nearFriendly) return false;
    for (const building of this.buildings) {
      const footprint = BUILDING_STATS[building.kind].footprint;
      const overlapX = Math.abs(x - building.sprite.x) < (stats.footprint.x + footprint.x) / 2 + 18;
      const overlapY = Math.abs(y - building.sprite.y) < (stats.footprint.y + footprint.y) / 2 + 18;
      if (overlapX && overlapY) return false;
    }
    return true;
  }

  private checkWaves(): void {
    const wave = this.level.enemy.waves[this.waveIndex];
    if (!wave || this.gameSeconds < wave.at) return;
    this.waveIndex += 1;
    this.showMessage(wave.message, 4200);
    const command = this.findEnemyBuilding('command') ?? this.findEnemyBuilding('factory') ?? this.findEnemyBuilding('barracks');
    const playerCommand = this.findPlayerBuilding('command');
    if (!command || !playerCommand) return;
    wave.units.forEach((kind, index) => {
      const unit = this.createUnit(kind, 'enemy', command.sprite.x + 80 + index * 28, command.sprite.y + 90 + index * 18);
      unit.attackTarget = playerCommand;
      unit.lastOrderWasAttack = true;
    });
  }

  private checkVictory(): void {
    const playerCommand = this.findPlayerBuilding('command');
    const enemyCommand = this.findEnemyBuilding('command');
    if (!playerCommand) {
      this.finish(false, 'Your Command Center has been destroyed.');
      return;
    }
    if (this.level.id === 'm02') {
      const enemyProduction = this.buildings.some((building) => building.team === 'enemy' && (building.kind === 'factory' || building.kind === 'barracks' || building.kind === 'command'));
      if (!enemyProduction) {
        this.finish(true, 'Enemy production is offline.');
      }
      return;
    }
    if (!enemyCommand) {
      this.finish(true, 'Enemy Command Center destroyed.');
    }
  }

  private finish(won: boolean, message: string): void {
    if (this.ended) return;
    this.ended = true;
    this.cancelPlacement();
    this.clearSelection();
    const { width, height } = this.scale;
    const panel = this.add.rectangle(width / 2, height / 2, 520, 260, 0x192119, 0.96).setScrollFactor(0).setDepth(9500);
    panel.setStrokeStyle(3, won ? PLAYER_COLOR : ENEMY_COLOR);
    this.add.text(width / 2, height / 2 - 88, won ? 'MISSION COMPLETE' : 'MISSION FAILED', {
      color: won ? '#dff7bf' : '#ffd2cb',
      fontSize: '32px',
      fontStyle: '700'
    }).setOrigin(0.5).setScrollFactor(0).setDepth(9501);
    this.add.text(width / 2, height / 2 - 34, message, {
      color: '#f1f5df',
      fontSize: '17px',
      align: 'center',
      wordWrap: { width: 430 }
    }).setOrigin(0.5).setScrollFactor(0).setDepth(9501);

    const nextIndex = won ? Math.min(this.levelIndex + 1, CAMPAIGN_LEVELS.length - 1) : this.levelIndex;
    const retry = this.addButtonLike(width / 2 - 150, height / 2 + 54, won ? 'Replay' : 'Retry', () => this.scene.start('GameScene', { levelIndex: this.levelIndex }));
    const next = this.addButtonLike(width / 2 + 34, height / 2 + 54, won && this.levelIndex < CAMPAIGN_LEVELS.length - 1 ? 'Next Mission' : 'Campaign', () => {
      if (won && this.levelIndex < CAMPAIGN_LEVELS.length - 1) {
        this.scene.start('GameScene', { levelIndex: nextIndex });
      } else {
        this.scene.start('MenuScene');
      }
    });
    retry.setDepth(9501);
    next.setDepth(9501);
  }

  private addButtonLike(x: number, y: number, label: string, action: () => void): Phaser.GameObjects.Rectangle {
    const rect = this.add.rectangle(x, y, 148, 44, 0xb9423f).setOrigin(0, 0).setScrollFactor(0);
    rect.setStrokeStyle(2, 0xf1c2a4);
    const text = this.add.text(x + 22, y + 12, label, { color: '#fff8ee', fontSize: '15px', fontStyle: '700' }).setScrollFactor(0).setDepth(9502);
    rect.setInteractive({ useHandCursor: true });
    text.setInteractive({ useHandCursor: true });
    rect.on('pointerup', action);
    text.on('pointerup', action);
    return rect;
  }

  private updateUi(): void {
    this.resourceText.setText(`Ore: $${this.resources}`);
    this.objectiveText.setText(`${this.level.title}  |  ${this.level.objective}`);
    const minutes = Math.floor(this.gameSeconds / 60).toString().padStart(2, '0');
    const seconds = Math.floor(this.gameSeconds % 60).toString().padStart(2, '0');
    this.timerText.setText(`${minutes}:${seconds}`);
    for (const button of this.buttons) {
      const usable = button.canUse();
      button.rect.setFillStyle(usable ? 0x263025 : 0x242824, usable ? 1 : 0.68);
      button.rect.setStrokeStyle(2, usable ? 0x6c7e67 : 0x444b43);
      button.title.setColor(usable ? '#f1f5df' : '#7f877a');
      button.detail.setColor(usable ? '#c1cdb6' : '#737b70');
    }
  }

  private drawMinimap(): void {
    if (!this.minimapGraphics) return;
    const { width, height } = this.scale;
    const mapW = this.level.map.width * TILE;
    const mapH = this.level.map.height * TILE;
    const miniW = 172;
    const miniH = 104;
    const x = width - miniW - 22;
    const y = height - miniH - 10;
    const sx = miniW / mapW;
    const sy = miniH / mapH;
    this.minimapGraphics.clear();
    this.minimapGraphics.fillStyle(0x0d120f, 0.96);
    this.minimapGraphics.fillRect(x, y, miniW, miniH);
    this.minimapGraphics.lineStyle(2, 0x64725f, 1);
    this.minimapGraphics.strokeRect(x, y, miniW, miniH);
    for (const node of this.resourcesNodes) {
      this.minimapGraphics.fillStyle(NEUTRAL_COLOR, 0.85);
      this.minimapGraphics.fillRect(x + node.sprite.x * sx - 2, y + node.sprite.y * sy - 2, 4, 4);
    }
    for (const building of this.buildings) {
      this.minimapGraphics.fillStyle(building.team === 'player' ? PLAYER_COLOR : ENEMY_COLOR, 1);
      this.minimapGraphics.fillRect(x + building.sprite.x * sx - 3, y + building.sprite.y * sy - 3, 6, 6);
    }
    for (const unit of this.units) {
      this.minimapGraphics.fillStyle(unit.team === 'player' ? PLAYER_COLOR : ENEMY_COLOR, 1);
      this.minimapGraphics.fillRect(x + unit.sprite.x * sx - 1, y + unit.sprite.y * sy - 1, 3, 3);
    }
    const cam = this.cameras.main;
    this.minimapGraphics.lineStyle(1, 0xf1f5df, 0.9);
    this.minimapGraphics.strokeRect(x + cam.scrollX * sx, y + cam.scrollY * sy, cam.width * sx, cam.height * sy);
  }

  private showMessage(message: string, duration = 2200): void {
    this.messageText?.setText(message).setVisible(true);
    this.time.delayedCall(duration, () => {
      if (this.messageText?.text === message) this.messageText.setVisible(false);
    });
  }

  private findUnitAt(x: number, y: number, team?: Team): UnitEntity | undefined {
    return this.units.find((unit) => (!team || unit.team === team) && Phaser.Math.Distance.Between(x, y, unit.sprite.x, unit.sprite.y) <= UNIT_STATS[unit.kind].selectableRadius + 8);
  }

  private findBuildingAt(x: number, y: number, team?: Team): BuildingEntity | undefined {
    return this.buildings.find((building) => {
      if (team && building.team !== team) return false;
      const footprint = BUILDING_STATS[building.kind].footprint;
      return Math.abs(x - building.sprite.x) <= footprint.x / 2 && Math.abs(y - building.sprite.y) <= footprint.y / 2;
    });
  }

  private findEntityAt(x: number, y: number, team?: Team): UnitEntity | BuildingEntity | undefined {
    return this.findUnitAt(x, y, team) ?? this.findBuildingAt(x, y, team);
  }

  private findResourceAt(x: number, y: number): ResourceNode | undefined {
    return this.resourcesNodes.find((node) => Phaser.Math.Distance.Between(x, y, node.sprite.x, node.sprite.y) < 48);
  }

  private findNearestEnemy(x: number, y: number, team: Team, maxRange: number): UnitEntity | BuildingEntity | undefined {
    let best: UnitEntity | BuildingEntity | undefined;
    let bestDistance = maxRange;
    const enemies: Array<UnitEntity | BuildingEntity> = [...this.units, ...this.buildings].filter((entity) => entity.team !== team && entity.hp > 0);
    for (const enemy of enemies) {
      const dist = Phaser.Math.Distance.Between(x, y, enemy.sprite.x, enemy.sprite.y);
      if (dist < bestDistance) {
        best = enemy;
        bestDistance = dist;
      }
    }
    return best;
  }

  private findNearestResource(x: number, y: number): ResourceNode | undefined {
    let best: ResourceNode | undefined;
    let bestDistance = Number.POSITIVE_INFINITY;
    for (const node of this.resourcesNodes) {
      if (node.amount <= 0) continue;
      const dist = Phaser.Math.Distance.Between(x, y, node.sprite.x, node.sprite.y);
      if (dist < bestDistance) {
        best = node;
        bestDistance = dist;
      }
    }
    return best;
  }

  private findNearestBuildingOfType(x: number, y: number, team: Team, kind: BuildingType): BuildingEntity | undefined {
    let best: BuildingEntity | undefined;
    let bestDistance = Number.POSITIVE_INFINITY;
    for (const building of this.buildings) {
      if (building.team !== team || building.kind !== kind) continue;
      const dist = Phaser.Math.Distance.Between(x, y, building.sprite.x, building.sprite.y);
      if (dist < bestDistance) {
        best = building;
        bestDistance = dist;
      }
    }
    return best;
  }

  private findPlayerBuilding(kind: BuildingType): BuildingEntity | undefined {
    return this.buildings.find((building) => building.team === 'player' && building.kind === kind && building.hp > 0);
  }

  private findEnemyBuilding(kind: BuildingType): BuildingEntity | undefined {
    return this.buildings.find((building) => building.team === 'enemy' && building.kind === kind && building.hp > 0);
  }

  private findSpawnPointNear(x: number, y: number): Phaser.Math.Vector2 {
    const angle = Phaser.Math.FloatBetween(0, Math.PI * 2);
    const radius = Phaser.Math.Between(76, 112);
    return new Phaser.Math.Vector2(x + Math.cos(angle) * radius, y + Math.sin(angle) * radius);
  }

  private autoAssignHarvesters(): void {
    for (const unit of this.units) {
      if (unit.team === 'player' && unit.kind === 'harvester') this.assignHarvester(unit);
    }
  }

  private assignHarvester(unit: UnitEntity): void {
    unit.harvestTarget = this.findNearestResource(unit.sprite.x, unit.sprite.y);
    if (unit.harvestTarget) {
      unit.moveTarget = new Phaser.Math.Vector2(unit.harvestTarget.sprite.x, unit.harvestTarget.sprite.y);
    }
  }

  private selectIdleHarvester(): void {
    const harvester = this.units.find((unit) => unit.team === 'player' && unit.kind === 'harvester');
    if (!harvester) return;
    this.clearSelection();
    this.setUnitSelected(harvester, true);
    this.cameras.main.pan(harvester.sprite.x, harvester.sprite.y, 240, 'Sine.easeInOut');
  }
}
