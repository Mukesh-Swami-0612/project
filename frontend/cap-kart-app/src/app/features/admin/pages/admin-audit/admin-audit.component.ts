import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuditService, AuditLogDto, AuditLogFilterDto } from '../../../../core/services/audit.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-admin-audit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="audit-page">
      <div class="page-header">
        <div>
          <h1>Audit Logs</h1>
          <p>System-wide activity tracking and security audit trail</p>
        </div>
        <button class="btn-export" (click)="exportLogs()" [disabled]="exporting">
          <span>{{ exporting ? '⏳' : '📥' }}</span>
          {{ exporting ? 'Exporting...' : 'Export to Excel' }}
        </button>
      </div>

      <!-- Filters -->
      <div class="filters-section">
        <div class="search-box">
          <span class="search-icon">🔍</span>
          <input
            type="text"
            placeholder="Filter by email..."
            [(ngModel)]="filter.email"
            (keyup.enter)="applyFilters()"
          />
        </div>

        <input
          type="text"
          class="filter-input"
          placeholder="Filter by action..."
          [(ngModel)]="filter.action"
          (keyup.enter)="applyFilters()"
        />

        <input
          type="date"
          class="filter-input"
          [(ngModel)]="filter.fromDate"
          title="From date"
        />

        <input
          type="date"
          class="filter-input"
          [(ngModel)]="filter.toDate"
          title="To date"
        />

        <button class="btn-primary" (click)="applyFilters()">
          <span>🔍</span> Search
        </button>
        <button class="btn-secondary" (click)="resetFilters()">
          <span>🔄</span> Reset
        </button>
      </div>

      <!-- Loading -->
      <div class="loading-state" *ngIf="loading">
        <div class="spinner"></div>
        <p>Loading audit logs...</p>
      </div>

      <!-- Error -->
      <div class="error-state" *ngIf="errorMessage && !loading">
        <span class="error-icon">⚠️</span>
        <p>{{ errorMessage }}</p>
        <button class="btn-secondary" (click)="loadLogs()">Retry</button>
      </div>

      <!-- Results -->
      <div class="table-container" *ngIf="!loading && !errorMessage">
        <div class="table-header">
          <span class="record-count">{{ total }} records found</span>
        </div>

        <table class="data-table">
          <thead>
            <tr>
              <th>Timestamp</th>
              <th>User Email</th>
              <th>Action</th>
              <th>Details</th>
              <th>IP Address</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let log of logs">
              <td>{{ formatDate(log.timestamp) }}</td>
              <td>{{ log.userEmail }}</td>
              <td>
                <span class="action-tag">{{ log.action }}</span>
              </td>
              <td class="details-cell">{{ log.details || '—' }}</td>
              <td>{{ log.ipAddress || '—' }}</td>
              <td>
                <span class="status-badge" [class]="log.success ? 'status-success' : 'status-failed'">
                  {{ log.success ? 'Success' : 'Failed' }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>

        <div class="empty-state" *ngIf="logs.length === 0">
          <span class="empty-icon">📋</span>
          <h3>No audit logs found</h3>
          <p>Try adjusting your filters</p>
        </div>
      </div>

      <!-- Pagination -->
      <div class="pagination" *ngIf="!loading && logs.length > 0">
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
    .audit-page {
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

    .btn-export {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px 24px;
      background: #10b981;
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      font-size: 14px;
      transition: all 0.2s;
    }

    .btn-export:hover:not(:disabled) {
      background: #059669;
    }

    .btn-export:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .filters-section {
      display: flex;
      gap: 12px;
      margin-bottom: 24px;
      flex-wrap: wrap;
      align-items: center;
    }

    .search-box {
      flex: 1;
      min-width: 240px;
      display: flex;
      align-items: center;
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 10px 16px;
    }

    .search-icon {
      margin-right: 8px;
      color: #94a3b8;
    }

    .search-box input {
      border: none;
      outline: none;
      flex: 1;
      font-size: 14px;
    }

    .filter-input {
      padding: 10px 16px;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      font-size: 14px;
      background: white;
      color: #1e293b;
    }

    .btn-primary {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 20px;
      background: linear-gradient(90deg, #2563eb, #22c55e);
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      font-size: 14px;
      transition: all 0.2s;
      white-space: nowrap;
    }

    .btn-primary:hover {
      background: #4338ca;
    }

    .btn-secondary {
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
      white-space: nowrap;
    }

    .btn-secondary:hover {
      background: #f8fafc;
    }

    .loading-state, .error-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 80px 20px;
      gap: 16px;
    }

    .spinner {
      width: 40px;
      height: 40px;
      border: 4px solid #e2e8f0;
      border-top: 4px solid var(--primary-blue);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .error-icon {
      font-size: 48px;
    }

    .error-state p {
      color: #ef4444;
      font-weight: 500;
    }

    .table-container {
      background: white;
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      margin-bottom: 24px;
    }

    .table-header {
      padding: 16px 20px;
      border-bottom: 1px solid #f1f5f9;
      background: #f8fafc;
    }

    .record-count {
      font-size: 13px;
      color: #64748b;
      font-weight: 500;
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
      padding: 14px 16px;
      text-align: left;
      font-size: 12px;
      font-weight: 600;
      color: #475569;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .data-table td {
      padding: 14px 16px;
      border-bottom: 1px solid #f1f5f9;
      font-size: 14px;
      color: #1e293b;
    }

    .data-table tbody tr:hover {
      background: #f8fafc;
    }

    .action-tag {
      display: inline-block;
      padding: 3px 10px;
      background: #ede9fe;
      color: #6d28d9;
      border-radius: 6px;
      font-size: 12px;
      font-weight: 500;
    }

    .details-cell {
      max-width: 300px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      color: #64748b;
    }

    .status-badge {
      display: inline-block;
      padding: 4px 10px;
      border-radius: 10px;
      font-size: 12px;
      font-weight: 600;
    }

    .status-badge.status-success {
      background: #dcfce7;
      color: #16a34a;
    }

    .status-badge.status-failed {
      background: #fee2e2;
      color: #dc2626;
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
      .filters-section {
        flex-direction: column;
      }

      .table-container {
        overflow-x: auto;
      }
    }
  `]
})
export class AdminAuditComponent implements OnInit {
  private auditService = inject(AuditService);

  logs: AuditLogDto[] = [];
  loading = false;
  exporting = false;
  errorMessage = '';
  total = 0;
  currentPage = 1;
  pageSize = 20;

  filter: AuditLogFilterDto = {
    email: '',
    action: '',
    fromDate: '',
    toDate: '',
    pageNumber: 1,
    pageSize: 20
  };

  get totalPages(): number {
    return Math.ceil(this.total / this.pageSize) || 1;
  }

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.loading = true;
    this.errorMessage = '';

    const activeFilter: AuditLogFilterDto = {
      ...this.filter,
      pageNumber: this.currentPage,
      pageSize: this.pageSize
    };

    // Remove empty strings
    if (!activeFilter.email) delete activeFilter.email;
    if (!activeFilter.action) delete activeFilter.action;
    if (!activeFilter.fromDate) delete activeFilter.fromDate;
    if (!activeFilter.toDate) delete activeFilter.toDate;

    this.auditService.getLogs(activeFilter)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (res) => {
          this.logs = res.data || [];
          this.total = res.total || 0;
        },
        error: (err) => {
          this.errorMessage = 'Failed to load audit logs. Please try again.';
          console.error('Audit logs error:', err);
        }
      });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadLogs();
  }

  resetFilters(): void {
    this.filter = { email: '', action: '', fromDate: '', toDate: '' };
    this.currentPage = 1;
    this.loadLogs();
  }

  exportLogs(): void {
    this.exporting = true;
    const activeFilter: AuditLogFilterDto = { ...this.filter };
    if (!activeFilter.email) delete activeFilter.email;
    if (!activeFilter.action) delete activeFilter.action;
    if (!activeFilter.fromDate) delete activeFilter.fromDate;
    if (!activeFilter.toDate) delete activeFilter.toDate;

    this.auditService.exportLogs(activeFilter).subscribe({
      next: (blob) => {
        this.exporting = false;
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `AuditLogs_${new Date().toISOString().slice(0, 10)}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.exporting = false;
        console.error('Export failed:', err);
        alert('Export failed. Please try again.');
      }
    });
  }

  formatDate(timestamp: string): string {
    return new Date(timestamp).toLocaleString('en-US', {
      year: 'numeric', month: 'short', day: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.loadLogs();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.loadLogs();
    }
  }
}

