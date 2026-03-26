import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
  CartDto,
  AddToCartRequest,
  UpdateCartItemRequest,
  CartCheckoutRequest,
  CartCheckoutResponse,
} from '../models';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/cart';

  private readonly _cart = signal<CartDto | null>(null);

  readonly cart = this._cart.asReadonly();
  readonly itemCount = computed(() => {
    const cart = this._cart();
    return cart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0;
  });
  readonly totalAmount = computed(() => this._cart()?.totalAmount ?? 0);

  getCart(): Observable<CartDto> {
    return this.http.get<CartDto>(this.baseUrl).pipe(
      tap((cart) => this._cart.set(cart)),
    );
  }

  addItem(request: AddToCartRequest): Observable<CartDto> {
    return this.http.post<CartDto>(`${this.baseUrl}/items`, request).pipe(
      tap((cart) => this._cart.set(cart)),
    );
  }

  updateItem(request: UpdateCartItemRequest): Observable<CartDto> {
    return this.http.put<CartDto>(`${this.baseUrl}/items`, request).pipe(
      tap((cart) => this._cart.set(cart)),
    );
  }

  removeItem(productId: string): Observable<CartDto> {
    return this.http.delete<CartDto>(`${this.baseUrl}/items/${productId}`).pipe(
      tap((cart) => this._cart.set(cart)),
    );
  }

  clearCart(): Observable<void> {
    return this.http.delete<void>(this.baseUrl).pipe(
      tap(() => this._cart.set(null)),
    );
  }

  checkout(request: CartCheckoutRequest): Observable<CartCheckoutResponse> {
    return this.http.post<CartCheckoutResponse>(`${this.baseUrl}/checkout`, request).pipe(
      tap(() => this._cart.set(null)),
    );
  }
}
