import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { OrderService } from '../../../../core/services/order.service';
import { Order, WorkflowStatus, WorkflowStep } from '../../../../core/models/order.model';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="orders-page">
      <div class="page-header">
        <div>
          <h1>Order Management</h1>
          <p>Manage workflow orders and track their progress</p>
        </div>
        <button class="btn-secondary" (click)="refreshOrders()">
          <span>🔄</span> Refresh
        </button>
      </div>

      <!-- Stats -->
      <div class="stats-row">
        <div class="stat-item">
          <span class="stat-label">Total Orders</span>
          <span class="stat-value">{{ orders.length }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">In Progress</span>
          <span class="stat-value text-warning">{{ getInProgressCount() }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">Completed</span>
          <span class="stat-value text-success">{{ getCompletedCount() }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">Failed</span>
          <span class="stat-value text-danger">{{ getFailedCount() }}</span>
        </div>
      </div>

      <!-- Loading State -->
      <div class="loading-state" *ngIf="loading">
        <div class="spinner"></div>
        <p>Loading orders...</p>
      </div>

      <!-- Error State -->
      <div class="error-state" *ngIf="errorMessage && !loading">
        <span class="error-icon">⚠️</span>
        <h3>Failed to load orders</h3>
        <p>{{ errorMessage }}</p>
        <button class="btn-primary" (click)="loadOrders()">
          <span>🔄</span> Try Again
        </button>
      </div>

      <!-- Table -->
      <div class="table-container" *ngIf="!loading && !errorMessage">
        <table class="data-table">
          <thead>
            <tr>
              <th>Order ID</th>
              <th>Product ID</th>
              <th>Status</th>
              <th>Current Step</th>
              <th>Created At</th>
              <th>Updated At</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let order of paginatedOrders">
              <td>
                <div class="order-id">
                  <span class="id-text">{{ formatOrderId(order.id) }}</span>
                </div>
              </td>
              <td>
                <span class="product-id">#{{ order.productId }}</span>
              </td>
              <td>
                <span class="status-badge" [class]="'status-' + order.status.toLowerCase()">
                  {{ order.status }}
                </span>
              </td>
              <td>
                <span class="step-badge">{{ formatStep(order.currentStep) }}</span>
              </td>
              <td>{{ formatDate(order.createdAt) }}</td>
              <td>{{ formatDate(order.updatedAt) }}</td>
              <td>
                <div class="action-buttons">
                  <button 
                    class="btn-icon" 
                    title="View Details"
                    (click)="viewOrder(order.id)"
                  >
                    👁️
                  </button>
                  <button 
                    class="btn-icon" 
                    title="View History"
                    (click)="viewHistory(order.id)"
                  >
                    📜
                  </button>
                  <button 
                    *ngIf="canRetry(order)"
                    class="btn-icon text-warning" 
                    title="Retry"
                    (click)="retryOrder(order)"
                  >
                    🔄
                  </button>
                  <button 
                    *ngIf="canCancel(order)"
                    class="btn-icon text-danger" 
                    title="Cancel"
                    (click)="cancelOrder(order)"
                  >
                    ❌
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>

        <div class="empty-state" *ngIf="orders.length === 0">
          <span class="empty-icon">📦</span>
          <h3>No orders found</h3>
          <p>There are no workflow orders in the system yet</p>
        </div>
      </div>

      <!-- Pagination -->
      <div class="pagination" *ngIf="!loading && !errorMessage && orders.length > 0">
        <button class="page-btn" [disabled]="currentPage === 1" (click)="previousPage()">
          Previous
        </button>
        <span class="page-info">Page {{ currentPage }} of {{ totalPages }}</span>
        <button class="page-btn" [disabled]="currentPage === totalPages" (click)="nextPage()">
          Next
        </button>
      </div>
    </div>
  `,
  styles: [`
    .orders-page {
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

    .btn-primary, .btn-secondary {
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
    }

    .btn-primary {
      background: linear-gradient(90deg, #2563eb, #22c55e);
      color: white;
    }

    .btn-primary:hover {
      background: #4338ca;
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(79, 70, 229, 0.3);
    }

    .btn-secondary {
      background: white;
      color: #475569;
      border: 1px solid #e2e8f0;
    }

    .btn-secondary:hover {
      background: #f8fafc;
      border-color: #cbd5e1;
    }

    .stats-row {
      display: flex;
      gap: 24px;
      margin-bottom: 24px;
    }

    .stat-item {
      background: white;
      padding: 20px;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .stat-label {
      font-size: 13px;
      color: #64748b;
    }

    .stat-value {
      font-size: 28px;
      font-weight: 700;
      color: #1e293b;
    }

    .text-success {
      color: #10b981;
    }

    .text-warning {
      color: #f59e0b;
    }

    .text-danger {
      color: #ef4444;
    }

    .loading-state, .error-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 60px 20px;
      background: white;
      border-radius: 12px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }

    .spinner {
      width: 40px;
      height: 40px;
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

    .loading-state p, .error-state p {
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

    .error-state .btn-primary {
      margin-top: 8px;
    }

    .table-container {
      background: white;
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      margin-bottom: 24px;
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;
    }

    .data-table thead {
      background: #f8fafc;
      border-bottom: 2px solid #e2e8f0;
    }

    .data-table th {
      padding: 16px;
      text-align: left;
      font-size: 13px;
      font-weight: 600;
      color: #475569;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .data-table td {
      padding: 16px;
      border-bottom: 1px solid #f1f5f9;
      font-size: 14px;
      color: #1e293b;
    }

    .data-table tbody tr:hover {
      background: #f8fafc;
    }

    .order-id {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .id-text {
      font-family: monospace;
      font-size: 12px;
      color: #64748b;
    }

    .product-id {
      font-weight: 600;
      color: var(--primary-blue);
    }

    .status-badge {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 12px;
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

    .step-badge {
      display: inline-block;
      padding: 4px 10px;
      background: #f1f5f9;
      border-radius: 6px;
      font-size: 12px;
      color: #475569;
    }

    .action-buttons {
      display: flex;
      gap: 8px;
    }

    .btn-icon {
      background: transparent;
      border: none;
      font-size: 16px;
      cursor: pointer;
      padding: 6px;
      border-radius: 4px;
      transition: all 0.2s;
    }

    .btn-icon:hover {
      background: #f1f5f9;
    }

    .empty-state {
      text-align: center;
      padding: 60px 20px;
    }

    .empty-icon {
      font-size: 64px;
      display: block;
      margin-bottom: 16px;
    }

    .empty-state h3 {
      font-size: 20px;
      color: #1e293b;
      margin: 0 0 8px 0;
    }

    .empty-state p {
      color: #64748b;
      margin: 0;
    }

    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 16px;
    }

    .page-btn {
      padding: 8px 16px;
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      cursor: pointer;
      font-size: 14px;
      transition: all 0.2s;
    }

    .page-btn:hover:not(:disabled) {
      background: #f8fafc;
      border-color: #cbd5e1;
    }

    .page-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .page-info {
      font-size: 14px;
      color: #64748b;
    }

    @media (max-width: 768px) {
      .stats-row {
        flex-direction: column;
      }

      .table-container {
        overflow-x: auto;
      }
    }
  `]
})
export class AdminOrdersComponent implements OnInit {
  private orderService = inject(OrderService);
  private router = inject(Router);

  orders: Order[] = [];
  paginatedOrders: Order[] = [];
  loading = false;
  errorMessage = '';
  
  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalPages = 1;

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.loading = true;
    this.errorMessage = '';

    this.orderService.getAllOrders({ page: 1, pageSize: 100 })
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (result) => {
          this.orders = result.data;
          this.updatePagination();
        },
        error: (err) => {
          console.error('Error loading orders:', err);
          this.errorMessage = 'Failed to load orders. Please try again.';
        }
      });
  }

  updatePagination(): void {
    this.totalPages = Math.ceil(this.orders.length / this.pageSize) || 1;
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    this.paginatedOrders = this.orders.slice(startIndex, endIndex);
  }

  refreshOrders(): void {
    this.currentPage = 1;
    this.loadOrders();
  }

  // Pagination
  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.updatePagination();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.updatePagination();
    }
  }

  // Stats
  getInProgressCount(): number {
    return this.orders.filter(o => o.status === WorkflowStatus.InProgress).length;
  }

  getCompletedCount(): number {
    return this.orders.filter(o => o.status === WorkflowStatus.Completed).length;
  }

  getFailedCount(): number {
    return this.orders.filter(o => o.status === WorkflowStatus.Failed).length;
  }

  // Formatting
  formatOrderId(id: string): string {
    return id.substring(0, 8) + '...';
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-US', { 
      year: 'numeric', 
      month: 'short', 
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatStep(step: WorkflowStep): string {
    // Convert enum to readable format
    return step.replace(/([A-Z])/g, ' $1').trim();
  }

  // Actions
  canCancel(order: Order): boolean {
    return order.status === WorkflowStatus.InProgress;
  }

  canRetry(order: Order): boolean {
    return order.status === WorkflowStatus.Failed;
  }

  viewOrder(orderId: string): void {
    this.router.navigate(['/admin/orders', orderId]);
  }

  viewHistory(orderId: string): void {
    // View order history
    console.log('View history:', orderId);
    alert('Order history view coming soon!');
  }

  retryOrder(order: Order): void {
    if (confirm(`Retry failed order for Product #${order.productId}?`)) {
      this.orderService.retryOrder(order.id).subscribe({
        next: (updatedOrder) => {
          console.log('Order retry initiated:', updatedOrder);
          this.loadOrders(); // Refresh list
        },
        error: (err) => {
          console.error('Failed to retry order:', err);
          alert('Failed to retry order. Please try again.');
        }
      });
    }
  }

  cancelOrder(order: Order): void {
    if (confirm(`Are you sure you want to cancel the order for Product #${order.productId}?`)) {
      this.orderService.cancelOrder(order.id).subscribe({
        next: () => {
          console.log('Order cancelled successfully');
          this.loadOrders(); // Refresh list
        },
        error: (err) => {
          console.error('Failed to cancel order:', err);
          alert('Failed to cancel order. Please try again.');
        }
      });
    }
  }
}
