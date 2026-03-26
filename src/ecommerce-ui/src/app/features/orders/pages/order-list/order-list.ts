import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
} from '@angular/core';
import { Router } from '@angular/router';
import { AgGridAngular } from 'ag-grid-angular';
import {
  AllCommunityModule,
  ModuleRegistry,
  ColDef,
  GridReadyEvent,
  ICellRendererParams,
  CellClassParams,
} from 'ag-grid-community';
import { OrderService } from '../../../../core/services/order.service';
import { ToastService } from '../../../../core/services/toast.service';
import { OrderDto, PagedResult } from '../../../../core/models';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [AgGridAngular, LoadingSpinnerComponent, EmptyStateComponent],
  templateUrl: './order-list.html',
  styleUrl: './order-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderListComponent implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly currentPage = signal(1);
  readonly pageSize = signal(10);
  readonly pagedResult = signal<PagedResult<OrderDto> | null>(null);

  readonly orders = computed(() => this.pagedResult()?.items ?? []);
  readonly totalPages = computed(() => this.pagedResult()?.totalPages ?? 0);
  readonly hasNextPage = computed(() => this.pagedResult()?.hasNextPage ?? false);
  readonly hasPreviousPage = computed(() => this.pagedResult()?.hasPreviousPage ?? false);
  readonly isEmpty = computed(() => !this.loading() && this.orders().length === 0);

  readonly columnDefs: ColDef<OrderDto>[] = [
    {
      headerName: 'Order ID',
      field: 'id',
      valueFormatter: (params) => params.value?.substring(0, 8) ?? '',
      minWidth: 120,
      flex: 1,
    },
    {
      headerName: 'Customer',
      field: 'customerEmail',
      minWidth: 180,
      flex: 1.5,
    },
    {
      headerName: 'Status',
      field: 'status',
      minWidth: 160,
      cellRenderer: (params: ICellRendererParams<OrderDto>) => {
        const status = params.value ?? '';
        const variantMap: Record<string, string> = {
          Pending: 'warning',
          ReservationConfirmed: 'info',
          PaymentAuthorized: 'info',
          Confirmed: 'success',
          Shipped: 'info',
          Delivered: 'success',
          Cancelled: 'danger',
          Failed: 'danger',
        };
        const variant = variantMap[status] ?? 'neutral';
        return `<span class="badge badge-${variant}">${status}</span>`;
      },
    },
    {
      headerName: 'Total',
      field: 'totalAmount',
      valueFormatter: (params) =>
        new Intl.NumberFormat('en-US', {
          style: 'currency',
          currency: 'USD',
        }).format(params.value ?? 0),
      minWidth: 120,
      flex: 0.8,
    },
    {
      headerName: 'Items',
      valueGetter: (params) => params.data?.items?.length ?? 0,
      minWidth: 80,
      flex: 0.5,
    },
    {
      headerName: 'Created',
      field: 'createdAt',
      valueFormatter: (params) =>
        params.value ? new Date(params.value).toLocaleDateString() : '',
      minWidth: 120,
      flex: 1,
    },
    {
      headerName: 'Actions',
      minWidth: 100,
      flex: 0.6,
      sortable: false,
      filter: false,
      cellRenderer: (params: ICellRendererParams<OrderDto>) => {
        const btn = document.createElement('button');
        btn.className = 'btn btn-sm btn-view';
        btn.textContent = 'View';
        btn.addEventListener('click', () => {
          if (params.data) {
            this.router.navigate(['/orders', params.data.id]);
          }
        });
        return btn;
      },
    },
  ];

  readonly defaultColDef: ColDef = {
    sortable: true,
    filter: true,
    resizable: true,
  };

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.loading.set(true);
    this.orderService.getOrders(this.currentPage(), this.pageSize()).subscribe({
      next: (result) => {
        this.pagedResult.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load orders.');
        this.loading.set(false);
      },
    });
  }

  onGridReady(_event: GridReadyEvent): void {
    // Grid is ready
  }

  goToPage(page: number): void {
    this.currentPage.set(page);
    this.loadOrders();
  }

  nextPage(): void {
    if (this.hasNextPage()) {
      this.goToPage(this.currentPage() + 1);
    }
  }

  previousPage(): void {
    if (this.hasPreviousPage()) {
      this.goToPage(this.currentPage() - 1);
    }
  }

  onPageSizeChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.pageSize.set(Number(value));
    this.currentPage.set(1);
    this.loadOrders();
  }

  browseProducts(): void {
    this.router.navigate(['/catalog']);
  }
}
