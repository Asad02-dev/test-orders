import { Component, input, computed, ChangeDetectionStrategy } from '@angular/core';

type BadgeVariant = 'success' | 'warning' | 'danger' | 'info' | 'neutral';

const ORDER_STATUS_MAP: Record<string, BadgeVariant> = {
  Pending: 'warning',
  ReservationConfirmed: 'info',
  PaymentAuthorized: 'info',
  Confirmed: 'success',
  Shipped: 'info',
  Delivered: 'success',
  Cancelled: 'danger',
  Failed: 'danger',
};

const PAYMENT_STATUS_MAP: Record<string, BadgeVariant> = {
  Pending: 'warning',
  Authorized: 'info',
  Captured: 'success',
  Failed: 'danger',
  Refunded: 'neutral',
};

@Component({
  selector: 'app-status-badge',
  standalone: true,
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadgeComponent {
  readonly status = input.required<string>();
  readonly type = input<'order' | 'payment'>('order');

  readonly variant = computed<BadgeVariant>(() => {
    const map = this.type() === 'payment' ? PAYMENT_STATUS_MAP : ORDER_STATUS_MAP;
    return map[this.status()] ?? 'neutral';
  });

  readonly badgeClass = computed(() => `badge badge-${this.variant()}`);
}
