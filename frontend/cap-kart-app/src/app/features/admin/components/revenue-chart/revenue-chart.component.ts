import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

interface ChartData {
  label: string;
  value: number;
}

@Component({
  selector: 'app-revenue-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="chart-container">
      <!-- Loading State -->
      <div *ngIf="loading" class="loading-state">
        <div class="spinner"></div>
        <p>Loading chart data...</p>
      </div>

      <!-- Empty State -->
      <div *ngIf="!loading && chartData.length === 0" class="empty-state">
        <span class="empty-icon">📊</span>
        <p>No data available</p>
      </div>

      <!-- Chart -->
      <div *ngIf="!loading && chartData.length > 0" class="chart">
        <div class="y-axis">
          <span *ngFor="let label of yAxisLabels">{{ label }}</span>
        </div>
        <div class="chart-bars">
          <div class="bar-wrapper" *ngFor="let data of chartData">
            <div class="bar-container">
              <div 
                class="bar" 
                [style.height.%]="(data.value / maxValue) * 100"
                [title]="data.value.toString()"
              >
                <span class="bar-value">{{ data.value }}</span>
              </div>
            </div>
            <span class="x-label">{{ data.label }}</span>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .chart-container {
      height: 300px;
      padding: 20px 0;
      position: relative;
    }

    .loading-state, .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
      gap: 12px;
    }

    .spinner {
      width: 32px;
      height: 32px;
      border: 3px solid #e2e8f0;
      border-top: 3px solid var(--primary-blue);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .loading-state p, .empty-state p {
      color: #64748b;
      font-size: 14px;
      margin: 0;
    }

    .empty-icon {
      font-size: 48px;
      opacity: 0.5;
    }

    .chart {
      display: flex;
      height: 100%;
      gap: 16px;
    }

    .y-axis {
      display: flex;
      flex-direction: column-reverse;
      justify-content: space-between;
      font-size: 12px;
      color: #94a3b8;
      padding-right: 12px;
      border-right: 1px solid #e2e8f0;
    }

    .chart-bars {
      flex: 1;
      display: flex;
      align-items: flex-end;
      gap: 12px;
      padding-bottom: 24px;
    }

    .bar-wrapper {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
    }

    .bar-container {
      width: 100%;
      height: 240px;
      display: flex;
      align-items: flex-end;
    }

    .bar {
      width: 100%;
      background: linear-gradient(180deg, var(--primary-blue) 0%, var(--primary-green) 100%);
      border-radius: 8px 8px 0 0;
      transition: all 0.3s ease;
      position: relative;
      cursor: pointer;
      display: flex;
      align-items: flex-start;
      justify-content: center;
      padding-top: 8px;
    }

    .bar:hover {
      background: linear-gradient(180deg, #1e40af 0%, var(--primary-blue) 100%);
      transform: scaleY(1.02);
    }

    .bar-value {
      font-size: 11px;
      font-weight: 600;
      color: white;
      opacity: 0;
      transition: opacity 0.2s;
    }

    .bar:hover .bar-value {
      opacity: 1;
    }

    .x-label {
      font-size: 12px;
      color: #64748b;
      font-weight: 500;
    }

    @media (max-width: 768px) {
      .chart-container {
        height: 250px;
      }

      .bar-container {
        height: 190px;
      }

      .x-label {
        font-size: 10px;
      }
    }
  `]
})
export class RevenueChartComponent implements OnChanges {
  @Input() chartData: ChartData[] = [];
  @Input() loading = false;

  maxValue = 0;
  yAxisLabels: string[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['chartData'] && this.chartData.length > 0) {
      this.maxValue = Math.max(...this.chartData.map(d => d.value), 1);
      this.generateYAxisLabels();
    }
  }

  generateYAxisLabels(): void {
    const step = Math.ceil(this.maxValue / 5);
    this.yAxisLabels = [];
    for (let i = 0; i <= 5; i++) {
      const value = step * i;
      this.yAxisLabels.push(value.toString());
    }
  }
}
