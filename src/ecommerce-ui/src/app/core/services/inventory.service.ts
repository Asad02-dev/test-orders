import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { InventoryItemDto, CreateInventoryItemRequest, RestockRequest } from '../models';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/inventory';

  getInventory(productId: string): Observable<InventoryItemDto> {
    return this.http.get<InventoryItemDto>(`${this.baseUrl}/products/${productId}`);
  }

  getLowStock(): Observable<InventoryItemDto[]> {
    return this.http.get<InventoryItemDto[]>(`${this.baseUrl}/low-stock`);
  }

  createInventoryItem(request: CreateInventoryItemRequest): Observable<InventoryItemDto> {
    return this.http.post<InventoryItemDto>(this.baseUrl, request);
  }

  restock(productId: string, quantity: number): Observable<InventoryItemDto> {
    const request: RestockRequest = { quantity };
    return this.http.post<InventoryItemDto>(
      `${this.baseUrl}/products/${productId}/restock`,
      request,
    );
  }
}
