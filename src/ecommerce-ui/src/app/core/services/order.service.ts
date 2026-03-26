import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { OrderDto, PlaceOrderRequest, PagedResult } from '../models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/orders';

  getOrders(page: number = 1, pageSize: number = 10): Observable<PagedResult<OrderDto>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResult<OrderDto>>(this.baseUrl, { params });
  }

  getOrder(id: string): Observable<OrderDto> {
    return this.http.get<OrderDto>(`${this.baseUrl}/${id}`);
  }

  placeOrder(request: PlaceOrderRequest): Observable<OrderDto> {
    return this.http.post<OrderDto>(this.baseUrl, request);
  }

  cancelOrder(id: string, reason: string): Observable<OrderDto> {
    return this.http.post<OrderDto>(`${this.baseUrl}/${id}/cancel`, { reason });
  }
}
