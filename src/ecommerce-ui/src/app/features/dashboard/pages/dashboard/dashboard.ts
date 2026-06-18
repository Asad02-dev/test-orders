import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DatePipe, SlicePipe } from '@angular/common';
import { forkJoin, catchError, of } from 'rxjs';
import { CatalogService } from '../../../../core/services/catalog.service';
import { OrderService } from '../../../../core/services/order.service';
import { InventoryService } from '../../../../core/services/inventory.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { OrderDto, InventoryItemDto, PagedResult } from '../../../../core/models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { CurrencyFormatPipe } from '../../../../shared/pipes/currency-format.pipe';

interface QuickAction {
  icon: string;
  label: string;
  route: string;
  color: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, DatePipe, SlicePipe, StatusBadgeComponent, LoadingSpinnerComponent, CurrencyFormatPipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);
  private readonly orderService = inject(OrderService);
  private readonly inventoryService = inject(InventoryService);
  private readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly currentDate = signal(new Date());

  readonly totalProducts = signal<number | null>(null);
  readonly totalOrders = signal<number | null>(null);
  readonly lowStockCount = signal<number | null>(null);
  readonly notificationStatus = signal<string>('N/A');

  readonly recentOrders = signal<OrderDto[]>([]);
  readonly lowStockItems = signal<InventoryItemDto[]>([]);

  readonly quickActions: QuickAction[] = [
    { icon: '🛍️', label: 'Browse Catalog', route: '/catalog', color: 'var(--color-accent)' },
    { icon: '🛒', label: 'View Cart', route: '/cart', color: 'var(--color-success)' },
    { icon: '📋', label: 'My Orders', route: '/orders', color: 'var(--color-info)' },
    { icon: '⚙️', label: 'Manage Products', route: '/catalog/manage', color: 'var(--color-warning)' },
    { icon: '📦', label: 'Inventory', route: '/inventory', color: 'var(--color-danger)' },
    { icon: '🔔', label: 'Notifications', route: '/notifications', color: 'var(--color-neutral)' },
  ];

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading.set(true);

    forkJoin({
      products: this.catalogService.getProducts(1, 1).pipe(catchError(() => of(null))),
      orders: this.orderService.getOrders(1, 5).pipe(catchError(() => of(null))),
      lowStock: this.inventoryService.getLowStock().pipe(catchError(() => of(null))),
      notifStatus: this.notificationService.getStatus().pipe(catchError(() => of(null))),
    }).subscribe({
      next: (results) => {
        this.totalProducts.set(results.products?.totalCount ?? null);

        if (results.orders) {
          this.totalOrders.set(results.orders.totalCount);
          this.recentOrders.set(results.orders.items.slice(0, 5));
        }

        if (results.lowStock) {
          this.lowStockCount.set(results.lowStock.length);
          this.lowStockItems.set(results.lowStock.slice(0, 5));
        }

        if (results.notifStatus) {
          const status = results.notifStatus.status;
          this.notificationStatus.set(status === 'Healthy' || status === 'Online' ? '✅ Online' : '❌ Offline');
        }

        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  navigateTo(route: string): void {
    this.router.navigate([route]);
  }
}
