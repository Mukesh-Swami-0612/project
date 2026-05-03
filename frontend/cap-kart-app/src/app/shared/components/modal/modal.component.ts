import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="modal-overlay" *ngIf="isOpen" (click)="onOverlayClick($event)">
      <div class="modal-container" [class]="'modal-' + size" (click)="$event.stopPropagation()">
        <div class="modal-header" *ngIf="title || showClose">
          <h2 class="modal-title">{{ title }}</h2>
          <button class="modal-close" (click)="close()" *ngIf="showClose">
            ✕
          </button>
        </div>

        <div class="modal-body">
          <ng-content></ng-content>
        </div>

        <div class="modal-footer" *ngIf="showFooter">
          <ng-content select="[slot=footer]"></ng-content>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: var(--spacing-lg);
      animation: fadeIn 0.2s ease;
    }

    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    .modal-container {
      background: var(--surface-container-lowest);
      border-radius: var(--radius-lg);
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
      max-height: 90vh;
      display: flex;
      flex-direction: column;
      animation: slideUp 0.3s ease;
    }

    @keyframes slideUp {
      from {
        opacity: 0;
        transform: translateY(20px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .modal-sm { width: 100%; max-width: 400px; }
    .modal-md { width: 100%; max-width: 600px; }
    .modal-lg { width: 100%; max-width: 900px; }
    .modal-xl { width: 100%; max-width: 1200px; }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: var(--spacing-lg);
      border-bottom: 1px solid var(--outline-variant);
    }

    .modal-title {
      font-size: 20px;
      font-weight: 600;
      color: var(--on-surface);
      margin: 0;
    }

    .modal-close {
      background: none;
      border: none;
      font-size: 24px;
      color: var(--on-surface-variant);
      cursor: pointer;
      padding: 4px;
      line-height: 1;
      transition: color 0.2s ease;
    }

    .modal-close:hover {
      color: var(--on-surface);
    }

    .modal-body {
      padding: var(--spacing-lg);
      overflow-y: auto;
      flex: 1;
    }

    .modal-footer {
      padding: var(--spacing-lg);
      border-top: 1px solid var(--outline-variant);
      display: flex;
      justify-content: flex-end;
      gap: var(--spacing-md);
    }
  `]
})
export class ModalComponent implements OnInit, OnDestroy {
  @Input() isOpen = false;
  @Input() title = '';
  @Input() size: 'sm' | 'md' | 'lg' | 'xl' = 'md';
  @Input() showClose = true;
  @Input() showFooter = false;
  @Input() closeOnOverlayClick = true;
  @Output() closed = new EventEmitter<void>();

  ngOnInit() {
    if (this.isOpen) {
      document.body.style.overflow = 'hidden';
    }
  }

  ngOnDestroy() {
    document.body.style.overflow = '';
  }

  ngOnChanges() {
    if (this.isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
  }

  close(): void {
    this.isOpen = false;
    document.body.style.overflow = '';
    this.closed.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    if (this.closeOnOverlayClick) {
      this.close();
    }
  }
}
