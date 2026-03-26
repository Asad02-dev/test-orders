import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NotificationRecord } from '../models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/notifications';

  getStatus(): Observable<{ status: string }> {
    return this.http.get<{ status: string }>(`${this.baseUrl}/status`);
  }

  getNotifications(count?: number): Observable<NotificationRecord[]> {
    let params = new HttpParams();
    if (count !== undefined) {
      params = params.set('count', count.toString());
    }
    return this.http.get<NotificationRecord[]>(this.baseUrl, { params });
  }

  getNotificationsForOrder(orderId: string): Observable<NotificationRecord[]> {
    return this.http.get<NotificationRecord[]>(`${this.baseUrl}/orders/${orderId}`);
  }
}
