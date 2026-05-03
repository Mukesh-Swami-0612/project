import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card" [class]="variant">
      <div class="card-header" *ngIf="title || hasHeaderSlot">
        <h3 class="card-title" *ngIf="title">{{ title }}</h3>
        <ng-content select="[slot=header]"></ng-content>
      </div>

      <div class="card-body" [class.no-padding]="noPadding">
        <ng-content></ng-content>
      </div>

      <div class="card-footer" *ngIf="hasFooterSlot">
        <ng-content select="[slot=footer]"></ng-content>
      </div>
    </div>
  `,
  styles: [`
    .card {
      background: var(--surface-container-lowest);
      border-radius: var(--radius-lg);
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
      overflow: hidden;
    }

    .card.elevated {
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
    }

    .card.outlined {
      box-shadow: none;
      border: 1px solid var(--outline-variant);
    }

    .card-header {
      padding: var(--spacing-lg);
      border-bottom: 1px solid var(--outline-variant);
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .card-title {
      font-size: 18px;
      font-weight: 600;
      color: var(--on-surface);
      margin: 0;
    }

    .card-body {
      padding: var(--spacing-lg);
    }

    .card-body.no-padding {
      padding: 0;
    }

    .card-footer {
      padding: var(--spacing-lg);
      border-top: 1px solid var(--outline-variant);
      background: var(--surface-container-low);
    }
  `]
})
export class CardComponent {
  @Input() title = '';
  @Input() variant: 'default' | 'elevated' | 'outlined' = 'default';
  @Input() noPadding = false;

  hasHeaderSlot = false;
  hasFooterSlot = false;

  ngAfterContentInit() {
    // Check for slotted content
  }
}
