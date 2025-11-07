import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from './auth.service';
import { Observable } from 'rxjs';

export interface StockoutReportItem {
  productId: number;
  productName: string;
  shelfId: number;
  shelfLocation: string;
  stockoutCount: number;
  avgReplenishmentTimeInHours: number;
  avgReplenishmentDelayInHours: number;
  shelfAvailabilityPercentage: number;
}

@Injectable({ providedIn: 'root' })
export class InventoryReportService {
  private readonly API_URL = '/api/analytics/stockout-report';

  constructor(private http: HttpClient, private auth: AuthService) {}

  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) throw new Error('Authentication token not found.');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  getInventoryReport(startDate: string, endDate: string): Observable<StockoutReportItem[]> {
    const params = { startDate, endDate };
    return this.http.get<StockoutReportItem[]>(this.API_URL, {
      headers: this.getAuthHeaders(),
      params
    });
  }

  getInventoryReportPdf(startDate: string, endDate: string): Observable<Blob> {
    const params = { startDate, endDate };
    const headers = this.getAuthHeaders().set('Accept', 'application/pdf');

    return this.http.get(`${this.API_URL}/pdf`, {
      headers,
      params,
      responseType: 'blob'
    });
  }
}


