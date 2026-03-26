import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PaymentService } from '../../../../core/services/payment.service';
import { ToastService } from '../../../../core/services/toast.service';
import { PaymentDto } from '../../../../core/models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { CurrencyFormatPipe } from '../../../../shared/pipes/currency-format.pipe';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-payment-list',
  standalone: true,
  imports: [FormsModule, StatusBadgeComponent, LoadingSpinnerComponent, EmptyStateComponent, CurrencyFormatPipe, DatePipe],
  templateUrl: './payment-list.html',
  styleUrl: './payment-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaymentListComponent {
  private readonly paymentService = inject(PaymentService);
  private readonly toastService = inject(ToastService);

  readonly searchOrderId = signal('');
  readonly loading = signal(false);
  readonly payment = signal<PaymentDto | null>(null);
  readonly notFound = signal(false);
  readonly recentSearches = signal<string[]>([]);

  searchPayment(): void {
    const orderId = this.searchOrderId().trim();
    if (!orderId) {
      this.toastService.warning('Please enter an Order ID.');
      return;
    }

    this.loading.set(true);
    this.payment.set(null);
    this.notFound.set(false);

    this.paymentService.getPaymentByOrder(orderId).subscribe({
      next: (payment) => {
        this.payment.set(payment);
        this.loading.set(false);
        this.addToRecentSearches(orderId);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
        this.addToRecentSearches(orderId);
      },
    });
  }

  searchFromHistory(orderId: string): void {
    this.searchOrderId.set(orderId);
    this.searchPayment();
  }

  clearSearch(): void {
    this.searchOrderId.set('');
    this.payment.set(null);
    this.notFound.set(false);
  }

  private addToRecentSearches(orderId: string): void {
    this.recentSearches.update((searches) => {
      const filtered = searches.filter((s) => s !== orderId);
      return [orderId, ...filtered].slice(0, 10);
    });
  }
}
