import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../../core/services/product.service';
import { Product } from '../../../../core/models/product.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { DataTableComponent, TableColumn, TableAction } from '../../../../shared/components/data-table/data-table.component';
import { FilterPanelComponent, FilterDefinition } from '../../../../shared/components/filter-panel/filter-panel.component';
import { ModalComponent } from '../../../../shared/components/modal/modal.component';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    FormsModule,
    StatusBadgeComponent,
    DataTableComponent,
    FilterPanelComponent,
    ModalComponent
  ],
  template: `
    <div class="admin-products-container">
      <!-- Page Header -->
      <div class="page-header">
        <div class="header-content">
          <h1>Product Management</h1>
          <p class="subtitle">Manage your product catalog, inventory, and lifecycle</p>
        </div>
        <div class="header-actions">
          <button class="btn-secondary" (click)="exportProducts()">
            <span class="icon">📥</span>
            Export
          </button>
          <button class="btn-primary" routerLink="/admin/products/new">
            <span class="icon">➕</span>
            Create Product
          </button>
        </div>
      </div>

      <!-- Filters -->
      <div class="filters-section">
        <app-filter-panel
          [filters]="filterDefinitions"
          [initialValues]="filterValues"
          (apply)="onFilterApply($event)">
        </app-filter-panel>
      </div>

      <!-- Products Table -->
      <div class="table-section">
        <app-data-table
          [data]="products"
          [columns]="tableColumns"
          [actions]="tableActions"
          [loading]="loading"
          [showSearch]="true"
          [showPagination]="true"
          [currentPage]="currentPage"
          [pageSize]="pageSize"
          [totalItems]="totalItems"
          [sortConfig]="sortConfig"
          [emptyMessage]="'No products found. Create your first product to get started.'"
          (search)="onSearch($event)"
          (sort)="onSort($event)"
          (pageChange)="onPageChange($event)"
          (actionClick)="onActionClick($event)">
          
          <!-- Custom badge template -->
          <ng-template #badgeTemplate let-item="item" let-value="value">
            <app-status-badge [status]="value"></app-status-badge>
          </ng-template>
        </app-data-table>
      </div>

      <!-- Delete Confirmation Modal -->
      <app-modal
        [isOpen]="showDeleteModal"
        title="Confirm Delete"
        size="sm"
        [showFooter]="true"
        (closed)="onDeleteCancel()">
        <div class="modal-content">
          <p>Are you sure you want to delete this product?</p>
          <p class="warning-text">This action cannot be undone.</p>
        </div>
        
        <div slot="footer" class="modal-actions">
          <button class="btn-secondary" (click)="onDeleteCancel()">Cancel</button>
          <button class="btn-danger" (click)="onDeleteConfirm()">Delete</button>
        </div>
      </app-modal>
    </div>
  `,
  styles: [`
    .admin-products-container {
      padding: var(--spacing-xl);
      max-width: 1600px;
      margin: 0 auto;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: var(--spacing-xl);
    }

    .header-content h1 {
      font-size: 32px;
      font-weight: 700;
      color: var(--on-surface);
      margin: 0 0 8px 0;
    }

    .subtitle {
      color: var(--on-surface-variant);
      font-size: 16px;
      margin: 0;
    }

    .header-actions {
      display: flex;
      gap: var(--spacing-md);
    }

    .btn-primary, .btn-secondary, .btn-danger {
      padding: 12px 24px;
      border: none;
      border-radius: var(--radius-md);
      font-size: 14px;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 8px;
      transition: all 0.2s ease;
    }

    .btn-primary {
      background: var(--primary);
      color: var(--on-primary);
    }

    .btn-primary:hover {
      background: var(--primary-container);
      transform: translateY(-1px);
      box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    }

    .btn-secondary {
      background: var(--surface-container);
      color: var(--on-surface);
      border: 1px solid var(--outline-variant);
    }

    .btn-secondary:hover {
      background: var(--surface-container-high);
    }

    .btn-danger {
      background: var(--error);
      color: var(--on-error);
    }

    .btn-danger:hover {
      background: #a31515;
    }

    .icon {
      font-size: 18px;
    }

    .filters-section {
      margin-bottom: var(--spacing-lg);
    }

    .table-section {
      background: var(--surface-container-lowest);
      border-radius: var(--radius-lg);
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    .modal-content {
      padding: var(--spacing-md) 0;
    }

    .modal-content p {
      margin: 0 0 var(--spacing-sm) 0;
      color: var(--on-surface);
    }

    .warning-text {
      color: var(--error);
      font-weight: 500;
    }

    .modal-actions {
      display: flex;
      gap: var(--spacing-md);
      justify-content: flex-end;
    }

    @media (max-width: 768px) {
      .page-header {
        flex-direction: column;
        gap: var(--spacing-lg);
      }

      .header-actions {
        width: 100%;
        flex-direction: column;
      }

      .btn-primary, .btn-secondary {
        width: 100%;
        justify-content: center;
      }
    }
  `]
})
export class AdminProductsComponent implements OnInit {
  private productService = inject(ProductService);
  
  products: Product[] = [];
  loading = false;
  error: string | null = null;
  
  // Pagination
  currentPage = 1;
  pageSize = 20;
  totalItems = 0;

  // Sorting
  sortConfig: { column: string; direction: 'asc' | 'desc' } | null = {
    column: 'createdAt',
    direction: 'desc'
  };

  // Filtering
  filterValues: Record<string, any> = {};
  searchTerm = '';

  // Modal
  showDeleteModal = false;
  productToDelete: number | null = null;

  // Table Configuration
  tableColumns: TableColumn[] = [
    { key: 'id', label: 'ID', sortable: true, width: '80px', type: 'number' },
    { key: 'name', label: 'Product Name', sortable: true, type: 'text' },
    { key: 'sku', label: 'SKU', sortable: true, type: 'text' },
    { key: 'categoryName', label: 'Category', sortable: false, type: 'text' },
    { key: 'status', label: 'Status', sortable: true, type: 'badge' },
    { key: 'createdAt', label: 'Created', sortable: true, type: 'date' },
    { key: 'actions', label: 'Actions', sortable: false, type: 'actions', width: '200px' }
  ];

  tableActions: TableAction[] = [
    {
      label: 'View',
      icon: '👁️',
      action: 'view',
      variant: 'secondary'
    },
    {
      label: 'Edit',
      icon: '✏️',
      action: 'edit',
      variant: 'primary'
    },
    {
      label: 'Delete',
      icon: '🗑️',
      action: 'delete',
      variant: 'danger',
      condition: (item: any) => item.status === 'Draft' || item.status === 'Rejected'
    }
  ];

  filterDefinitions: FilterDefinition[] = [
    {
      key: 'status',
      label: 'Status',
      type: 'select',
      options: [
        { label: 'Draft', value: 'Draft' },
        { label: 'In Enrichment', value: 'InEnrichment' },
        { label: 'Ready for Review', value: 'ReadyForReview' },
        { label: 'Approved', value: 'Approved' },
        { label: 'Published', value: 'Published' },
        { label: 'Rejected', value: 'Rejected' },
        { label: 'Archived', value: 'Archived' }
      ],
      placeholder: 'All Statuses'
    },
    {
      key: 'category',
      label: 'Category',
      type: 'select',
      options: [
        { label: 'Electronics', value: 1 },
        { label: 'Clothing', value: 2 },
        { label: 'Home & Garden', value: 3 }
      ],
      placeholder: 'All Categories'
    },
    {
      key: 'price',
      label: 'Price Range',
      type: 'range'
    }
  ];

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    this.loading = true;
    this.error = null;
    
    this.productService.getProducts(this.searchTerm, this.currentPage, this.pageSize)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (result) => {
          this.products = result.items;
          this.totalItems = result.totalCount;
        },
        error: (err) => {
          console.error('Failed to load products', err);
          this.error = 'Could not load products. Please try again later.';
        }
      });
  }

  onSearch(searchTerm: string) {
    this.searchTerm = searchTerm;
    this.currentPage = 1;
    this.loadProducts();
  }

  onSort(sortConfig: { column: string; direction: 'asc' | 'desc' }) {
    this.sortConfig = sortConfig;
    this.loadProducts();
  }

  onPageChange(page: number) {
    this.currentPage = page;
    this.loadProducts();
  }

  onFilterApply(filters: Record<string, any>) {
    this.filterValues = filters;
    this.currentPage = 1;
    this.loadProducts();
  }

  onActionClick(event: { action: string; item: any }) {
    const { action, item } = event;
    
    switch (action) {
      case 'view':
        this.viewProduct(item.id);
        break;
      case 'edit':
        this.editProduct(item.id);
        break;
      case 'delete':
        this.confirmDelete(item.id);
        break;
    }
  }

  viewProduct(id: number) {
    // Navigate to product detail view
    window.location.href = `/admin/products/${id}`;
  }

  editProduct(id: number) {
    // Navigate to product edit
    window.location.href = `/admin/products/${id}`;
  }

  confirmDelete(id: number) {
    this.productToDelete = id;
    this.showDeleteModal = true;
  }

  onDeleteConfirm() {
    if (this.productToDelete) {
      this.productService.deleteProduct(this.productToDelete).subscribe({
        next: () => {
          this.showDeleteModal = false;
          this.productToDelete = null;
          this.loadProducts();
        },
        error: (err) => {
          console.error('Failed to delete product', err);
          alert('Failed to delete product. It may have dependencies.');
          this.showDeleteModal = false;
        }
      });
    }
  }

  onDeleteCancel() {
    this.showDeleteModal = false;
    this.productToDelete = null;
  }

  exportProducts() {
    // TODO: Implement export functionality
    console.log('Exporting products...');
    alert('Export functionality coming soon!');
  }
}

