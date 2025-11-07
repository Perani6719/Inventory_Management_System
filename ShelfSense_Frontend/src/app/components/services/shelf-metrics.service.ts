import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from './auth.service';
import { Observable } from 'rxjs';

export interface ShelfMetric {
  shelfId: number;
  shelfCode: string;
  totalCapacity: number;
  currentStock: number;
  occupancyPercentage: number;
  totalProductsAssigned: number;
  restockCountLast30Days: number;
  averageDaysBetweenRestocks: number;
}

export interface ApiResponse<T> {
  message: string;
  data: T;
}

const SHELF_METRICS_API_URL = '/api/ProductShelf/metrics';

@Injectable({ providedIn: 'root' })
export class ShelfMetricsService {
  constructor(private http: HttpClient, private auth: AuthService) {}

  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) throw new Error('Authentication token not found.');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  getShelfMetrics(): Observable<ApiResponse<ShelfMetric[]>> {
    return this.http.get<ApiResponse<ShelfMetric[]>>(SHELF_METRICS_API_URL, {
      headers: this.getAuthHeaders()
    });
  }
}
