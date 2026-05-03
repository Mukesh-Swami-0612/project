import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
  type?: 'text' | 'number' | 'date' | 'badge' | 'actions';
}

export interface TableAction {
  label: string;
  icon?: string;
  action: string;
  variant?: 'primary' | 'secondary' | 'danger';
  condition?: (item: any) => boolean;
}

export interface SortConfig {
  column: string;
  direction: 'asc' | 'desc';
}

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="data-table-container">
      <!-- Table Header -->
      <div class="table-header" *ngIf="showSearch || showFilters">
        <div class="search-section" *ngIf="showSearch">
          <div class="search-input-group">
            <input
              type="text"
              placeholder="Search..."
              [(ngModel)]="searchTerm"
              (input)="onSearch()"
              class="search-input">
            <span class="search-icon">🔍</span>
          </div>
        </div>
        
        <div class="filter-section" *ngIf="showFilters">
          <ng-content select="[slot=filters]"></ng-content>
        </div>
      </div>

      <!-- Table -->
      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th *ngFor="let column of columns" 
                  [style.width]="column.width"
                  [class.sortable]="column.sortable"
                  (click)="onSort(column)">
                <div class="th-content">
                  <span>{{ column.label }}</span>
                  <span *ngIf="column.sortable && sortConfig?.column === column.key" 
                        class="sort-indicator">
                    {{ sortConfig?.direction === 'asc' ? '↑' : '↓' }}
                  </span>
                </div>
              </th>
            </tr>
          </thead>
          <tbody>
            <tr *ngIf="loading" class="loading-row">
              <td [attr.colspan]="columns.length" class="loading-cell">
                <div class="loading-spinner"></div>
                <span>Loading...</span>
              </td>
            </tr>
            
            <tr *ngIf="!loading && data.length === 0" class="empty-row">
              <td [attr.colspan]="columns.length" class="empty-cell">
                <div class="empty-state">
                  <span class="empty-icon">📦</span>
                  <p>{{ emptyMessage || 'No data available' }}</p>
                </div>
              </td>
            </tr>

            <tr *ngFor="let item of data; trackBy: trackByFn" 
                class="data-row"
                [class.selected]="selectedItems.has(getItemId(item))">
              <td *ngFor="let column of columns" [class]="'cell-' + column.type">
                <ng-container [ngSwitch]="column.type">
                  <!-- Text/Default -->
                  <span *ngSwitchDefault>{{ getColumnValue(item, column.key) }}</span>
                  
                  <!-- Number -->
                  <span *ngSwitchCase="'number'" class="number-cell">
                    {{ getColumnValue(item, column.key) | number }}
                  </span>
                  
                  <!-- Date -->
                  <span *ngSwitchCase="'date'" class="date-cell">
                    {{ getColumnValue(item, column.key) | date:'short' }}
                  </span>
                  
                  <!-- Badge -->
                  <ng-container *ngSwitchCase="'badge'">
                    <ng-content select="[slot=badge]" 
                                [ngTemplateOutlet]="badgeTemplate" 
                                [ngTemplateOutletContext]="{ item: item, value: getColumnValue(item, column.key) }">
                    </ng-content>
                  </ng-container>
                  
                  <!-- Actions -->
                  <div *ngSwitchCase="'actions'" class="actions-cell">
                    <button *ngFor="let action of getAvailableActions(item)"
                            class="action-btn"
                            [class]="'btn-' + (action.variant || 'secondary')"
                            (click)="onAction(action.action, item)"
                            [title]="action.label">
                      <span *ngIf="action.icon">{{ action.icon }}</span>
                      <span>{{ action.label }}</span>
                    </button>
                  </div>
                </ng-container>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div class="table-footer" *ngIf="showPagination">
        <div class="pagination-info">
          Showing {{ (currentPage - 1) * pageSize + 1 }} to 
          {{ getMaxItems() }} of {{ totalItems }} entries
        </div>
        
        <div class="pagination-controls">
          <button class="pagination-btn" 
                  [disabled]="currentPage === 1"
                  (click)="onPageChange(currentPage - 1)">
            Previous
          </button>
          
          <span class="page-numbers">
            <button *ngFor="let page of getVisiblePages()" 
                    class="page-btn"
                    [class.active]="page === currentPage"
                    (click)="onPageChange(page)">
              {{ page }}
            </button>
          </span>
          
          <button class="pagination-btn" 
                  [disabled]="currentPage === totalPages"
                  (click)="onPageChange(currentPage + 1)">
            Next
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .data-table-container {
      background: var(--surface-container-lowest);
      border-radius: var(--radius-lg);
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
      overflow: hidden;
    }

    .table-header {
      padding: var(--spacing-lg);
      border-bottom: 1px solid var(--outline-variant);
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: var(--spacing-md);
    }

    .search-input-group {
      position: relative;
      min-width: 300px;
    }

    .search-input {
      width: 100%;
      padding: 10px 40px 10px 16px;
      border: 1px solid var(--outline-variant);
      border-radius: var(--radius-md);
      font-size: 14px;
      background: var(--surface-container-low);
    }

    .search-input:focus {
      outline: none;
      border-color: var(--primary);
      box-shadow: 0 0 0 2px rgba(0, 56, 123, 0.1);
    }

    .search-icon {
      position: absolute;
      right: 12px;
      top: 50%;
      transform: translateY(-50%);
      color: var(--on-surface-variant);
    }

    .table-wrapper {
      overflow-x: auto;
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;
    }

    .data-table th {
      background: var(--surface-container);
      padding: 16px;
      text-align: left;
      font-weight: 600;
      color: var(--on-surface-variant);
      border-bottom: 1px solid var(--outline-variant);
      white-space: nowrap;
    }

    .data-table th.sortable {
      cursor: pointer;
      user-select: none;
    }

    .data-table th.sortable:hover {
      background: var(--surface-container-high);
    }

    .th-content {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .sort-indicator {
      font-size: 12px;
      color: var(--primary);
    }

    .data-table td {
      padding: 16px;
      border-bottom: 1px solid var(--outline-variant);
      vertical-align: middle;
    }

    .data-row:hover {
      background: var(--surface-container-low);
    }

    .data-row.selected {
      background: var(--primary-container);
    }

    .loading-cell, .empty-cell {
      text-align: center;
      padding: 48px 16px;
      color: var(--on-surface-variant);
    }

    .loading-spinner {
      width: 20px;
      height: 20px;
      border: 2px solid var(--outline-variant);
      border-top: 2px solid var(--primary);
      border-radius: 50%;
      animation: spin 1s linear infinite;
      display: inline-block;
      margin-right: 8px;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
    }

    .empty-icon {
      font-size: 48px;
      opacity: 0.5;
    }

    .actions-cell {
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .action-btn {
      padding: 6px 12px;
      border: 1px solid var(--outline-variant);
      border-radius: var(--radius-sm);
      background: var(--surface-container-lowest);
      color: var(--on-surface);
      font-size: 12px;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 4px;
      transition: all 0.2s ease;
    }

    .action-btn:hover {
      background: var(--surface-container);
    }

    .action-btn.btn-primary {
      background: var(--primary);
      color: var(--on-primary);
      border-color: var(--primary);
    }

    .action-btn.btn-danger {
      background: var(--error);
      color: var(--on-error);
      border-color: var(--error);
    }

    .table-footer {
      padding: var(--spacing-lg);
      border-top: 1px solid var(--outline-variant);
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .pagination-info {
      color: var(--on-surface-variant);
      font-size: 14px;
    }

    .pagination-controls {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .pagination-btn, .page-btn {
      padding: 8px 12px;
      border: 1px solid var(--outline-variant);
      border-radius: var(--radius-sm);
      background: var(--surface-container-lowest);
      color: var(--on-surface);
      cursor: pointer;
      font-size: 14px;
    }

    .pagination-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .pagination-btn:not(:disabled):hover,
    .page-btn:hover {
      background: var(--surface-container);
    }

    .page-btn.active {
      background: var(--primary);
      color: var(--on-primary);
      border-color: var(--primary);
    }

    .page-numbers {
      display: flex;
      gap: 4px;
    }

    .number-cell {
      text-align: right;
      font-variant-numeric: tabular-nums;
    }

    .date-cell {
      font-variant-numeric: tabular-nums;
    }
  `]
})
export class DataTableComponent implements OnInit {
  @Input() data: any[] = [];
  @Input() columns: TableColumn[] = [];
  @Input() actions: TableAction[] = [];
  @Input() loading = false;
  @Input() showSearch = true;
  @Input() showFilters = false;
  @Input() showPagination = true;
  @Input() emptyMessage = '';
  @Input() currentPage = 1;
  @Input() pageSize = 10;
  @Input() totalItems = 0;
  @Input() sortConfig: SortConfig | null = null;

  @Output() search = new EventEmitter<string>();
  @Output() sort = new EventEmitter<SortConfig>();
  @Output() pageChange = new EventEmitter<number>();
  @Output() actionClick = new EventEmitter<{action: string, item: any}>();

  searchTerm = '';
  selectedItems = new Set<any>();

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize);
  }

  ngOnInit() {
    // Initialize component
  }

  trackByFn(index: number, item: any): any {
    return item.id || index;
  }

  getItemId(item: any): any {
    return item.id || item;
  }

  getColumnValue(item: any, key: string): any {
    return key.split('.').reduce((obj, prop) => obj?.[prop], item);
  }

  getAvailableActions(item: any): TableAction[] {
    return this.actions.filter(action => 
      !action.condition || action.condition(item)
    );
  }

  onSearch(): void {
    this.search.emit(this.searchTerm);
  }

  onSort(column: TableColumn): void {
    if (!column.sortable) return;
    
    const direction = this.sortConfig?.column === column.key && this.sortConfig.direction === 'asc' 
      ? 'desc' 
      : 'asc';
    
    this.sort.emit({ column: column.key, direction });
  }

  onPageChange(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.pageChange.emit(page);
    }
  }

  onAction(action: string, item: any): void {
    this.actionClick.emit({ action, item });
  }

  getVisiblePages(): number[] {
    const pages: number[] = [];
    const maxVisible = 5;
    const half = Math.floor(maxVisible / 2);
    
    let start = Math.max(1, this.currentPage - half);
    let end = Math.min(this.totalPages, start + maxVisible - 1);
    
    if (end - start + 1 < maxVisible) {
      start = Math.max(1, end - maxVisible + 1);
    }
    
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    
    return pages;
  }

  getMaxItems(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalItems);
  }
}