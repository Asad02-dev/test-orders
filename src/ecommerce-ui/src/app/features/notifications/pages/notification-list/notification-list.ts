import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AgGridAngular } from 'ag-grid-angular';
import { AllCommunityModule, ModuleRegistry, ColDef, GridReadyEvent, RowClassParams } from 'ag-grid-community';
import { NotificationService } from '../../../../core/services/notification.service';
import { ToastService } from '../../../../core/services/toast.service';
import { NotificationRecord } from '../../../../core/models';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
  selector: 'app-notification-list',
  standalone: true,
  imports: [FormsModule, AgGridAngular, LoadingSpinnerComponent, EmptyStateComponent],
  templateUrl: './notification-list.html',
  styleUrl: './notification-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationListComponent implements OnInit {
  private readonly notificationService = inject(NotificationService);
  private readonly toastService = inject(ToastService);

  readonly loading = signal(false);
  readonly notifications = signal<NotificationRecord[]>([]);
  readonly serviceStatus = signal<string>('unknown');
  readonly selectedCount = signal(25);
  readonly searchOrderId = signal('');
  readonly counts = [10, 25, 50, 100, 200];

  readonly columnDefs: ColDef[] = [
    { field: 'type', headerName: 'Type', flex: 1, minWidth: 120 },
    { field: 'recipientEmail', headerName: 'Recipient', flex: 2, minWidth: 180 },
    { field: 'subject', headerName: 'Subject', flex: 2, minWidth: 200 },
    {
      field: 'sentAt',
      headerName: 'Sent',
      flex: 1,
      minWidth: 80,
      cellRenderer: (params: { value: string | null }) => {
        return params.value
          ? '<span class="badge badge-success">Yes</span>'
          : '<span class="badge badge-warning">No</span>';
      },
    },
    {
      field: 'createdAt',
      headerName: 'Created',
      flex: 1,
      minWidth: 140,
      valueFormatter: (params) => params.value ? new Date(params.value).toLocaleDateString() : '',
    },
    {
      field: 'sentAt',
      headerName: 'Sent Date',
      flex: 1,
      minWidth: 140,
      valueFormatter: (params) => params.value ? new Date(params.value).toLocaleDateString() : '—',
    },
  ];

  readonly defaultColDef: ColDef = {
    sortable: true,
    filter: true,
    resizable: true,
  };

  readonly getRowClass = (params: RowClassParams): string => {
    return params.data && !params.data.sentAt ? 'notification-unsent-row' : '';
  };

  ngOnInit(): void {
    this.loadStatus();
    this.loadNotifications();
  }

  loadStatus(): void {
    this.notificationService.getStatus().subscribe({
      next: (res: { status: string }) => this.serviceStatus.set(res.status),
      error: () => this.serviceStatus.set('offline'),
    });
  }

  loadNotifications(): void {
    this.loading.set(true);
    this.searchOrderId.set('');
    this.notificationService.getNotifications(this.selectedCount()).subscribe({
      next: (items: NotificationRecord[]) => {
        this.notifications.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load notifications.');
        this.loading.set(false);
      },
    });
  }

  onCountChange(count: number): void {
    this.selectedCount.set(count);
    this.loadNotifications();
  }

  searchByOrderId(): void {
    const orderId = this.searchOrderId().trim();
    if (!orderId) {
      this.loadNotifications();
      return;
    }

    this.loading.set(true);
    this.notificationService.getNotificationsForOrder(orderId).subscribe({
      next: (items: NotificationRecord[]) => {
        this.notifications.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to search notifications.');
        this.loading.set(false);
      },
    });
  }

  onGridReady(params: GridReadyEvent): void {
    params.api.sizeColumnsToFit();
  }
}
