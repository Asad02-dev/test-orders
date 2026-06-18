import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AgGridAngular } from 'ag-grid-angular';
import { AllCommunityModule, ModuleRegistry, ColDef, GridReadyEvent, ICellRendererParams } from 'ag-grid-community';
import { InventoryService } from '../../../../core/services/inventory.service';
import { ToastService } from '../../../../core/services/toast.service';
import { InventoryItemDto, CreateInventoryItemRequest } from '../../../../core/models';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

ModuleRegistry.registerModules([AllCommunityModule]);

@Component({
  selector: 'app-inventory-management',
  standalone: true,
  imports: [FormsModule, AgGridAngular, LoadingSpinnerComponent, EmptyStateComponent],
  templateUrl: './inventory-management.html',
  styleUrl: './inventory-management.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InventoryManagementComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly toastService = inject(ToastService);

  readonly loading = signal(false);
  readonly inventoryItems = signal<InventoryItemDto[]>([]);
  readonly showLowStockOnly = signal(false);
  readonly showAddForm = signal(false);
  readonly restockingItemId = signal<string | null>(null);
  readonly restockQuantity = signal(0);

  readonly newItem = signal<CreateInventoryItemRequest>({
    productId: '',
    productName: '',
    quantityOnHand: 0,
    reorderThreshold: 10,
  });

  readonly lowStockItems = computed(() =>
    this.inventoryItems().filter((item) => item.isLowStock)
  );

  readonly displayedItems = computed(() =>
    this.showLowStockOnly() ? this.lowStockItems() : this.inventoryItems()
  );

  readonly columnDefs: ColDef[] = [
    { field: 'productName', headerName: 'Product Name', flex: 2, minWidth: 150 },
    { field: 'quantityOnHand', headerName: 'Qty On Hand', flex: 1, minWidth: 100 },
    { field: 'quantityReserved', headerName: 'Qty Reserved', flex: 1, minWidth: 100 },
    { field: 'quantityAvailable', headerName: 'Available', flex: 1, minWidth: 100 },
    { field: 'reorderThreshold', headerName: 'Reorder Threshold', flex: 1, minWidth: 120 },
    {
      field: 'isLowStock',
      headerName: 'Status',
      flex: 1,
      minWidth: 100,
      cellRenderer: (params: ICellRendererParams) => {
        return params.value
          ? '<span class="badge badge-danger">Low Stock</span>'
          : '<span class="badge badge-success">In Stock</span>';
      },
    },
    {
      field: 'updatedAt',
      headerName: 'Last Updated',
      flex: 1,
      minWidth: 140,
      valueFormatter: (params) => {
        if (!params.value) return '';
        return new Date(params.value).toLocaleDateString();
      },
    },
  ];

  readonly defaultColDef: ColDef = {
    sortable: true,
    filter: true,
    resizable: true,
  };

  ngOnInit(): void {
    this.loadInventory();
  }

  loadInventory(): void {
    this.loading.set(true);
    // The inventory API only exposes getLowStock() for listing; individual items require a productId.
    this.inventoryService.getLowStock().subscribe({
      next: (items: InventoryItemDto[]) => {
        this.inventoryItems.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load inventory.');
        this.loading.set(false);
      },
    });
  }

  toggleLowStockFilter(): void {
    this.showLowStockOnly.update((v) => !v);
  }

  toggleAddForm(): void {
    this.showAddForm.update((v) => !v);
  }

  updateNewItem(field: keyof CreateInventoryItemRequest, value: string | number): void {
    this.newItem.update((item) => ({ ...item, [field]: value }));
  }

  submitNewItem(): void {
    const item = this.newItem();
    if (!item.productId || !item.productName) {
      this.toastService.warning('Please fill in all required fields.');
      return;
    }

    this.loading.set(true);
    this.inventoryService.createInventoryItem(item).subscribe({
      next: () => {
        this.toastService.success('Inventory item created successfully.');
        this.showAddForm.set(false);
        this.newItem.set({ productId: '', productName: '', quantityOnHand: 0, reorderThreshold: 10 });
        this.loadInventory();
      },
      error: () => {
        this.toastService.error('Failed to create inventory item.');
        this.loading.set(false);
      },
    });
  }

  startRestock(itemId: string): void {
    this.restockingItemId.set(itemId);
    this.restockQuantity.set(0);
  }

  cancelRestock(): void {
    this.restockingItemId.set(null);
    this.restockQuantity.set(0);
  }

  submitRestock(item: InventoryItemDto): void {
    const qty = this.restockQuantity();
    if (qty <= 0) {
      this.toastService.warning('Please enter a valid quantity.');
      return;
    }

    this.inventoryService.restock(item.productId, qty).subscribe({
      next: () => {
        this.toastService.success(`Restocked ${item.productName} with ${qty} units.`);
        this.restockingItemId.set(null);
        this.restockQuantity.set(0);
        this.loadInventory();
      },
      error: () => {
        this.toastService.error(`Failed to restock ${item.productName}.`);
      },
    });
  }

  onGridReady(params: GridReadyEvent): void {
    params.api.sizeColumnsToFit();
  }
}
