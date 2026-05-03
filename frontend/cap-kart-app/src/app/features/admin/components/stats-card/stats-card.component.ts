import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-stats-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="stats-card" [class]="'color-' + color">
      <div class="card-content">
        <div class="card-header">
          <div class="icon-wrapper">
            <span class="icon">{{ icon }}</span>
          </div>
          <div class="growth" [class.negative]="growth < 0">
            <span class="arrow">{{ growth >= 0 ? '↑' : '↓' }}</span>
            <span>{{ Math.abs(growth) }}%</span>
          </div>
        </div>
        <div class="card-body">
          <div class="value">{{ prefix }}{{ formatValue(value) }}</div>
          <div class="title">{{ title }}</div>
        </div>
      </div>
      <div class="card-footer">
        <span class="footer-text">vs last period</span>
      </div>
    </div>
  `,
  styles: [`
    .stats-card {
      background: white;
      border-radius: 12px;
      padding: 24px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      transition: all 0.3s ease;
      border-left: 4px solid transparent;
    }

    .stats-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 20px rgba(0,0,0,0.1);
    }

    .stats-card.color-blue {
      border-left-color: #3b82f6;
    }

    .stats-card.color-green {
      border-left-color: #10b981;
    }

    .stats-card.color-blue {
      border-left-color: #2563eb;
    }

    .stats-card.color-green {
      border-left-color: #22c55e;
    }

    .stats-card.color-orange {
      border-left-color: #f59e0b;
    }

    .card-content {
      margin-bottom: 12px;
    }

    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
    }

    .icon-wrapper {
      width: 48px;
      height: 48px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 24px;
    }

    .color-blue .icon-wrapper {
      background: #eff6ff;
    }

    .color-green .icon-wrapper {
      background: #f0fdf4;
    }

    .color-blue .icon-wrapper {
      background: rgba(37, 99, 235, 0.1);
    }

    .color-green .icon-wrapper {
      background: rgba(34, 197, 94, 0.1);
    }

    .color-orange .icon-wrapper {
      background: #fffbeb;
    }

    .growth {
      display: flex;
      align-items: center;
      gap: 4px;
      padding: 4px 12px;
      border-radius: 20px;
      background: #dcfce7;
      color: #16a34a;
      font-size: 13px;
      font-weight: 600;
    }

    .growth.negative {
      background: #fee2e2;
      color: #dc2626;
    }

    .arrow {
      font-size: 14px;
    }

    .card-body {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .value {
      font-size: 32px;
      font-weight: 700;
      color: #1e293b;
      line-height: 1;
    }

    .title {
      font-size: 14px;
      color: #64748b;
      font-weight: 500;
    }

    .card-footer {
      padding-top: 12px;
      border-top: 1px solid #f1f5f9;
    }

    .footer-text {
      font-size: 12px;
      color: #94a3b8;
    }
  `]
})
export class StatsCardComponent {
  @Input() title = '';
  @Input() value: number = 0;
  @Input() icon = '📊';
  @Input() growth: number = 0;
  @Input() color: 'blue' | 'green' | 'orange' = 'blue';
  @Input() prefix = '';

  Math = Math;

  formatValue(value: number): string {
    if (value >= 1000000) {
      return (value / 1000000).toFixed(1) + 'M';
    } else if (value >= 1000) {
      return (value / 1000).toFixed(1) + 'K';
    }
    return value.toLocaleString();
  }
}
