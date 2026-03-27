import { Component, ChangeDetectionStrategy, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, switchMap } from 'rxjs/operators';
import { CartService } from '../../../../core/services/cart.service';
import { ToastService } from '../../../../core/services/toast.service';
import { CartItemDto, UpdateCartItemRequest } from '../../../../core/models';
import { CurrencyFormatPipe } from '../../../../shared/pipes/currency-format.pipe';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    CurrencyFormatPipe,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    ConfirmDialogComponent,
  ],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CartComponent implements OnInit, OnDestroy {
  private readonly cartService = inject(CartService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly cart = this.cartService.cart;
  readonly itemCount = this.cartService.itemCount;
  readonly totalAmount = this.cartService.totalAmount;

  readonly showRemoveDialog = signal(false);
  readonly showClearDialog = signal(false);
  readonly itemToRemove = signal<CartItemDto | null>(null);

  private readonly quantityUpdate$ = new Subject<UpdateCartItemRequest>();
  private readonly destroy$ = new Subject<void>();

  ngOnInit(): void {
    this.loadCart();

    this.quantityUpdate$
      .pipe(
        debounceTime(400),
        switchMap((req: UpdateCartItemRequest) => this.cartService.updateItem(req)),
      )
      .subscribe({
        error: () => this.toastService.error('Failed to update quantity.'),
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.quantityUpdate$.complete();
  }

  loadCart(): void {
    this.loading.set(true);
    this.cartService.getCart().subscribe({
      next: () => this.loading.set(false),
      error: () => {
        this.toastService.error('Failed to load cart.');
        this.loading.set(false);
      },
    });
  }

  onQuantityChange(item: CartItemDto, quantity: number): void {
    const newQuantity = Math.max(1, Math.floor(quantity));
    if (newQuantity === item.quantity) return;

    this.quantityUpdate$.next({ productId: item.productId, quantity: newQuantity });
  }

  incrementQuantity(item: CartItemDto): void {
    this.onQuantityChange(item, item.quantity + 1);
  }

  decrementQuantity(item: CartItemDto): void {
    if (item.quantity <= 1) return;
    this.onQuantityChange(item, item.quantity - 1);
  }

  confirmRemoveItem(item: CartItemDto): void {
    this.itemToRemove.set(item);
    this.showRemoveDialog.set(true);
  }

  removeItem(): void {
    const item = this.itemToRemove();
    if (!item) return;

    this.showRemoveDialog.set(false);
    this.cartService.removeItem(item.productId).subscribe({
      next: () => this.toastService.success(`${item.productName} removed from cart.`),
      error: () => this.toastService.error('Failed to remove item.'),
    });
    this.itemToRemove.set(null);
  }

  cancelRemove(): void {
    this.showRemoveDialog.set(false);
    this.itemToRemove.set(null);
  }

  confirmClearCart(): void {
    this.showClearDialog.set(true);
  }

  clearCart(): void {
    this.showClearDialog.set(false);
    this.cartService.clearCart().subscribe({
      next: () => this.toastService.success('Cart cleared.'),
      error: () => this.toastService.error('Failed to clear cart.'),
    });
  }

  cancelClear(): void {
    this.showClearDialog.set(false);
  }

  proceedToCheckout(): void {
    this.router.navigate(['/cart', 'checkout']);
  }
}
