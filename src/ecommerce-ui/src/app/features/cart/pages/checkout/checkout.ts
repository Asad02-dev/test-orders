import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CartService } from '../../../../core/services/cart.service';
import { ToastService } from '../../../../core/services/toast.service';
import { CurrencyFormatPipe } from '../../../../shared/pipes/currency-format.pipe';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    CurrencyFormatPipe,
    LoadingSpinnerComponent,
    EmptyStateComponent,
  ],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CheckoutComponent implements OnInit {
  private readonly cartService = inject(CartService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly cart = this.cartService.cart;
  readonly totalAmount = this.cartService.totalAmount;
  readonly itemCount = this.cartService.itemCount;

  readonly loading = signal(false);
  readonly submitting = signal(false);
  private readonly idempotencyKey = crypto.randomUUID();

  readonly checkoutForm: FormGroup = this.fb.group({
    customerName: ['', [Validators.required, Validators.minLength(2)]],
    customerEmail: ['', [Validators.required, Validators.email]],
  });

  ngOnInit(): void {
    if (!this.cart()) {
      this.loading.set(true);
      this.cartService.getCart().subscribe({
        next: (cart) => {
          this.loading.set(false);
          if (!cart || cart.items.length === 0) {
            this.router.navigate(['/cart']);
          }
        },
        error: () => {
          this.toastService.error('Failed to load cart.');
          this.loading.set(false);
          this.router.navigate(['/cart']);
        },
      });
    } else if (this.cart()!.items.length === 0) {
      this.router.navigate(['/cart']);
    }
  }

  get nameControl() {
    return this.checkoutForm.get('customerName')!;
  }

  get emailControl() {
    return this.checkoutForm.get('customerEmail')!;
  }

  get canSubmit(): boolean {
    return this.checkoutForm.valid && this.itemCount() > 0 && !this.submitting();
  }

  placeOrder(): void {
    if (!this.canSubmit) return;
    this.checkoutForm.markAllAsTouched();
    if (this.checkoutForm.invalid) return;

    this.submitting.set(true);
    const { customerName, customerEmail } = this.checkoutForm.value;

    this.cartService
      .checkout({
        customerName,
        customerEmail,
        idempotencyKey: this.idempotencyKey,
      })
      .subscribe({
        next: (response) => {
          this.submitting.set(false);
          this.toastService.success('Order placed successfully!');
          this.router.navigate(['/orders', response.orderId]);
        },
        error: () => {
          this.submitting.set(false);
          this.toastService.error('Failed to place order. Please try again.');
        },
      });
  }
}
