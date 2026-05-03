import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../../../core/services/report.service';
import { RevenueChartComponent } from '../../components/revenue-chart/revenue-chart.component';
import { forkJoin, finalize } from 'rxjs';

interface StatCard {
  label: string;
  value: number | string;
  icon: string;
  color: string;
  subtitle?: string;
}

interface ChartData {
  label: string;
  value: number;
}

interface RejectedProduct {
  productId?: number;
  productName?: string;
  name?: string;
  rejectionCount?: number;
  count?: number;
  reason?: string;
  lastRejected?: string;
}

interface FailureItem {
  id?: number;
  workflowId?: string;
  error?: string;
  message?: string;
  timestamp?: string;
  step?: string;
}

@Component({
  selector: 'app-admin-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule, RevenueChartComponent],
  template: `
    <div class="analytics-page">
      <div class="page-header">
        <div>
          <h1>Analytics Dashboard</h1>
          <p>Comprehensive insights and performance metrics</p>
        </div>
        <div class="header-actions">
          <select class="period-select" [(ngModel)]="selectedPeriod" (change)="loadData()">
            <option value="7">Last 7 days</option>
            <option value="30">Last 30 days</option>
            <option value="90">Last 90 days</option>
          </select>
          <button class="btn-refresh" (click)="loadData()" [disabled]="loading">
            <span>🔄</span> Refresh
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <div class="loading-state" *ngIf="loading">
        <div class="spinner"></div>
        <p>Loading analytics data...</p>
      </div>

      <!-- Error State -->
      <div class="error-state" *ngIf="error && !loading">
        <span class="error-icon">⚠️</span>
        <h3>Failed to load analytics</h3>
        <p>{{ error }}</p>
        <button class="btn-primary" (click)="loadData()">
          <span>🔄</span> Try Again
        </button>
      </div>

      <!-- Analytics Content -->
      <ng-container *ngIf="!loading && !error">
        
        <!-- ═══════════════════════════════════════════════════════════════════ -->
        <!-- PRODUCT ANALYTICS SECTION -->
        <!-- ═══════════════════════════════════════════════════════════════════ -->
        <div class="section-header">
          <h2>📦 Product Analytics</h2>
        </div>

        <!-- Product Stats Cards -->
        <div class="stats-grid">
          <div class="stat-card" *ngFor="let card of productCards" [style.border-top-color]="card.color">
            <div class="stat-icon">{{ card.icon }}</div>
            <div class="stat-content">
              <div class="stat-value">{{ card.value }}</div>
              <div class="stat-label">{{ card.label }}</div>
              <div class="stat-subtitle" *ngIf="card.subtitle">{{ card.subtitle }}</div>
            </div>
          </div>
        </div>

        <!-- Product Trends Chart & Top Rejections -->
        <div class="charts-row">
          <div class="chart-card">
            <div class="card-header">
              <h3>Product Trends</h3>
            </div>
            <app-revenue-chart 
              [chartData]="productTrendData" 
              [loading]="productTrendsLoading">
            </app-revenue-chart>
          </div>

          <div class="chart-card">
            <div class="card-header">
              <h3>Top Rejected Products</h3>
            </div>
            <div class="list-content">
              <div *ngIf="topRejectedLoading" class="mini-loading">
                <div class="spinner-small"></div>
                <span>Loading...</span>
              </div>
              <div *ngIf="!topRejectedLoading && topRejectedProducts.length === 0" class="empty-mini">
                <span>📋</span>
                <p>No rejections found</p>
              </div>
              <div *ngIf="!topRejectedLoading && topRejectedProducts.length > 0" class="rejection-list">
                <div class="rejection-item" *ngFor="let item of topRejectedProducts">
                  <div class="rejection-info">
                    <div class="rejection-name">{{ item.productName || item.name || 'Product #' + item.productId }}</div>
                    <div class="rejection-reason" *ngIf="item.reason">{{ item.reason }}</div>
                  </div>
                  <div class="rejection-count">{{ item.rejectionCount || item.count || 0 }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- ═══════════════════════════════════════════════════════════════════ -->
        <!-- WORKFLOW ANALYTICS SECTION -->
        <!-- ═══════════════════════════════════════════════════════════════════ -->
        <div class="section-header">
          <h2>⚙️ Workflow Analytics</h2>
        </div>

        <!-- Workflow Stats Cards -->
        <div class="stats-grid">
          <div class="stat-card" *ngFor="let card of workflowCards" [style.border-top-color]="card.color">
            <div class="stat-icon">{{ card.icon }}</div>
            <div class="stat-content">
              <div class="stat-value">{{ card.value }}</div>
              <div class="stat-label">{{ card.label }}</div>
              <div class="stat-subtitle" *ngIf="card.subtitle">{{ card.subtitle }}</div>
            </div>
          </div>
        </div>

        <!-- Workflow Trends Chart & Failures -->
        <div class="charts-row">
          <div class="chart-card">
            <div class="card-header">
              <h3>Workflow Trends</h3>
            </div>
            <app-revenue-chart 
              [chartData]="workflowTrendData" 
              [loading]="workflowTrendsLoading">
            </app-revenue-chart>
          </div>

          <div class="chart-card">
            <div class="card-header">
              <h3>Recent Failures</h3>
            </div>
            <div class="list-content">
              <div *ngIf="workflowFailuresLoading" class="mini-loading">
                <div class="spinner-small"></div>
                <span>Loading...</span>
              </div>
              <div *ngIf="!workflowFailuresLoading && workflowFailures.length === 0" class="empty-mini">
                <span>✅</span>
                <p>No failures</p>
              </div>
              <div *ngIf="!workflowFailuresLoading && workflowFailures.length > 0" class="failure-list">
                <div class="failure-item" *ngFor="let item of workflowFailures">
                  <div class="failure-info">
                    <div class="failure-id">Workflow #{{ item.workflowId || item.id }}</div>
                    <div class="failure-message">{{ item.error || item.message || 'Unknown error' }}</div>
                  </div>
                  <div class="failure-time" *ngIf="item.timestamp">{{ formatTime(item.timestamp) }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- ═══════════════════════════════════════════════════════════════════ -->
        <!-- NOTIFICATION ANALYTICS SECTION -->
        <!-- ═══════════════════════════════════════════════════════════════════ -->
        <div class="section-header">
          <h2>🔔 Notification Analytics</h2>
        </div>

        <!-- Notification Stats Cards -->
        <div class="stats-grid">
          <div class="stat-card" *ngFor="let card of notificationCards" [style.border-top-color]="card.color">
            <div class="stat-icon">{{ card.icon }}</div>
            <div class="stat-content">
              <div class="stat-value">{{ card.value }}</div>
              <div class="stat-label">{{ card.label }}</div>
              <div class="stat-subtitle" *ngIf="card.subtitle">{{ card.subtitle }}</div>
            </div>
          </div>
        </div>

        <!-- Notification Trends Chart & Performance -->
        <div class="charts-row">
          <div class="chart-card">
            <div class="card-header">
              <h3>Notification Trends</h3>
            </div>
            <app-revenue-chart 
              [chartData]="notificationTrendData" 
              [loading]="notificationTrendsLoading">
            </app-revenue-chart>
          </div>

          <div class="chart-card">
            <div class="card-header">
              <h3>Performance Metrics</h3>
            </div>
            <div class="list-content">
              <div *ngIf="notificationPerfLoading" class="mini-loading">
                <div class="spinner-small"></div>
                <span>Loading...</span>
              </div>
              <div *ngIf="!notificationPerfLoading && notificationPerformance" class="perf-metrics">
                <div class="perf-item">
                  <span class="perf-label">Success Rate</span>
                  <span class="perf-value">{{ notificationPerformance.successRate || '—' }}</span>
                </div>
                <div class="perf-item">
                  <span class="perf-label">Avg Delivery Time</span>
                  <span class="perf-value">{{ notificationPerformance.avgDeliveryTime || '—' }}</span>
                </div>
                <div class="perf-item">
                  <span class="perf-label">Total Sent</span>
                  <span class="perf-value">{{ notificationPerformance.totalSent || '—' }}</span>
                </div>
                <div class="perf-item">
                  <span class="perf-label">Failed</span>
                  <span class="perf-value text-danger">{{ notificationPerformance.failed || '—' }}</span>
                </div>
              </div>
              <div *ngIf="!notificationPerfLoading && !notificationPerformance" class="empty-mini">
                <span>📊</span>
                <p>No performance data</p>
              </div>
            </div>
          </div>
        </div>

      </ng-container>
    </div>
  `,
  styles: [`
    .analytics-page {
      max-width: 1400px;
      margin: 0 auto;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 32px;
    }

    .page-header h1 {
      font-size: 32px;
      font-weight: 700;
      color: #1e293b;
      margin: 0 0 8px 0;
    }

    .page-header p {
      color: #64748b;
      margin: 0;
      font-size: 14px;
    }

    .header-actions {
      display: flex;
      gap: 12px;
      align-items: center;
    }

    .period-select {
      padding: 10px 16px;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      font-size: 14px;
      background: white;
      cursor: pointer;
      color: #1e293b;
    }

    .btn-refresh {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 20px;
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      color: #475569;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
      transition: all 0.2s;
    }

    .btn-refresh:hover:not(:disabled) {
      background: #f8fafc;
    }

    .btn-refresh:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .loading-state, .error-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 80px 20px;
      gap: 16px;
      background: white;
      border-radius: 12px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }

    .spinner {
      width: 48px;
      height: 48px;
      border: 4px solid #e2e8f0;
      border-top: 4px solid var(--primary-blue);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .loading-state p {
      color: #64748b;
      margin: 0;
    }

    .error-icon {
      font-size: 48px;
    }

    .error-state h3 {
      font-size: 20px;
      color: #1e293b;
      margin: 0;
    }

    .error-state p {
      color: #64748b;
      margin: 0;
    }

    .btn-primary {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px 24px;
      background: linear-gradient(90deg, #2563eb, #22c55e);
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      font-size: 14px;
      transition: all 0.2s;
      margin-top: 8px;
    }

    .btn-primary:hover {
      background: #4338ca;
    }

    .section-header {
      margin: 40px 0 24px;
    }

    .section-header h2 {
      font-size: 24px;
      font-weight: 700;
      color: #1e293b;
      margin: 0;
      padding-bottom: 12px;
      border-bottom: 3px solid #f1f5f9;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 20px;
      margin-bottom: 24px;
    }

    .stat-card {
      background: white;
      border-radius: 12px;
      padding: 24px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      border-top: 4px solid var(--primary-blue);
      display: flex;
      gap: 16px;
      align-items: flex-start;
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .stat-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(0,0,0,0.08);
    }

    .stat-icon {
      font-size: 32px;
      line-height: 1;
    }

    .stat-content {
      flex: 1;
    }

    .stat-value {
      font-size: 28px;
      font-weight: 700;
      color: #1e293b;
      line-height: 1;
      margin-bottom: 4px;
    }

    .stat-label {
      font-size: 14px;
      color: #64748b;
      margin-bottom: 4px;
    }

    .stat-subtitle {
      font-size: 12px;
      color: #94a3b8;
    }

    .charts-row {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 24px;
      margin-bottom: 24px;
    }

    .chart-card {
      background: white;
      border-radius: 12px;
      padding: 24px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }

    .card-header {
      margin-bottom: 20px;
    }

    .card-header h3 {
      font-size: 18px;
      font-weight: 600;
      color: #1e293b;
      margin: 0;
    }

    .list-content {
      min-height: 300px;
      display: flex;
      flex-direction: column;
    }

    .mini-loading, .empty-mini {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 60px 20px;
      gap: 12px;
    }

    .spinner-small {
      width: 32px;
      height: 32px;
      border: 3px solid #e2e8f0;
      border-top: 3px solid var(--primary-blue);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    .mini-loading span, .empty-mini p {
      color: #64748b;
      font-size: 14px;
      margin: 0;
    }

    .empty-mini span {
      font-size: 48px;
      opacity: 0.5;
    }

    .rejection-list, .failure-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .rejection-item, .failure-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 12px;
      background: #f8fafc;
      border-radius: 8px;
      border-left: 3px solid #ef4444;
      transition: background 0.2s;
    }

    .rejection-item:hover, .failure-item:hover {
      background: #f1f5f9;
    }

    .rejection-info, .failure-info {
      flex: 1;
      min-width: 0;
    }

    .rejection-name, .failure-id {
      font-size: 14px;
      font-weight: 600;
      color: #1e293b;
      margin-bottom: 4px;
    }

    .rejection-reason, .failure-message {
      font-size: 12px;
      color: #64748b;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .rejection-count {
      font-size: 20px;
      font-weight: 700;
      color: #ef4444;
      margin-left: 12px;
    }

    .failure-time {
      font-size: 12px;
      color: #94a3b8;
      white-space: nowrap;
      margin-left: 12px;
    }

    .perf-metrics {
      display: flex;
      flex-direction: column;
      gap: 16px;
      padding: 12px 0;
    }

    .perf-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 12px 16px;
      background: #f8fafc;
      border-radius: 8px;
    }

    .perf-label {
      font-size: 14px;
      color: #64748b;
    }

    .perf-value {
      font-size: 16px;
      font-weight: 600;
      color: #1e293b;
    }

    .text-danger {
      color: #ef4444;
    }

    @media (max-width: 1024px) {
      .charts-row {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 768px) {
      .stats-grid {
        grid-template-columns: 1fr;
      }

      .header-actions {
        flex-direction: column;
        align-items: stretch;
      }
    }
  `]
})
export class AdminAnalyticsComponent implements OnInit {
  private reportService = inject(ReportService);

  loading = false;
  error = '';
  selectedPeriod = '30';

  // Product Analytics
  productCards: StatCard[] = [];
  productTrendData: ChartData[] = [];
  productTrendsLoading = false;
  topRejectedProducts: RejectedProduct[] = [];
  topRejectedLoading = false;

  // Workflow Analytics
  workflowCards: StatCard[] = [];
  workflowTrendData: ChartData[] = [];
  workflowTrendsLoading = false;
  workflowFailures: FailureItem[] = [];
  workflowFailuresLoading = false;

  // Notification Analytics
  notificationCards: StatCard[] = [];
  notificationTrendData: ChartData[] = [];
  notificationTrendsLoading = false;
  notificationPerformance: any = null;
  notificationPerfLoading = false;

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.error = '';

    const from = this.getFromDate();
    const query = { from };

    // Load all main reports
    forkJoin({
      products: this.reportService.getProductReport(query),
      workflows: this.reportService.getWorkflowReport(query),
      notifications: this.reportService.getNotificationReport(query)
    })
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: ({ products, workflows, notifications }) => {
          // Product cards
          this.productCards = [
            { label: 'Total Products', value: products.totalCount ?? 0, icon: '📦', color: '#2563eb' },
            { label: 'Published', value: products['publishedCount'] ?? products['published'] ?? '—', icon: '✅', color: '#10b981' },
            { label: 'Pending Approval', value: products['pendingCount'] ?? products['pending'] ?? '—', icon: '⏳', color: '#f59e0b' },
            { label: 'Rejected', value: products['rejectedCount'] ?? products['rejected'] ?? '—', icon: '❌', color: '#ef4444' }
          ];

          // Workflow cards
          this.workflowCards = [
            { label: 'Total Workflows', value: workflows.totalCount ?? 0, icon: '⚙️', color: '#6366f1' },
            { label: 'Completed', value: workflows['completedCount'] ?? workflows['completed'] ?? '—', icon: '✔️', color: '#10b981' },
            { label: 'In Progress', value: workflows['inProgressCount'] ?? workflows['inProgress'] ?? '—', icon: '🔄', color: '#0ea5e9' },
            { label: 'Failed', value: workflows['failedCount'] ?? workflows['failed'] ?? '—', icon: '⚠️', color: '#ef4444' }
          ];

          // Notification cards
          this.notificationCards = [
            { label: 'Total Sent', value: notifications.totalCount ?? 0, icon: '🔔', color: '#22c55e' },
            { label: 'Delivered', value: notifications['deliveredCount'] ?? notifications['delivered'] ?? '—', icon: '📬', color: '#10b981' },
            { label: 'Failed', value: notifications['failedCount'] ?? notifications['failed'] ?? '—', icon: '📭', color: '#ef4444' },
            { label: 'Pending', value: notifications['pendingCount'] ?? notifications['pending'] ?? '—', icon: '⏱️', color: '#f59e0b' }
          ];

          // Load additional data
          this.loadProductTrends(query);
          this.loadTopRejections(query);
          this.loadWorkflowTrends(query);
          this.loadWorkflowFailures(query);
          this.loadNotificationTrends(query);
          this.loadNotificationPerformance(query);
        },
        error: (err) => {
          console.error('Failed to load analytics:', err);
          this.error = 'Failed to load analytics data. Please try again.';
        }
      });
  }

  loadProductTrends(query: any): void {
    this.productTrendsLoading = true;
    this.reportService.getProductTrends(query)
      .pipe(finalize(() => this.productTrendsLoading = false))
      .subscribe({
        next: (data) => {
          if (data && Array.isArray(data)) {
            this.productTrendData = data.map((item: any) => ({
              label: this.formatTrendLabel(item.date || item.label || item.period),
              value: item.count || item.value || 0
            }));
          } else if (data && data['data'] && Array.isArray(data['data'])) {
            this.productTrendData = data['data'].map((item: any) => ({
              label: this.formatTrendLabel(item.date || item.label || item.period),
              value: item.count || item.value || 0
            }));
          } else {
            this.productTrendData = [];
          }
        },
        error: (err) => {
          console.error('Product trends error:', err);
          this.productTrendData = [];
        }
      });
  }

  loadTopRejections(query: any): void {
    this.topRejectedLoading = true;
    this.reportService.getTopRejectedProducts(query)
      .pipe(finalize(() => this.topRejectedLoading = false))
      .subscribe({
        next: (data) => {
          if (data && Array.isArray(data)) {
            this.topRejectedProducts = data.slice(0, 5);
          } else if (data && data['data'] && Array.isArray(data['data'])) {
            this.topRejectedProducts = data['data'].slice(0, 5);
          } else {
            this.topRejectedProducts = [];
          }
        },
        error: (err) => {
          console.error('Top rejections error:', err);
          this.topRejectedProducts = [];
        }
      });
  }

  loadWorkflowTrends(query: any): void {
    this.workflowTrendsLoading = true;
    this.reportService.getWorkflowTrends(query)
      .pipe(finalize(() => this.workflowTrendsLoading = false))
      .subscribe({
        next: (data) => {
          if (data && Array.isArray(data)) {
            this.workflowTrendData = data.map((item: any) => ({
              label: this.formatTrendLabel(item.date || item.label || item.period),
              value: item.count || item.value || 0
            }));
          } else if (data && data['data'] && Array.isArray(data['data'])) {
            this.workflowTrendData = data['data'].map((item: any) => ({
              label: this.formatTrendLabel(item.date || item.label || item.period),
              value: item.count || item.value || 0
            }));
          } else {
            this.workflowTrendData = [];
          }
        },
        error: (err) => {
          console.error('Workflow trends error:', err);
          this.workflowTrendData = [];
        }
      });
  }

  loadWorkflowFailures(query: any): void {
    this.workflowFailuresLoading = true;
    this.reportService.getWorkflowFailures(query)
      .pipe(finalize(() => this.workflowFailuresLoading = false))
      .subscribe({
        next: (data) => {
          if (data && Array.isArray(data)) {
            this.workflowFailures = data.slice(0, 5);
          } else if (data && data['data'] && Array.isArray(data['data'])) {
            this.workflowFailures = data['data'].slice(0, 5);
          } else {
            this.workflowFailures = [];
          }
        },
        error: (err) => {
          console.error('Workflow failures error:', err);
          this.workflowFailures = [];
        }
      });
  }

  loadNotificationTrends(query: any): void {
    this.notificationTrendsLoading = true;
    this.reportService.getNotificationTrends(query)
      .pipe(finalize(() => this.notificationTrendsLoading = false))
      .subscribe({
        next: (data) => {
          if (data && Array.isArray(data)) {
            this.notificationTrendData = data.map((item: any) => ({
              label: this.formatTrendLabel(item.date || item.label || item.period),
              value: item.count || item.value || 0
            }));
          } else if (data && data['data'] && Array.isArray(data['data'])) {
            this.notificationTrendData = data['data'].map((item: any) => ({
              label: this.formatTrendLabel(item.date || item.label || item.period),
              value: item.count || item.value || 0
            }));
          } else {
            this.notificationTrendData = [];
          }
        },
        error: (err) => {
          console.error('Notification trends error:', err);
          this.notificationTrendData = [];
        }
      });
  }

  loadNotificationPerformance(query: any): void {
    this.notificationPerfLoading = true;
    this.reportService.getNotificationPerformance(query)
      .pipe(finalize(() => this.notificationPerfLoading = false))
      .subscribe({
        next: (data) => {
          this.notificationPerformance = data || null;
        },
        error: (err) => {
          console.error('Notification performance error:', err);
          this.notificationPerformance = null;
        }
      });
  }

  formatTrendLabel(date: string): string {
    if (!date) return '';
    try {
      const d = new Date(date);
      return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    } catch {
      return date;
    }
  }

  formatTime(timestamp: string): string {
    if (!timestamp) return '';
    try {
      const d = new Date(timestamp);
      const now = new Date();
      const diffMs = now.getTime() - d.getTime();
      const diffMins = Math.floor(diffMs / 60000);
      
      if (diffMins < 1) return 'Just now';
      if (diffMins < 60) return `${diffMins}m ago`;
      
      const diffHours = Math.floor(diffMins / 60);
      if (diffHours < 24) return `${diffHours}h ago`;
      
      const diffDays = Math.floor(diffHours / 24);
      return `${diffDays}d ago`;
    } catch {
      return timestamp;
    }
  }

  private getFromDate(): string {
    const d = new Date();
    d.setDate(d.getDate() - parseInt(this.selectedPeriod));
    return d.toISOString().slice(0, 10);
  }
}

