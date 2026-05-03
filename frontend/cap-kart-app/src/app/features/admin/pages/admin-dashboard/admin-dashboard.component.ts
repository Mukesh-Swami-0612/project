import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { DashboardService, DashboardSummary } from '../../../../core/services/dashboard.service';
import { ReportService } from '../../../../core/services/report.service';
import { StatsCardComponent } from '../../components/stats-card/stats-card.component';
import { RevenueChartComponent } from '../../components/revenue-chart/revenue-chart.component';
import { RecentActivityComponent } from '../../components/recent-activity/recent-activity.component';
import { finalize, forkJoin } from 'rxjs';

interface DashboardStats {
  totalProducts: number;
  totalOrders: number;
  publishedProducts: number;
  pendingApprovals: number;
  rejectedProducts: number;
  totalNotifications: number;
}

interface ChartData {
  label: string;
  value: number;
}

interface Activity {
  icon: string;
  title: string;
  description: string;
  time: string;
  type: 'success' | 'warning' | 'info' | 'error';
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    StatsCardComponent,
    RevenueChartComponent,
    RecentActivityComponent
  ],
  template: `
    <div class="dashboard">
      <div class="dashboard-header">
        <div>
          <h1>Dashboard</h1>
          <p>Welcome back! Here's what's happening today.</p>
        </div>
        <button class="refresh-btn" (click)="refreshData()">
          <span>🔄</span> Refresh
        </button>
      </div>

      <!-- Loading State -->
      <div class="loading-state" *ngIf="loading">
        <div class="spinner"></div>
        <p>Loading dashboard data...</p>
      </div>

      <!-- Error State -->
      <div class="error-state" *ngIf="error && !loading">
        <span class="error-icon">⚠️</span>
        <h3>Failed to load dashboard</h3>
        <p>{{ error }}</p>
        <button class="btn-primary" (click)="loadDashboardData()">
          <span>🔄</span> Try Again
        </button>
      </div>

      <!-- Dashboard Content -->
      <div *ngIf="!loading && !error">
        <!-- KPI Cards -->
        <div class="stats-grid">
          <app-stats-card
            title="Total Products"
            [value]="stats.totalProducts"
            icon="📦"
            color="green"
          ></app-stats-card>

          <app-stats-card
            title="Published Products"
            [value]="stats.publishedProducts"
            icon="✅"
            color="green"
          ></app-stats-card>

          <app-stats-card
            title="Pending Approvals"
            [value]="stats.pendingApprovals"
            icon="⏳"
            color="orange"
          ></app-stats-card>

          <app-stats-card
            title="Total Workflows"
            [value]="stats.totalOrders"
            icon="🛍️"
            color="blue"
          ></app-stats-card>
        </div>

        <!-- Charts Section -->
        <div class="charts-section">
          <div class="chart-card">
            <div class="card-header">
              <h3>Workflow Trends</h3>
            </div>
            <app-revenue-chart [chartData]="workflowTrendData" [loading]="trendsLoading"></app-revenue-chart>
          </div>

          <div class="chart-card">
            <div class="card-header">
              <h3>Product Status</h3>
            </div>
            <div class="status-chart">
              <div class="status-item">
                <div class="status-info">
                  <span class="status-name">Published</span>
                  <span class="status-value">{{ stats.publishedProducts }}</span>
                </div>
                <div class="progress-bar">
                  <div class="progress-fill" 
                       [style.width.%]="getPercentage(stats.publishedProducts, stats.totalProducts)" 
                       style="background: #10b981"></div>
                </div>
              </div>
              <div class="status-item">
                <div class="status-info">
                  <span class="status-name">Pending Approval</span>
                  <span class="status-value">{{ stats.pendingApprovals }}</span>
                </div>
                <div class="progress-bar">
                  <div class="progress-fill" 
                       [style.width.%]="getPercentage(stats.pendingApprovals, stats.totalProducts)" 
                       style="background: #f59e0b"></div>
                </div>
              </div>
              <div class="status-item">
                <div class="status-info">
                  <span class="status-name">Rejected</span>
                  <span class="status-value">{{ stats.rejectedProducts }}</span>
                </div>
                <div class="progress-bar">
                  <div class="progress-fill" 
                       [style.width.%]="getPercentage(stats.rejectedProducts, stats.totalProducts)" 
                       style="background: #ef4444"></div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Recent Activity -->
        <div class="activity-section">
          <app-recent-activity [activities]="recentActivities" [loading]="activityLoading"></app-recent-activity>

          <div class="quick-actions-card">
            <h3>Quick Actions</h3>
            <div class="actions-grid">
              <button class="action-btn" (click)="navigateTo('/admin/products')">
                <span class="action-icon">📦</span>
                <span class="action-label">Add Product</span>
              </button>
              <button class="action-btn" (click)="navigateTo('/admin/orders')">
                <span class="action-icon">🛍️</span>
                <span class="action-label">View Orders</span>
              </button>
              <button class="action-btn" (click)="navigateTo('/admin/users')">
                <span class="action-icon">👥</span>
                <span class="action-label">Manage Users</span>
              </button>
              <button class="action-btn" (click)="navigateTo('/admin/reports')">
                <span class="action-icon">📊</span>
                <span class="action-label">View Reports</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard {
      max-width: 1400px;
      margin: 0 auto;
    }

    .loading-state, .error-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 80px 20px;
      background: white;
      border-radius: 12px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      margin-bottom: 32px;
    }

    .spinner {
      width: 48px;
      height: 48px;
      border: 4px solid #e2e8f0;
      border-top: 4px solid var(--primary-blue);
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin-bottom: 16px;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .loading-state p {
      color: #64748b;
      margin: 0;
    }

    .error-state {
      gap: 16px;
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
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
      font-size: 14px;
      background: linear-gradient(90deg, #2563eb, #22c55e);
      color: white;
      margin-top: 8px;
    }

    .btn-primary:hover {
      background: #4338ca;
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(79, 70, 229, 0.3);
    }

    .dashboard-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 32px;
    }

    .dashboard-header h1 {
      font-size: 32px;
      font-weight: 700;
      color: #1e293b;
      margin: 0 0 8px 0;
    }

    .dashboard-header p {
      color: #64748b;
      margin: 0;
      font-size: 14px;
    }

    .refresh-btn {
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

    .refresh-btn:hover {
      background: #f8fafc;
      border-color: #cbd5e1;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 24px;
      margin-bottom: 32px;
    }

    .charts-section {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 24px;
      margin-bottom: 32px;
    }

    .chart-card {
      background: white;
      border-radius: 12px;
      padding: 24px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }

    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }

    .card-header h3 {
      font-size: 18px;
      font-weight: 600;
      color: #1e293b;
      margin: 0;
    }

    .period-select {
      padding: 6px 12px;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      font-size: 13px;
      color: #475569;
      background: white;
      cursor: pointer;
    }

    .category-chart {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .status-chart {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .category-item,
    .status-item {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .category-info,
    .status-info {
      display: flex;
      justify-content: space-between;
      font-size: 14px;
    }

    .category-name,
    .status-name {
      color: #475569;
    }

    .category-value,
    .status-value {
      font-weight: 600;
      color: #1e293b;
    }

    .progress-bar {
      height: 8px;
      background: #f1f5f9;
      border-radius: 4px;
      overflow: hidden;
    }

    .progress-fill {
      height: 100%;
      transition: width 0.3s ease;
    }

    .activity-section {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: 24px;
    }

    .quick-actions-card {
      background: white;
      border-radius: 12px;
      padding: 24px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }

    .quick-actions-card h3 {
      font-size: 18px;
      font-weight: 600;
      color: #1e293b;
      margin: 0 0 20px 0;
    }

    .actions-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }

    .action-btn {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
      padding: 20px;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s;
    }

    .action-btn:hover {
      background: linear-gradient(90deg, #1e40af, #16a34a);
      border-color: var(--primary-blue);
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(79, 70, 229, 0.2);
    }

    .action-btn:hover .action-icon,
    .action-btn:hover .action-label {
      color: white;
    }

    .action-icon {
      font-size: 32px;
    }

    .action-label {
      font-size: 13px;
      font-weight: 500;
      color: #475569;
    }

    @media (max-width: 1024px) {
      .charts-section,
      .activity-section {
        grid-template-columns: 1fr;
      }
    }

    @media (max-width: 768px) {
      .stats-grid {
        grid-template-columns: 1fr;
      }

      .actions-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class AdminDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private reportService = inject(ReportService);
  private router = inject(Router);

  loading = false;
  error = '';
  trendsLoading = false;
  activityLoading = false;

  stats: DashboardStats = {
    totalProducts: 0,
    totalOrders: 0,
    publishedProducts: 0,
    pendingApprovals: 0,
    rejectedProducts: 0,
    totalNotifications: 0
  };

  workflowTrendData: ChartData[] = [];
  recentActivities: Activity[] = [];

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;
    this.error = '';

    forkJoin({
      summary: this.dashboardService.getSummary(),
      trends: this.reportService.getWorkflowTrends()
    })
    .pipe(finalize(() => this.loading = false))
    .subscribe({
      next: (results) => {
        const summary = results.summary;
        this.stats.totalProducts = summary.totalProducts ?? 0;
        this.stats.publishedProducts = summary.publishedProducts ?? 0;
        this.stats.pendingApprovals = summary.pendingApprovals ?? 0;
        this.stats.rejectedProducts = summary.rejectedProducts ?? 0;
        this.stats.totalOrders = summary.totalWorkflows ?? 0;
        this.stats.totalNotifications = summary.totalNotifications ?? 0;

        if (summary.recentActivity && Array.isArray(summary.recentActivity)) {
          this.recentActivities = this.mapRecentActivity(summary.recentActivity);
        }

        const trendsData = results.trends;
        if (trendsData && Array.isArray(trendsData)) {
          this.workflowTrendData = trendsData.map((item: any) => ({
            label: item.date || item.status || 'Unknown',
            value: item.count || 0
          }));
        } else {
          this.workflowTrendData = [];
        }
      },
      error: (err) => {
        console.error('Failed to load dashboard data:', err);
        this.error = 'Failed to load dashboard data. Please try again.';
      }
    });
  }


  mapRecentActivity(activities: any[]): Activity[] {
    return activities.slice(0, 6).map((item: any) => ({
      icon: this.getActivityIcon(item.type),
      title: item.title || 'Activity',
      description: item.description || '',
      time: this.formatActivityTime(item.timestamp || item.time),
      type: this.getActivityType(item.type)
    }));
  }

  getActivityIcon(type: string): string {
    const icons: Record<string, string> = {
      'product_approved': '✅',
      'product_rejected': '❌',
      'workflow_started': '🛍️',
      'workflow_completed': '✅',
      'workflow_failed': '⚠️',
      'notification_sent': '📧',
      'default': '📋'
    };
    return icons[type] || icons['default'];
  }

  getActivityType(type: string): 'success' | 'warning' | 'info' | 'error' {
    if (type?.includes('approved') || type?.includes('completed')) return 'success';
    if (type?.includes('failed') || type?.includes('rejected')) return 'error';
    if (type?.includes('warning') || type?.includes('alert')) return 'warning';
    return 'info';
  }

  formatActivityTime(timestamp: string): string {
    if (!timestamp) return 'Recently';
    
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins} min ago`;
    
    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    
    const diffDays = Math.floor(diffHours / 24);
    return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
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

  getPercentage(value: number, total: number): number {
    if (total === 0) return 0;
    return Math.round((value / total) * 100);
  }

  refreshData(): void {
    this.loadDashboardData();
  }

  navigateTo(route: string): void {
    this.router.navigate([route]);
  }
}
