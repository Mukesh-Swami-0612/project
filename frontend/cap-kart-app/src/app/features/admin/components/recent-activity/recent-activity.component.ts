import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

interface Activity {
  icon: string;
  title: string;
  description: string;
  time: string;
  type: 'success' | 'warning' | 'info' | 'error';
}

@Component({
  selector: 'app-recent-activity',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="activity-card">
      <div class="card-header">
        <h3>Recent Activity</h3>
      </div>

      <!-- Loading State -->
      <div *ngIf="loading" class="loading-state">
        <div class="spinner"></div>
        <p>Loading activities...</p>
      </div>

      <!-- Empty State -->
      <div *ngIf="!loading && activities.length === 0" class="empty-state">
        <span class="empty-icon">📋</span>
        <p>No recent activity</p>
      </div>

      <!-- Activity List -->
      <div *ngIf="!loading && activities.length > 0" class="activity-list">
        <div class="activity-item" *ngFor="let activity of activities">
          <div class="activity-icon" [class]="'type-' + activity.type">
            <span>{{ activity.icon }}</span>
          </div>
          <div class="activity-content">
            <div class="activity-title">{{ activity.title }}</div>
            <div class="activity-description">{{ activity.description }}</div>
          </div>
          <div class="activity-time">{{ activity.time }}</div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .activity-card {
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

    .loading-state, .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 60px 20px;
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

    .activity-list {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .activity-item {
      display: flex;
      gap: 16px;
      padding: 16px;
      border-radius: 8px;
      background: #f8fafc;
      transition: all 0.2s;
    }

    .activity-item:hover {
      background: #f1f5f9;
    }

    .activity-icon {
      width: 40px;
      height: 40px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 20px;
      flex-shrink: 0;
    }

    .activity-icon.type-success {
      background: #dcfce7;
    }

    .activity-icon.type-warning {
      background: #fef3c7;
    }

    .activity-icon.type-info {
      background: #dbeafe;
    }

    .activity-icon.type-error {
      background: #fee2e2;
    }

    .activity-content {
      flex: 1;
      min-width: 0;
    }

    .activity-title {
      font-size: 14px;
      font-weight: 600;
      color: #1e293b;
      margin-bottom: 4px;
    }

    .activity-description {
      font-size: 13px;
      color: #64748b;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .activity-time {
      font-size: 12px;
      color: #94a3b8;
      white-space: nowrap;
    }

    @media (max-width: 768px) {
      .activity-time {
        display: none;
      }
    }
  `]
})
export class RecentActivityComponent {
  @Input() activities: Activity[] = [];
  @Input() loading = false;
}
