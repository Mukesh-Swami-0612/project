import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../../core/services/product.service';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { CardComponent } from '../../../../shared/components/card/card.component';
import { ModalComponent } from '../../../../shared/components/modal/modal.component';
import { finalize } from 'rxjs';

interface ApprovalItem {
  id: number;
  name: string;
  sku: string;
  categoryName: string;
  status: string;
  submittedBy: string;
  submittedAt: Date;
  thumbnailUrl?: string;
}

@Component({
  selector: 'app-admin-approvals',
  standalone: true,
  imports: [
    CommonModule, 
    RouterModule, 
    FormsModule,
    StatusBadgeComponent,
    CardComponent,
    ModalComponent
  ],
  template: `
    <div class="admin-approvals-container">
      <!-- Page Header -->
      <div class="page-header">
        <div class="header-content">
          <h1>Product Approvals</h1>
          <p class="subtitle">Review and approve products pending publication</p>
        </div>
        <div class="stats-badges">
          <div class="stat-badge pending">
            <span class="stat-value">{{ pendingCount }}</span>
            <span class="stat-label">Pending</span>
          </div>
          <div class="stat-badge approved">
            <span class="stat-value">{{ approvedCount }}</span>
            <span class="stat-label">Approved Today</span>
          </div>
        </div>
      </div>

      <!-- Filter Tabs -->
      <div class="filter-tabs">
        <button 
          class="tab-btn"
          [class.active]="activeTab === 'pending'"
          (click)="setActiveTab('pending')">
          Pending Review ({{ pendingCount }})
        </button>
        <button 
          class="tab-btn"
          [class.active]="activeTab === 'approved'"
          (click)="setActiveTab('approved')">
          Approved
        </button>
        <button 
          class="tab-btn"
          [class.active]="activeTab === 'rejected'"
          (click)="setActiveTab('rejected')">
          Rejected
        </button>
      </div>

      <!-- Loading State -->
      <div class="loading-state" *ngIf="loading">
        <div class="spinner"></div>
        <p>Loading products...</p>
      </div>

      <!-- Approval Cards Grid -->
      <div class="approvals-grid" *ngIf="!loading">
        <app-card *ngFor="let item of filteredItems" class="approval-card">
          <div class="card-content">
            <!-- Product Image -->
            <div class="product-image">
              <img [src]="item.thumbnailUrl || 'assets/images/placeholder.png'" 
                   [alt]="item.name"
                   (error)="onImageError($event)">
            </div>

            <!-- Product Info -->
            <div class="product-info">
              <h3 class="product-name">{{ item.name }}</h3>
              <p class="product-sku">SKU: {{ item.sku }}</p>
              <p class="product-category">{{ item.categoryName }}</p>
              
              <div class="product-meta">
                <app-status-badge [status]="getProductStatus(item.status)"></app-status-badge>
                <span class="submitted-info">
                  Submitted {{ item.submittedAt | date:'short' }}
                </span>
              </div>
            </div>

            <!-- Actions -->
            <div class="card-actions">
              <button class="btn-view" (click)="viewProduct(item.id)">
                <span class="icon">👁️</span>
                View Details
              </button>
              
              <div class="approval-actions" *ngIf="item.status === 'ReadyForReview'">
                <button class="btn-approve" (click)="openApproveModal(item)">
                  <span class="icon">✓</span>
                  Approve
                </button>
                <button class="btn-reject" (click)="openRejectModal(item)">
                  <span class="icon">✕</span>
                  Reject
                </button>
              </div>

              <div class="status-info" *ngIf="item.status !== 'ReadyForReview'">
                <span class="status-text">
                  {{ item.status === 'Approved' ? 'Approved' : 'Rejected' }}
                </span>
              </div>
            </div>
          </div>
        </app-card>

        <!-- Empty State -->
        <div class="empty-state" *ngIf="filteredItems.length === 0">
          <span class="empty-icon">📋</span>
          <h3>No products {{ activeTab }}</h3>
          <p>There are no products in this category at the moment.</p>
        </div>
      </div>

      <!-- Approve Modal -->
      <app-modal
        [isOpen]="showApproveModal"
        title="Approve Product"
        size="md"
        [showFooter]="true"
        (closed)="onApproveCancel()">
        <div class="modal-content" *ngIf="selectedProduct">
          <p>Are you sure you want to approve this product?</p>
          <div class="product-summary">
            <strong>{{ selectedProduct.name }}</strong>
            <span>SKU: {{ selectedProduct.sku }}</span>
          </div>
          
          <div class="form-group">
            <label>Comments (Optional)</label>
            <textarea 
              [(ngModel)]="approvalComments"
              placeholder="Add any comments or notes..."
              rows="4"
              class="form-textarea"></textarea>
          </div>
        </div>
        
        <div slot="footer" class="modal-actions">
          <button class="btn-secondary" (click)="onApproveCancel()">Cancel</button>
          <button class="btn-success" (click)="onApproveConfirm()">Approve Product</button>
        </div>
      </app-modal>

      <!-- Reject Modal -->
      <app-modal
        [isOpen]="showRejectModal"
        title="Reject Product"
        size="md"
        [showFooter]="true"
        (closed)="onRejectCancel()">
        <div class="modal-content" *ngIf="selectedProduct">
          <p>Please provide a reason for rejecting this product:</p>
          <div class="product-summary">
            <strong>{{ selectedProduct.name }}</strong>
            <span>SKU: {{ selectedProduct.sku }}</span>
          </div>
          
          <div class="form-group">
            <label>Rejection Reason *</label>
            <textarea 
              [(ngModel)]="rejectionReason"
              placeholder="Explain why this product is being rejected..."
              rows="4"
              class="form-textarea"
              required></textarea>
          </div>
        </div>
        
        <div slot="footer" class="modal-actions">
          <button class="btn-secondary" (click)="onRejectCancel()">Cancel</button>
          <button class="btn-danger" 
                  [disabled]="!rejectionReason"
                  (click)="onRejectConfirm()">
            Reject Product
          </button>
        </div>
      </app-modal>
    </div>
  `,
  styles: [`
    .admin-approvals-container {
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

    .stats-badges {
      display: flex;
      gap: var(--spacing-md);
    }

    .stat-badge {
      background: var(--surface-container-lowest);
      border-radius: var(--radius-md);
      padding: var(--spacing-md) var(--spacing-lg);
      text-align: center;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    .stat-badge.pending {
      border-left: 4px solid #f97316;
    }

    .stat-badge.approved {
      border-left: 4px solid #22c55e;
    }

    .stat-value {
      display: block;
      font-size: 24px;
      font-weight: 700;
      color: var(--on-surface);
      line-height: 1;
      margin-bottom: 4px;
    }

    .stat-label {
      display: block;
      font-size: 12px;
      color: var(--on-surface-variant);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .filter-tabs {
      display: flex;
      gap: var(--spacing-sm);
      margin-bottom: var(--spacing-xl);
      border-bottom: 2px solid var(--outline-variant);
    }

    .tab-btn {
      padding: var(--spacing-md) var(--spacing-lg);
      background: none;
      border: none;
      border-bottom: 2px solid transparent;
      color: var(--on-surface-variant);
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
      margin-bottom: -2px;
    }

    .tab-btn:hover {
      color: var(--on-surface);
      background: var(--surface-container-low);
    }

    .tab-btn.active {
      color: var(--primary);
      border-bottom-color: var(--primary);
    }

    .loading-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: calc(var(--spacing-xl) * 2);
      gap: var(--spacing-md);
    }

    .spinner {
      width: 40px;
      height: 40px;
      border: 4px solid var(--outline-variant);
      border-top: 4px solid var(--primary);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .approvals-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
      gap: var(--spacing-lg);
    }

    .approval-card {
      transition: transform 0.2s ease, box-shadow 0.2s ease;
    }

    .approval-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
    }

    .card-content {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-md);
    }

    .product-image {
      width: 100%;
      height: 200px;
      background: var(--surface-container);
      border-radius: var(--radius-md);
      overflow: hidden;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .product-image img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .product-info {
      flex: 1;
    }

    .product-name {
      font-size: 18px;
      font-weight: 600;
      color: var(--on-surface);
      margin: 0 0 8px 0;
      line-height: 1.3;
    }

    .product-sku, .product-category {
      font-size: 14px;
      color: var(--on-surface-variant);
      margin: 4px 0;
    }

    .product-meta {
      display: flex;
      align-items: center;
      gap: var(--spacing-md);
      margin-top: var(--spacing-md);
      padding-top: var(--spacing-md);
      border-top: 1px solid var(--outline-variant);
    }

    .submitted-info {
      font-size: 12px;
      color: var(--on-surface-variant);
    }

    .card-actions {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-sm);
    }

    .btn-view, .btn-approve, .btn-reject, .btn-success, .btn-danger, .btn-secondary {
      padding: 10px 16px;
      border: none;
      border-radius: var(--radius-md);
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      transition: all 0.2s ease;
    }

    .btn-view {
      background: var(--surface-container);
      color: var(--on-surface);
      border: 1px solid var(--outline-variant);
    }

    .btn-view:hover {
      background: var(--surface-container-high);
    }

    .approval-actions {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: var(--spacing-sm);
    }

    .btn-approve, .btn-success {
      background: var(--secondary);
      color: var(--on-secondary);
    }

    .btn-approve:hover, .btn-success:hover {
      background: #16a34a;
    }

    .btn-reject, .btn-danger {
      background: var(--error);
      color: var(--on-error);
    }

    .btn-reject:hover, .btn-danger:hover {
      background: #b91c1c;
    }

    .btn-secondary {
      background: var(--surface-container);
      color: var(--on-surface);
      border: 1px solid var(--outline-variant);
    }

    .btn-secondary:hover {
      background: var(--surface-container-high);
    }

    .btn-danger:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .status-info {
      text-align: center;
      padding: var(--spacing-md);
      background: var(--surface-container-low);
      border-radius: var(--radius-md);
    }

    .status-text {
      font-weight: 500;
      color: var(--on-surface-variant);
    }

    .empty-state {
      grid-column: 1 / -1;
      text-align: center;
      padding: calc(var(--spacing-xl) * 2);
    }

    .empty-icon {
      font-size: 64px;
      display: block;
      margin-bottom: var(--spacing-lg);
      opacity: 0.5;
    }

    .empty-state h3 {
      font-size: 20px;
      color: var(--on-surface);
      margin: 0 0 8px 0;
    }

    .empty-state p {
      color: var(--on-surface-variant);
      margin: 0;
    }

    .modal-content {
      padding: var(--spacing-md) 0;
    }

    .modal-content p {
      margin: 0 0 var(--spacing-md) 0;
      color: var(--on-surface);
    }

    .product-summary {
      background: var(--surface-container-low);
      padding: var(--spacing-md);
      border-radius: var(--radius-md);
      margin-bottom: var(--spacing-lg);
    }

    .product-summary strong {
      display: block;
      color: var(--on-surface);
      margin-bottom: 4px;
    }

    .product-summary span {
      font-size: 14px;
      color: var(--on-surface-variant);
    }

    .form-group {
      margin-bottom: var(--spacing-md);
    }

    .form-group label {
      display: block;
      font-weight: 500;
      color: var(--on-surface);
      margin-bottom: var(--spacing-sm);
    }

    .form-textarea {
      width: 100%;
      padding: 12px;
      border: 1px solid var(--outline-variant);
      border-radius: var(--radius-md);
      font-family: var(--font-body);
      font-size: 14px;
      resize: vertical;
      background: var(--surface-container-lowest);
      color: var(--on-surface);
    }

    .form-textarea:focus {
      outline: none;
      border-color: var(--primary);
      box-shadow: 0 0 0 2px rgba(0, 56, 123, 0.1);
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

      .stats-badges {
        width: 100%;
        justify-content: space-between;
      }

      .approvals-grid {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class AdminApprovalsComponent implements OnInit {
  private productService = inject(ProductService);

  loading = false;
  activeTab: 'pending' | 'approved' | 'rejected' = 'pending';
  
  allItems: ApprovalItem[] = [];
  filteredItems: ApprovalItem[] = [];
  
  pendingCount = 0;
  approvedCount = 0;
  rejectedCount = 0;

  // Modal state
  showApproveModal = false;
  showRejectModal = false;
  selectedProduct: ApprovalItem | null = null;
  approvalComments = '';
  rejectionReason = '';

  ngOnInit() {
    this.loadApprovals();
  }

  loadApprovals() {
    this.loading = true;
    
    this.productService.getProducts('', 1, 100)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (result) => {
          this.allItems = result.items.map(p => ({
            id: p.id,
            name: p.name,
            sku: p.sku,
            categoryName: p.categoryName,
            status: p.status,
            submittedBy: 'Admin',
            submittedAt: new Date(p.createdAt),
            thumbnailUrl: undefined
          }));
          
          this.calculateCounts();
          this.filterItems();
        },
        error: (err) => {
          console.error('Failed to load approvals', err);
        }
      });
  }

  calculateCounts() {
    this.pendingCount = this.allItems.filter(i => i.status === 'ReadyForReview').length;
    this.approvedCount = this.allItems.filter(i => i.status === 'Approved').length;
    this.rejectedCount = this.allItems.filter(i => i.status === 'Rejected').length;
  }

  setActiveTab(tab: 'pending' | 'approved' | 'rejected') {
    this.activeTab = tab;
    this.filterItems();
  }

  filterItems() {
    const statusMap = {
      'pending': 'ReadyForReview',
      'approved': 'Approved',
      'rejected': 'Rejected'
    };
    
    this.filteredItems = this.allItems.filter(
      item => item.status === statusMap[this.activeTab]
    );
  }

  viewProduct(id: number) {
    window.location.href = `/admin/products/${id}`;
  }

  openApproveModal(item: ApprovalItem) {
    this.selectedProduct = item;
    this.approvalComments = '';
    this.showApproveModal = true;
  }

  openRejectModal(item: ApprovalItem) {
    this.selectedProduct = item;
    this.rejectionReason = '';
    this.showRejectModal = true;
  }

  onApproveConfirm() {
    if (this.selectedProduct) {
      const id = this.selectedProduct.id;
      this.productService.approveProduct(id).subscribe({
        next: () => {
          this.showApproveModal = false;
          this.selectedProduct = null;
          this.approvalComments = '';
          this.loadApprovals();
        },
        error: (err) => {
          console.error('Failed to approve product:', err);
          alert('Failed to approve product. Please try again.');
          this.showApproveModal = false;
        }
      });
    }
  }

  onApproveCancel() {
    this.showApproveModal = false;
    this.selectedProduct = null;
    this.approvalComments = '';
  }

  onRejectConfirm() {
    if (this.selectedProduct && this.rejectionReason) {
      const id = this.selectedProduct.id;
      const reason = this.rejectionReason;
      this.productService.rejectProduct(id, reason).subscribe({
        next: () => {
          this.showRejectModal = false;
          this.selectedProduct = null;
          this.rejectionReason = '';
          this.loadApprovals();
        },
        error: (err) => {
          console.error('Failed to reject product:', err);
          alert('Failed to reject product. Please try again.');
          this.showRejectModal = false;
        }
      });
    }
  }

  onRejectCancel() {
    this.showRejectModal = false;
    this.selectedProduct = null;
    this.rejectionReason = '';
  }


  onImageError(event: any) {
    event.target.src = 'assets/images/placeholder.png';
  }

  getProductStatus(status: string): any {
    return status as any;
  }
}
