import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';

// API base URL
const DELIVERYLOG_API = '/api/DeliveryStatusLog';

// Response wrapper
export interface ApiResponse<T> {
  message: string;
  data: T;
}

// Delivery status log model
export interface DeliveryStatusLog {
  requestId: number;
  alertId?: number;
  deliveryStatus: string;
  statusChangedAt: string;
}

@Injectable({ providedIn: 'root' })
export class DeliveryLogService {
  constructor(private http: HttpClient, private auth: AuthService) {}

  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) {
      throw new Error('Authentication token not found.');
    }
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  /**
   * Get all delivery status logs.
   */
  getAllLogs(): Observable<ApiResponse<DeliveryStatusLog[]>> {
    return this.http.get<ApiResponse<DeliveryStatusLog[]>>(DELIVERYLOG_API, {
      headers: this.getAuthHeaders()
    });
  }
}
