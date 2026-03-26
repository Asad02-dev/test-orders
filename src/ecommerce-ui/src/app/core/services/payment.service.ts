import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaymentDto } from '../models';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/payments';

  getPaymentByOrder(orderId: string): Observable<PaymentDto> {
    return this.http.get<PaymentDto>(`${this.baseUrl}/orders/${orderId}`);
  }
}
