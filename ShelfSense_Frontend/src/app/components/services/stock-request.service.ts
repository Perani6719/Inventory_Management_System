import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { StockAlert } from '../dashboard/create-request-alert/create-request-alert';

// API base URL
const STOCKREQUEST_API = '/api/StockRequest';

// Response wrapper
export interface ApiResponse<T> {
  message: string;
  data: T;
}

// Stock request model
export interface StockRequestApiModel {
  alertId?: number | null;
  requestId?: number;
  storeId?: number;
  productId?: number;
  quantity?: number;
  deliveryStatus?: string;
  requestDate?: string;
  requestedDeliveryDate?: string;
  estimatedTimeOfArrival?: string;
  productName?: string;
  requestedBy?: string;
}

@Injectable({ providedIn: 'root' })
export class StockRequestService {
  constructor(private http: HttpClient, private auth: AuthService) {}

  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) {
      throw new Error('Authentication token not found.');
    }
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  /**
   * Get all stock requests (admin/manager view).
   */
  getAll(): Observable<StockRequestApiModel[] | ApiResponse<StockRequestApiModel[]>> {
    return this.http.get<StockRequestApiModel[] | ApiResponse<StockRequestApiModel[]>>(STOCKREQUEST_API, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * Create stock requests from alerts.
   */
  createFromAlerts(): Observable<string> {
    return this.http.post(`${STOCKREQUEST_API}/create-from-alerts`, {}, {
      headers: this.getAuthHeaders(),
      responseType: 'text'
    });
  }

  /**
   * Get delivered stock requests.
   */
  getDelivered(): Observable<StockRequestApiModel[] | ApiResponse<StockRequestApiModel[]>> {
    return this.http.get<StockRequestApiModel[] | ApiResponse<StockRequestApiModel[]>>(`${STOCKREQUEST_API}/delivered`, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * Get cancelled stock requests.
   */
  getCancelled(): Observable<StockRequestApiModel[] | ApiResponse<StockRequestApiModel[]>> {
    return this.http.get<StockRequestApiModel[] | ApiResponse<StockRequestApiModel[]>>(`${STOCKREQUEST_API}/cancelled`, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * Get in-transit stock requests.
   */
  getInTransit(): Observable<StockRequestApiModel[] | ApiResponse<StockRequestApiModel[]>> {
    return this.http.get<StockRequestApiModel[] | ApiResponse<StockRequestApiModel[]>>(`${STOCKREQUEST_API}/in-transit`, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * Get pending stock requests.
   */
  getPending(): Observable<StockRequestApiModel[] | ApiResponse<StockRequestApiModel[]>> {
    return this.http.get<StockRequestApiModel[] | ApiResponse<StockRequestApiModel[]>>(`${STOCKREQUEST_API}/pending`, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * Get stock request by ID.
   */
  getById(id: number): Observable<StockRequestApiModel | ApiResponse<StockRequestApiModel>> {
    return this.http.get<StockRequestApiModel | ApiResponse<StockRequestApiModel>>(`${STOCKREQUEST_API}/${id}`, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * Delete a stock request.
   */
  deleteRequest(id: number): Observable<any> {
    let headers = this.getAuthHeaders();
    headers = headers.set('X-Confirm-Delete', 'true');
    return this.http.delete(`${STOCKREQUEST_API}/${id}`, { headers });
  }

  /**
   * Get open alerts that need stock requests.
   */
  getOpenAlerts(): Observable<ApiResponse<StockAlert[]>> {
    return this.http.get<ApiResponse<StockAlert[]>>(`${STOCKREQUEST_API}/open-alerts`, {
      headers: this.getAuthHeaders()
    });
  }
}
