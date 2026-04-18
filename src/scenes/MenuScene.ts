import Phaser from 'phaser';
import { CAMPAIGN_LEVELS } from '../data/campaign';

export class MenuScene extends Phaser.Scene {
  constructor() {
    super('MenuScene');
  }

  create(): void {
    const { width, height } = this.scale;
    this.cameras.main.setBackgroundColor('#101312');

    this.add.rectangle(width / 2, height / 2, width, height, 0x18201b);
    this.add.rectangle(width / 2, 0, width, 190, 0x26332a).setOrigin(0.5, 0);

    this.add.text(70, 64, 'RED ALERT RTS', {
      color: '#f1f5df',
      fontSize: '44px',
      fontStyle: '700'
    });
    this.add.text(72, 116, 'Single-player campaign prototype', {
      color: '#a7b39c',
      fontSize: '20px'
    });

    this.add.text(72, 198, 'Campaign', {
      color: '#f1f5df',
      fontSize: '26px',
      fontStyle: '700'
    });

    CAMPAIGN_LEVELS.forEach((level, index) => {
      const x = 76;
      const y = 250 + index * 110;
      const card = this.add.rectangle(x, y, 760, 82, 0x202a22, 0.92).setOrigin(0, 0);
      card.setStrokeStyle(2, 0x6d806c);
      this.add.text(x + 22, y + 14, level.title, {
        color: '#f1f5df',
        fontSize: '22px',
        fontStyle: '700'
      });
      this.add.text(x + 22, y + 44, level.briefing, {
        color: '#c1cdb6',
        fontSize: '15px',
        wordWrap: { width: 600 }
      });
      const button = this.add.rectangle(x + 642, y + 22, 92, 38, 0xb9423f).setOrigin(0, 0);
      button.setStrokeStyle(2, 0xf1c2a4);
      const label = this.add.text(x + 665, y + 31, 'Start', {
        color: '#fff8ee',
        fontSize: '15px',
        fontStyle: '700'
      });
      button.setInteractive({ useHandCursor: true });
      button.on('pointerup', () => {
        this.scene.start('GameScene', { levelIndex: index });
      });
      label.setInteractive({ useHandCursor: true });
      label.on('pointerup', () => {
        this.scene.start('GameScene', { levelIndex: index });
      });
    });

    this.add.text(72, height - 92, 'Controls: drag-select units, right-click to move or attack. Build from the command bar. WASD moves the camera.', {
      color: '#c1cdb6',
      fontSize: '16px'
    });
  }
}
