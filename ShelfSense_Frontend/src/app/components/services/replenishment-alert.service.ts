import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
 
export interface ReplenishmentAlert {
  alertId: number;
  productId: number;
  shelfId: number;
  predictedDepletionDate: string;
  urgencyLevel: string;
  createdAt: string;
}
 
export interface ApiResponse<T> { message: string; data: T }
 
const API_URL = '/api/ReplenishmentAlert/all';
 
@Injectable({ providedIn: 'root' })
export class ReplenishmentAlertService {
  constructor(private http: HttpClient, private auth: AuthService) {}
 
  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) throw new Error('Authentication token not found.');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }
 
  getAlerts(): Observable<ApiResponse<ReplenishmentAlert[]>> {
    return this.http.get<ApiResponse<ReplenishmentAlert[]>>(API_URL, { headers: this.getAuthHeaders() });
  }
}
 
 
 