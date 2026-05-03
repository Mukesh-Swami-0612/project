import { AsyncPipe, NgFor, NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [NgFor, NgIf, AsyncPipe],
  template: `
    <div class="toast-container" aria-live="polite" aria-atomic="true">
      <div
        *ngFor="let t of (toastService.toasts$ | async)"
        class="toast"
        [class.toast--success]="t.type === 'success'"
        [class.toast--error]="t.type === 'error'"
        [class.toast--warning]="t.type === 'warning'"
        [class.toast--info]="t.type === 'info'">
        <div class="toast__content">
          <div class="toast__title" *ngIf="t.title">{{ t.title }}</div>
          <div class="toast__msg">{{ t.message }}</div>
        </div>
        <button class="toast__close" type="button" (click)="toastService.dismiss(t.id)" aria-label="Close">×</button>
      </div>
    </div>
  `,
  styles: [`
    .toast-container {
      position: fixed;
      top: 16px;
      right: 16px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 10px;
      width: min(420px, calc(100vw - 32px));
      pointer-events: none;
    }
    .toast {
      pointer-events: auto;
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 12px;
      padding: 12px 12px;
      border-radius: 12px;
      color: #0b1220;
      background: #ffffff;
      box-shadow: 0 14px 30px rgba(0,0,0,0.14);
      border-left: 6px solid #64748b;
      animation: toast-in 140ms ease-out;
    }
    .toast--success { border-left-color: #16a34a; }
    .toast--error { border-left-color: #dc2626; }
    .toast--warning { border-left-color: #f59e0b; }
    .toast--info { border-left-color: #2563eb; }
    .toast__title { font-weight: 700; margin-bottom: 2px; }
    .toast__msg { font-size: 13px; line-height: 1.35; color: #111827; }
    .toast__close {
      border: none;
      background: transparent;
      font-size: 18px;
      line-height: 1;
      cursor: pointer;
      color: #111827;
      opacity: 0.7;
    }
    .toast__close:hover { opacity: 1; }
    @keyframes toast-in {
      from { transform: translateY(-6px); opacity: 0; }
      to { transform: translateY(0); opacity: 1; }
    }
  `]
})
export class ToastContainerComponent {
  constructor(public toastService: ToastService) {}
}

