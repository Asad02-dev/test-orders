import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AgGridAngular } from 'ag-grid-angular';
import { AllCommunityModule, ModuleRegistry, ColDef, GridReadyEvent, RowClickedEvent } from 'ag-grid-community';
import { CatalogService } from '../../../../core/services/catalog.service';
import { ToastService } from '../../../../core/services/toast.service';
import { ProductDto, CreateProductRequest, UpdateProductRequest, PagedResult } from '../../../../core/models';
import { CurrencyFormatPipe } from '../../../../shared/pipes/currency-format.pipe';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';

ModuleRegistry.registerModules([AllCommunityModule]);

type FormMode = 'hidden' | 'create' | 'edit';

@Component({
  selector: 'app-product-management',
  standalone: true,
  imports: [FormsModule, AgGridAngular, ConfirmDialogComponent],
  templateUrl: './product-management.html',
  styleUrl: './product-management.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductManagementComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);
  private readonly toastService = inject(ToastService);
  private readonly currencyPipe = new CurrencyFormatPipe();

  readonly categories = ['Electronics', 'Clothing', 'Books', 'Home & Garden', 'Sports', 'Toys'];

  readonly loading = signal(false);
  readonly products = signal<ProductDto[]>([]);
  readonly formMode = signal<FormMode>('hidden');
  readonly editingProductId = signal<string | null>(null);

  readonly formData = signal<CreateProductRequest & { isActive: boolean }>({
    name: '',
    description: '',
    price: 0,
    category: '',
    imageUrl: '',
    isActive: true,
  });

  readonly showDeleteConfirm = signal(false);

  readonly columnDefs: ColDef<ProductDto>[] = [
    { field: 'name', headerName: 'Name', flex: 2, minWidth: 150 },
    { field: 'id', headerName: 'SKU', flex: 1, minWidth: 100, valueFormatter: (p) => p.value?.substring(0, 8) ?? '' },
    { field: 'category', headerName: 'Category', flex: 1, minWidth: 120 },
    {
      field: 'price',
      headerName: 'Price',
      flex: 1,
      minWidth: 100,
      valueFormatter: (p) => this.currencyPipe.transform(p.value),
    },
    {
      field: 'isActive',
      headerName: 'Status',
      flex: 1,
      minWidth: 100,
      cellRenderer: (p: { value: boolean }) => {
        const label = p.value ? 'Active' : 'Inactive';
        const cls = p.value ? 'status-active' : 'status-inactive';
        return `<span class="${cls}">${label}</span>`;
      },
    },
    {
      field: 'createdAt',
      headerName: 'Created',
      flex: 1,
      minWidth: 120,
      valueFormatter: (p) => (p.value ? new Date(p.value).toLocaleDateString() : ''),
    },
  ];

  readonly defaultColDef: ColDef = {
    sortable: true,
    filter: true,
    resizable: true,
  };

  ngOnInit(): void {
    this.loadAllProducts();
  }

  onGridReady(_event: GridReadyEvent): void {
    // Grid is ready
  }

  onRowClicked(event: RowClickedEvent<ProductDto>): void {
    if (event.data) {
      this.openEditForm(event.data);
    }
  }

  loadAllProducts(): void {
    this.loading.set(true);
    this.catalogService.getProducts(1, 1000).subscribe({
      next: (result: PagedResult<ProductDto>) => {
        this.products.set(result.items);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load products.');
        this.loading.set(false);
      },
    });
  }

  openCreateForm(): void {
    this.formMode.set('create');
    this.editingProductId.set(null);
    this.formData.set({
      name: '',
      description: '',
      price: 0,
      category: this.categories[0],
      imageUrl: '',
      isActive: true,
    });
  }

  openEditForm(product: ProductDto): void {
    this.formMode.set('edit');
    this.editingProductId.set(product.id);
    this.formData.set({
      name: product.name,
      description: product.description,
      price: product.price,
      category: product.category,
      imageUrl: product.imageUrl,
      isActive: product.isActive,
    });
  }

  closeForm(): void {
    this.formMode.set('hidden');
    this.editingProductId.set(null);
  }

  updateField(field: string, value: string | number | boolean): void {
    this.formData.update((data) => ({ ...data, [field]: value }));
  }

  saveProduct(): void {
    const data = this.formData();

    if (!data.name.trim() || !data.category || data.price <= 0) {
      this.toastService.warning('Please fill in all required fields.');
      return;
    }

    if (this.formMode() === 'create') {
      const request: CreateProductRequest = {
        name: data.name,
        description: data.description,
        price: data.price,
        category: data.category,
        imageUrl: data.imageUrl,
      };

      this.catalogService.createProduct(request).subscribe({
        next: () => {
          this.toastService.success('Product created successfully!');
          this.closeForm();
          this.loadAllProducts();
        },
        error: () => this.toastService.error('Failed to create product.'),
      });
    } else {
      const id = this.editingProductId();
      if (!id) return;

      const request: UpdateProductRequest = {
        name: data.name,
        description: data.description,
        price: data.price,
        category: data.category,
        imageUrl: data.imageUrl,
        isActive: data.isActive,
      };

      this.catalogService.updateProduct(id, request).subscribe({
        next: () => {
          this.toastService.success('Product updated successfully!');
          this.closeForm();
          this.loadAllProducts();
        },
        error: () => this.toastService.error('Failed to update product.'),
      });
    }
  }

  deactivateProduct(): void {
    this.showDeleteConfirm.set(true);
  }

  confirmDelete(): void {
    const id = this.editingProductId();
    if (!id) return;

    this.showDeleteConfirm.set(false);
    this.catalogService.deleteProduct(id).subscribe({
      next: () => {
        this.toastService.success('Product deleted successfully!');
        this.closeForm();
        this.loadAllProducts();
      },
      error: () => this.toastService.error('Failed to delete product.'),
    });
  }

  cancelDelete(): void {
    this.showDeleteConfirm.set(false);
  }
}
