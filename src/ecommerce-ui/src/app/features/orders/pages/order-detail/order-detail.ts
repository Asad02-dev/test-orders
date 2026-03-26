import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../../../core/services/order.service';
import { PaymentService } from '../../../../core/services/payment.service';
import { ToastService } from '../../../../core/services/toast.service';
import { OrderDto, PaymentDto, OrderStatus } from '../../../../core/models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { CurrencyFormatPipe } from '../../../../shared/pipes/currency-format.pipe';

interface TimelineStep {
  label: string;
  status: 'completed' | 'current' | 'upcoming' | 'failed';
}

const LIFECYCLE_STATUSES: OrderStatus[] = [
  'Pending',
  'ReservationConfirmed',
  'PaymentAuthorized',
  'Confirmed',
  'Shipped',
  'Delivered',
];

const STATUS_LABELS: Record<string, string> = {
  Pending: 'Pending',
  ReservationConfirmed: 'Reserved',
  PaymentAuthorized: 'Payment Authorized',
  Confirmed: 'Confirmed',
  Shipped: 'Shipped',
  Delivered: 'Delivered',
  Cancelled: 'Cancelled',
  Failed: 'Failed',
};

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [
    RouterLink,
    FormsModule,
    StatusBadgeComponent,
    LoadingSpinnerComponent,
    ConfirmDialogComponent,
    CurrencyFormatPipe,
  ],
  templateUrl: './order-detail.html',
  styleUrl: './order-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly orderService = inject(OrderService);
  private readonly paymentService = inject(PaymentService);
  private readonly toastService = inject(ToastService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly order = signal<OrderDto | null>(null);
  readonly payment = signal<PaymentDto | null>(null);
  readonly paymentLoading = signal(false);
  readonly showCancelDialog = signal(false);
  readonly showCancelForm = signal(false);
  readonly cancelReason = signal('');
  readonly cancelling = signal(false);

  readonly orderId = computed(() => this.order()?.id ?? '');
  readonly shortId = computed(() => this.orderId().substring(0, 8));

  readonly canCancel = computed(() => {
    const status = this.order()?.status;
    return status === 'Pending' || status === 'ReservationConfirmed' || status === 'PaymentAuthorized';
  });

  readonly orderTotal = computed(() =>
    this.order()?.items?.reduce((sum, item) => sum + item.totalPrice, 0) ?? 0,
  );

  readonly timelineSteps = computed<TimelineStep[]>(() => {
    const order = this.order();
    if (!order) return [];

    const currentStatus = order.status;
    const isFailed = currentStatus === 'Cancelled' || currentStatus === 'Failed';
    const currentIndex = LIFECYCLE_STATUSES.indexOf(currentStatus as OrderStatus);

    const steps: TimelineStep[] = LIFECYCLE_STATUSES.map((s, i) => {
      let stepStatus: TimelineStep['status'];
      if (isFailed) {
        // Find where the order was before failure
        stepStatus = i <= Math.max(currentIndex, 0) ? 'completed' : 'upcoming';
      } else if (i < currentIndex) {
        stepStatus = 'completed';
      } else if (i === currentIndex) {
        stepStatus = 'current';
      } else {
        stepStatus = 'upcoming';
      }
      return { label: STATUS_LABELS[s], status: stepStatus };
    });

    if (isFailed) {
      steps.push({ label: STATUS_LABELS[currentStatus], status: 'failed' });
    }

    return steps;
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadOrder(id);
    } else {
      this.error.set('Invalid order ID');
      this.loading.set(false);
    }
  }

  loadOrder(id: string): void {
    this.loading.set(true);
    this.orderService.getOrder(id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);
        this.loadPayment(id);
      },
      error: () => {
        this.error.set('Failed to load order details.');
        this.loading.set(false);
      },
    });
  }

  loadPayment(orderId: string): void {
    this.paymentLoading.set(true);
    this.paymentService.getPaymentByOrder(orderId).subscribe({
      next: (payment) => {
        this.payment.set(payment);
        this.paymentLoading.set(false);
      },
      error: () => {
        this.paymentLoading.set(false);
      },
    });
  }

  openCancelDialog(): void {
    this.showCancelDialog.set(true);
  }

  onCancelConfirmed(): void {
    this.showCancelDialog.set(false);
    this.showCancelForm.set(true);
  }

  onCancelDialogCancelled(): void {
    this.showCancelDialog.set(false);
  }

  submitCancellation(): void {
    const reason = this.cancelReason().trim();
    if (!reason) {
      this.toastService.warning('Please provide a cancellation reason.');
      return;
    }

    this.cancelling.set(true);
    this.orderService.cancelOrder(this.orderId(), reason).subscribe({
      next: (updatedOrder) => {
        this.order.set(updatedOrder);
        this.showCancelForm.set(false);
        this.cancelReason.set('');
        this.cancelling.set(false);
        this.toastService.success('Order cancelled successfully.');
      },
      error: () => {
        this.cancelling.set(false);
        this.toastService.error('Failed to cancel order.');
      },
    });
  }

  dismissCancelForm(): void {
    this.showCancelForm.set(false);
    this.cancelReason.set('');
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleString();
  }
}
