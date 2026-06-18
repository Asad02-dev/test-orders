import { Injectable, signal, computed } from '@angular/core';

export interface Toast {
  id: string;
  message: string;
  type: 'success' | 'error' | 'info' | 'warning';
  duration: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _toasts = signal<Toast[]>([]);

  readonly toasts = this._toasts.asReadonly();
  readonly count = computed(() => this._toasts().length);

  show(message: string, type: Toast['type'], duration: number = 5000): void {
    const toast: Toast = {
      id: crypto.randomUUID(),
      message,
      type,
      duration,
    };

    this._toasts.update((toasts) => [...toasts, toast]);

    if (duration > 0) {
      setTimeout(() => this.remove(toast.id), duration);
    }
  }

  success(message: string): void {
    this.show(message, 'success');
  }

  error(message: string): void {
    this.show(message, 'error', 8000);
  }

  info(message: string): void {
    this.show(message, 'info');
  }

  warning(message: string): void {
    this.show(message, 'warning', 6000);
  }

  remove(id: string): void {
    this._toasts.update((toasts) => toasts.filter((t) => t.id !== id));
  }
}
