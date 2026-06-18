import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { ToastService, Toast } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  templateUrl: './toast.html',
  styleUrl: './toast.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastComponent {
  private readonly toastService = inject(ToastService);

  readonly toasts = this.toastService.toasts;

  iconFor(type: Toast['type']): string {
    switch (type) {
      case 'success': return '✓';
      case 'error':   return '✕';
      case 'warning': return '⚠';
      case 'info':    return 'ℹ';
    }
  }

  remove(id: string): void {
    this.toastService.remove(id);
  }
}
