import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { OrderService } from '../../../../core/services/order.service';
import { Order, OrderHistory, WorkflowStatus, WorkflowStep } from '../../../../core/models/order.model';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-admin-order-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="order-detail-container">
      <div class="header">
        <a routerLink="/admin/orders" class="back-link">← Back to Orders</a>
        <h2>Order Details</h2>
      </div>

      <!-- Loading State -->
      <div *ngIf="loading" class="loading-state">
        <div class="spinner"></div>
        <p>Loading order details...</p>
      </div>

      <!-- Error State -->
      <div *ngIf="error && !loading" class="error-state">
        <span class="error-icon">⚠️</span>
        <h3>Failed to load order</h3>
        <p>{{ error }}</p>
        <button class="btn-primary" (click)="loadOrder()">
          <span>🔄</span> Try Again
        </button>
      </div>

      <!-- Order Content -->
      <div *ngIf="!loading && !error && order" class="order-content">
        
        <!-- Order Overview Card -->
        <div class="card">
          <div class="card-header">
            <h3>Order Overview</h3>
            <span class="status-badge" [class]="'status-' + order.status.toLowerCase()">
              {{ order.status }}
            </span>
          </div>
          <div class="card-body">
            <div class="info-grid">
              <div class="info-item">
                <label>Order ID</label>
                <span class="value monospace">{{ order.id }}</span>
              </div>
              <div class="info-item">
                <label>Product ID</label>
                <span class="value product-id">#{{ order.productId }}</span>
              </div>
              <div class="info-item">
                <label>Current Step</label>
                <span class="value">{{ formatStep(order.currentStep) }}</span>
              </div>
              <div class="info-item">
                <label>Retry Count</label>
                <span class="value">{{ order.retryCount }} / {{ order.maxRetries }}</span>
              </div>
              <div class="info-item">
                <label>Created At</label>
                <span class="value">{{ formatDate(order.createdAt) }}</span>
              </div>
              <div class="info-item">
                <label>Updated At</label>
                <span class="value">{{ formatDate(order.updatedAt) }}</span>
              </div>
              <div class="info-item" *ngIf="order.completedAt">
                <label>Completed At</label>
                <span class="value">{{ formatDate(order.completedAt) }}</span>
              </div>
              <div class="info-item" *ngIf="order.correlationId">
                <label>Correlation ID</label>
                <span class="value monospace small">{{ order.correlationId }}</span>
              </div>
            </div>

            <!-- Last Error (if exists) -->
            <div *ngIf="order.lastError" class="error-box">
              <strong>Last Error:</strong>
              <p>{{ order.lastError }}</p>
            </div>

            <!-- Next Retry (if scheduled) -->
            <div *ngIf="order.nextRetryAt" class="info-box">
              <strong>Next Retry Scheduled:</strong>
              <p>{{ formatDate(order.nextRetryAt) }}</p>
            </div>
          </div>
        </div>

        <!-- Order Actions Card -->
        <div class="card">
          <div class="card-header">
            <h3>Actions</h3>
          </div>
          <div class="card-body">
            <div class="action-buttons">
              <button 
                *ngIf="canRetry()"
                class="btn-warning" 
                (click)="retryOrder()"
                [disabled]="actionLoading"
              >
                <span>🔄</span> Retry Order
              </button>
              <button 
                *ngIf="canCancel()"
                class="btn-danger" 
                (click)="cancelOrder()"
                [disabled]="actionLoading"
              >
                <span>❌</span> Cancel Order
              </button>
              <span *ngIf="!canRetry() && !canCancel()" class="no-actions">
                No actions available for this order
              </span>
            </div>
          </div>
        </div>

        <!-- Order History Card -->
        <div class="card">
          <div class="card-header">
            <h3>Order History</h3>
            <button class="btn-icon" (click)="loadHistory()" [disabled]="historyLoading">
              <span>🔄</span>
            </button>
          </div>
          <div class="card-body">
            
            <!-- History Loading -->
            <div *ngIf="historyLoading" class="history-loading">
              <div class="spinner-small"></div>
              <span>Loading history...</span>
            </div>

            <!-- History Error -->
            <div *ngIf="historyError && !historyLoading" class="history-error">
              <span>⚠️ {{ historyError }}</span>
            </div>

            <!-- History Timeline -->
            <div *ngIf="!historyLoading && !historyError && history.length > 0" class="timeline">
              <div *ngFor="let item of history; let isLast = last" class="timeline-item" [class.last]="isLast">
                <div class="timeline-marker"></div>
                <div class="timeline-content">
                  <div class="timeline-header">
                    <span class="step-name">{{ formatStep(item.step) }}</span>
                    <span class="timeline-status" [class]="'status-' + item.status.toLowerCase()">
                      {{ item.status }}
                    </span>
                  </div>
                  <div class="timeline-time">{{ formatDate(item.timestamp) }}</div>
                  <div *ngIf="item.message" class="timeline-message">{{ item.message }}</div>
                </div>
              </div>
            </div>

            <!-- Empty History -->
            <div *ngIf="!historyLoading && !historyError && history.length === 0" class="empty-history">
              <span>📜</span>
              <p>No history records found</p>
            </div>
          </div>
        </div>

      </div>
    </div>
  `,
  styles: [`
    .order-detail-container {
      padding: 24px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .header {
      margin-bottom: 32px;
    }

    .back-link {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      margin-bottom: 16px;
      color: var(--primary-blue);
      text-decoration: none;
      font-weight: 500;
      transition: color 0.2s;
    }

    .back-link:hover {
      color: #4338ca;
      text-decoration: underline;
    }

    h2 {
      margin: 0;
      font-size: 32px;
      font-weight: 700;
      color: #1e293b;
    }

    /* Loading & Error States */
    .loading-state, .error-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 80px 20px;
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

    /* Cards */
    .order-content {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .card {
      background: white;
      border-radius: 12px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      overflow: hidden;
    }

    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 20px 24px;
      border-bottom: 1px solid #e2e8f0;
      background: #f8fafc;
    }

    .card-header h3 {
      margin: 0;
      font-size: 18px;
      font-weight: 600;
      color: #1e293b;
    }

    .card-body {
      padding: 24px;
    }

    /* Status Badge */
    .status-badge {
      display: inline-block;
      padding: 6px 14px;
      border-radius: 12px;
      font-size: 13px;
      font-weight: 600;
    }

    .status-badge.status-inprogress {
      background: #fef3c7;
      color: #d97706;
    }

    .status-badge.status-completed {
      background: #dcfce7;
      color: #16a34a;
    }

    .status-badge.status-failed {
      background: #fee2e2;
      color: #dc2626;
    }

    /* Info Grid */
    .info-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 24px;
    }

    .info-item {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .info-item label {
      font-size: 13px;
      font-weight: 500;
      color: #64748b;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .info-item .value {
      font-size: 15px;
      color: #1e293b;
      font-weight: 500;
    }

    .monospace {
      font-family: 'Courier New', monospace;
      font-size: 13px;
    }

    .small {
      font-size: 12px;
    }

    .product-id {
      color: var(--primary-blue);
      font-weight: 600;
    }

    /* Error & Info Boxes */
    .error-box, .info-box {
      margin-top: 24px;
      padding: 16px;
      border-radius: 8px;
      border-left: 4px solid;
    }

    .error-box {
      background: #fef2f2;
      border-color: #ef4444;
    }

    .error-box strong {
      color: #dc2626;
    }

    .error-box p {
      margin: 8px 0 0 0;
      color: #991b1b;
      font-size: 14px;
    }

    .info-box {
      background: #eff6ff;
      border-color: #3b82f6;
    }

    .info-box strong {
      color: #1e40af;
    }

    .info-box p {
      margin: 8px 0 0 0;
      color: #1e3a8a;
      font-size: 14px;
    }

    /* Action Buttons */
    .action-buttons {
      display: flex;
      gap: 12px;
      align-items: center;
    }

    .btn-primary, .btn-warning, .btn-danger, .btn-icon {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      padding: 12px 24px;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
      font-size: 14px;
    }

    .btn-primary {
      background: linear-gradient(90deg, #2563eb, #22c55e);
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background: #4338ca;
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(79, 70, 229, 0.3);
    }

    .btn-warning {
      background: #f59e0b;
      color: white;
    }

    .btn-warning:hover:not(:disabled) {
      background: #d97706;
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(245, 158, 11, 0.3);
    }

    .btn-danger {
      background: #ef4444;
      color: white;
    }

    .btn-danger:hover:not(:disabled) {
      background: #dc2626;
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(239, 68, 68, 0.3);
    }

    .btn-icon {
      padding: 8px 12px;
      background: transparent;
      color: #64748b;
      border: 1px solid #e2e8f0;
    }

    .btn-icon:hover:not(:disabled) {
      background: #f8fafc;
      border-color: #cbd5e1;
    }

    button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .no-actions {
      color: #64748b;
      font-style: italic;
      font-size: 14px;
    }

    /* Timeline */
    .timeline {
      position: relative;
      padding-left: 32px;
    }

    .timeline-item {
      position: relative;
      padding-bottom: 32px;
    }

    .timeline-item.last {
      padding-bottom: 0;
    }

    .timeline-item::before {
      content: '';
      position: absolute;
      left: -25px;
      top: 8px;
      bottom: -8px;
      width: 2px;
      background: #e2e8f0;
    }

    .timeline-item.last::before {
      display: none;
    }

    .timeline-marker {
      position: absolute;
      left: -31px;
      top: 0;
      width: 14px;
      height: 14px;
      border-radius: 50%;
      background: var(--primary-blue);
      border: 3px solid white;
      box-shadow: 0 0 0 2px #e2e8f0;
    }

    .timeline-content {
      background: #f8fafc;
      padding: 16px;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
    }

    .timeline-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 8px;
    }

    .step-name {
      font-weight: 600;
      color: #1e293b;
      font-size: 15px;
    }

    .timeline-status {
      padding: 3px 10px;
      border-radius: 10px;
      font-size: 12px;
      font-weight: 600;
    }

    .timeline-status.status-success,
    .timeline-status.status-completed {
      background: #dcfce7;
      color: #16a34a;
    }

    .timeline-status.status-failed,
    .timeline-status.status-error {
      background: #fee2e2;
      color: #dc2626;
    }

    .timeline-status.status-pending,
    .timeline-status.status-inprogress {
      background: #fef3c7;
      color: #d97706;
    }

    .timeline-time {
      font-size: 13px;
      color: #64748b;
      margin-bottom: 8px;
    }

    .timeline-message {
      font-size: 14px;
      color: #475569;
      margin-top: 8px;
      padding-top: 8px;
      border-top: 1px solid #e2e8f0;
    }

    /* History States */
    .history-loading, .history-error, .empty-history {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 40px 20px;
      gap: 12px;
    }

    .spinner-small {
      width: 20px;
      height: 20px;
      border: 3px solid #e2e8f0;
      border-top: 3px solid var(--primary-blue);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    .history-loading span {
      color: #64748b;
      font-size: 14px;
    }

    .history-error {
      color: #dc2626;
      font-size: 14px;
    }

    .empty-history {
      flex-direction: column;
      color: #64748b;
    }

    .empty-history span {
      font-size: 48px;
    }

    .empty-history p {
      margin: 8px 0 0 0;
      font-size: 14px;
    }

    @media (max-width: 768px) {
      .info-grid {
        grid-template-columns: 1fr;
      }

      .action-buttons {
        flex-direction: column;
        align-items: stretch;
      }

      .action-buttons button {
        width: 100%;
        justify-content: center;
      }
    }
  `]
})
export class AdminOrderDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private orderService = inject(OrderService);

  orderId: string | null = null;
  order: Order | null = null;
  history: OrderHistory[] = [];

  loading = false;
  error = '';
  historyLoading = false;
  historyError = '';
  actionLoading = false;

  ngOnInit(): void {
    this.orderId = this.route.snapshot.paramMap.get('id');
    if (this.orderId) {
      this.loadOrder();
      this.loadHistory();
    } else {
      this.error = 'Invalid order ID';
    }
  }

  loadOrder(): void {
    if (!this.orderId) return;

    this.loading = true;
    this.error = '';

    this.orderService.getOrderById(this.orderId)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (order) => {
          this.order = order;
        },
        error: (err) => {
          console.error('Error loading order:', err);
          this.error = 'Failed to load order details. Please try again.';
        }
      });
  }

  loadHistory(): void {
    if (!this.orderId) return;

    this.historyLoading = true;
    this.historyError = '';

    this.orderService.getOrderHistory(this.orderId)
      .pipe(finalize(() => this.historyLoading = false))
      .subscribe({
        next: (result) => {
          this.history = result.data;
        },
        error: (err) => {
          console.error('Error loading history:', err);
          this.historyError = 'Failed to load order history';
        }
      });
  }

  canCancel(): boolean {
    return this.order?.status === WorkflowStatus.InProgress;
  }

  canRetry(): boolean {
    return this.order?.status === WorkflowStatus.Failed;
  }

  retryOrder(): void {
    if (!this.order || !confirm(`Retry failed order for Product #${this.order.productId}?`)) {
      return;
    }

    this.actionLoading = true;
    this.orderService.retryOrder(this.order.id)
      .pipe(finalize(() => this.actionLoading = false))
      .subscribe({
        next: (updatedOrder) => {
          console.log('Order retry initiated:', updatedOrder);
          this.order = updatedOrder;
          this.loadHistory(); // Refresh history
        },
        error: (err) => {
          console.error('Failed to retry order:', err);
          alert('Failed to retry order. Please try again.');
        }
      });
  }

  cancelOrder(): void {
    if (!this.order || !confirm(`Are you sure you want to cancel the order for Product #${this.order.productId}?`)) {
      return;
    }

    this.actionLoading = true;
    this.orderService.cancelOrder(this.order.id)
      .pipe(finalize(() => this.actionLoading = false))
      .subscribe({
        next: () => {
          console.log('Order cancelled successfully');
          this.loadOrder(); // Refresh order
          this.loadHistory(); // Refresh history
        },
        error: (err) => {
          console.error('Failed to cancel order:', err);
          alert('Failed to cancel order. Please try again.');
        }
      });
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    });
  }

  formatStep(step: WorkflowStep): string {
    return step.replace(/([A-Z])/g, ' $1').trim();
  }
}
