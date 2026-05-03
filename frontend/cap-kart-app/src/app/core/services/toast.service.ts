import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'info' | 'warning';

export interface ToastMessage {
  id: string;
  type: ToastType;
  title?: string;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _toasts = new BehaviorSubject<ToastMessage[]>([]);
  readonly toasts$ = this._toasts.asObservable();

  show(message: string, type: ToastType = 'info', title?: string): void {
    const id = `${Date.now()}-${Math.random().toString(16).slice(2)}`;
    const next = [...this._toasts.value, { id, type, title, message }];
    this._toasts.next(next);

    setTimeout(() => this.dismiss(id), 4000);
  }

  dismiss(id: string): void {
    this._toasts.next(this._toasts.value.filter(t => t.id !== id));
  }
}

