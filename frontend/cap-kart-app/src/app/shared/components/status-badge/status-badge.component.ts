import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ProductStatus = 'Draft' | 'InEnrichment' | 'ReadyForReview' | 'Approved' | 'Published' | 'Rejected' | 'Archived';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span 
      class="status-badge"
      [ngClass]="getStatusClass()"
      [attr.data-status]="status">
      {{ status }}
    </span>
  `,
  styles: [`
    .status-badge {
      display: inline-flex;
      align-items: center;
      padding: 4px 12px;
      border-radius: var(--radius-full);
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      white-space: nowrap;
    }

    .status-draft {
      background: #fef3c7;
      color: #92400e;
    }

    .status-in-enrichment {
      background: #dbeafe;
      color: #1e40af;
    }

    .status-ready-for-review {
      background: #fed7aa;
      color: #ea580c;
    }

    .status-approved {
      background: #d1fae5;
      color: #065f46;
    }

    .status-published {
      background: #dcfce7;
      color: #166534;
    }

    .status-rejected {
      background: #fecaca;
      color: #dc2626;
    }

    .status-archived {
      background: #f3f4f6;
      color: #6b7280;
    }
  `]
})
export class StatusBadgeComponent {
  @Input() status: ProductStatus = 'Draft';

  getStatusClass(): string {
    const statusMap: Record<ProductStatus, string> = {
      'Draft': 'status-draft',
      'InEnrichment': 'status-in-enrichment',
      'ReadyForReview': 'status-ready-for-review',
      'Approved': 'status-approved',
      'Published': 'status-published',
      'Rejected': 'status-rejected',
      'Archived': 'status-archived'
    };
    return statusMap[this.status] || 'status-draft';
  }
}