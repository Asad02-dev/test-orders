import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CatalogService } from '../../../../core/services/catalog.service';
import { CartService } from '../../../../core/services/cart.service';
import { ToastService } from '../../../../core/services/toast.service';
import { ProductDto, PagedResult } from '../../../../core/models';
import { ProductCardComponent } from '../../../../shared/components/product-card/product-card';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [FormsModule, ProductCardComponent, LoadingSpinnerComponent, EmptyStateComponent],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductListComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);
  private readonly cartService = inject(CartService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  readonly categories = ['All', 'Electronics', 'Clothing', 'Books', 'Home & Garden', 'Sports', 'Toys'];

  readonly loading = signal(false);
  readonly searchQuery = signal('');
  readonly selectedCategory = signal('All');
  readonly currentPage = signal(1);
  readonly pageSize = 12;

  readonly pagedResult = signal<PagedResult<ProductDto> | null>(null);

  readonly products = computed(() => {
    const result = this.pagedResult();
    if (!result) return [];
    const query = this.searchQuery().toLowerCase();
    if (!query) return result.items;
    return result.items.filter(
      (p) => p.name.toLowerCase().includes(query) || p.description.toLowerCase().includes(query),
    );
  });

  readonly totalCount = computed(() => this.pagedResult()?.totalCount ?? 0);
  readonly totalPages = computed(() => this.pagedResult()?.totalPages ?? 0);
  readonly hasNextPage = computed(() => this.pagedResult()?.hasNextPage ?? false);
  readonly hasPreviousPage = computed(() => this.pagedResult()?.hasPreviousPage ?? false);

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading.set(true);
    const category = this.selectedCategory() === 'All' ? undefined : this.selectedCategory();

    this.catalogService.getProducts(this.currentPage(), this.pageSize, category).subscribe({
      next: (result) => {
        this.pagedResult.set(result);
        this.loading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load products.');
        this.loading.set(false);
      },
    });
  }

  onSearchChange(query: string): void {
    this.searchQuery.set(query);
  }

  onCategoryChange(category: string): void {
    this.selectedCategory.set(category);
    this.currentPage.set(1);
    this.loadProducts();
  }

  onAddToCart(product: ProductDto): void {
    this.cartService
      .addItem({
        productId: product.id,
        productName: product.name,
        unitPrice: product.price,
        quantity: 1,
      })
      .subscribe({
        next: () => this.toastService.success(`${product.name} added to cart!`),
        error: () => this.toastService.error(`Failed to add ${product.name} to cart.`),
      });
  }

  onViewDetails(product: ProductDto): void {
    this.router.navigate(['/catalog', product.id]);
  }

  goToPage(page: number): void {
    this.currentPage.set(page);
    this.loadProducts();
  }

  previousPage(): void {
    if (this.hasPreviousPage()) {
      this.goToPage(this.currentPage() - 1);
    }
  }

  nextPage(): void {
    if (this.hasNextPage()) {
      this.goToPage(this.currentPage() + 1);
    }
  }
}
