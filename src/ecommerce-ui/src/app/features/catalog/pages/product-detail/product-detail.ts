import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CatalogService } from '../../../../core/services/catalog.service';
import { CartService } from '../../../../core/services/cart.service';
import { ToastService } from '../../../../core/services/toast.service';
import { ProductDto } from '../../../../core/models';
import { CurrencyFormatPipe } from '../../../../shared/pipes/currency-format.pipe';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [FormsModule, RouterLink, CurrencyFormatPipe, LoadingSpinnerComponent],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly catalogService = inject(CatalogService);
  private readonly cartService = inject(CartService);
  private readonly toastService = inject(ToastService);

  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly product = signal<ProductDto | null>(null);
  readonly quantity = signal(1);

  readonly isActive = computed(() => this.product()?.isActive ?? false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.notFound.set(true);
      this.loading.set(false);
      return;
    }

    this.catalogService.getProduct(id).subscribe({
      next: (product: ProductDto) => {
        this.product.set(product);
        this.loading.set(false);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      },
    });
  }

  incrementQuantity(): void {
    this.quantity.update((q) => q + 1);
  }

  decrementQuantity(): void {
    this.quantity.update((q) => Math.max(1, q - 1));
  }

  onQuantityChange(value: number): void {
    this.quantity.set(Math.max(1, Math.floor(value)));
  }

  addToCart(): void {
    const product = this.product();
    if (!product) return;

    this.cartService
      .addItem({
        productId: product.id,
        productName: product.name,
        unitPrice: product.price,
        quantity: this.quantity(),
      })
      .subscribe({
        next: () => this.toastService.success(`${product.name} added to cart!`),
        error: () => this.toastService.error(`Failed to add ${product.name} to cart.`),
      });
  }

  goBack(): void {
    this.router.navigate(['/catalog']);
  }
}
