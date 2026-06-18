import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ProductDto,
  CreateProductRequest,
  UpdateProductRequest,
  PagedResult,
} from '../models';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/products';

  getProducts(
    page: number = 1,
    pageSize: number = 10,
    category?: string,
  ): Observable<PagedResult<ProductDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (category) {
      params = params.set('category', category);
    }

    return this.http.get<PagedResult<ProductDto>>(this.baseUrl, { params });
  }

  getProduct(id: string): Observable<ProductDto> {
    return this.http.get<ProductDto>(`${this.baseUrl}/${id}`);
  }

  createProduct(request: CreateProductRequest): Observable<ProductDto> {
    return this.http.post<ProductDto>(this.baseUrl, request);
  }

  updateProduct(id: string, request: UpdateProductRequest): Observable<ProductDto> {
    return this.http.put<ProductDto>(`${this.baseUrl}/${id}`, request);
  }

  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
