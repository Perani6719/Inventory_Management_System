import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';

// Define the expected structure of a stock request
export interface StockRequestApiModel {
  requestId: number;
  storeId: number;
  productId: number;
  productName?: string;
  quantity: number;
  requestDate: string;
  requestedDeliveryDate: string;
  alertId?: number;
  deliveryStatus: string;
  estimatedTimeOfArrival?: string;
}

// Generic API response wrapper
export interface ApiResponse<T> {
  message: string;
  data: T;
}

// ✅ Base API endpoint for warehouse operations
const WAREHOUSE_API_URL = '/api/Warehouse';

/**
 * @Service WarehouseService
 * Handles all API communication related to warehouse stock request operations.
 * Assumes AuthService provides the necessary JWT token.
 */
@Injectable({ providedIn: 'root' })
export class WarehouseService {
  constructor(private http: HttpClient, private auth: AuthService) {}

  /**
   * @method getAuthHeaders
   * Retrieves the JWT token and sets it in the Authorization header.
   * Throws an error if the token is missing.
   */
  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) {
      throw new Error('Authentication token not found.');
    }
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  /**
   * @method getPendingRequests
   * Fetches all pending warehouse requests.
   */
  getPendingRequests(): Observable<ApiResponse<StockRequestApiModel[]>> {
    return this.http.get<ApiResponse<StockRequestApiModel[]>>(`${WAREHOUSE_API_URL}/pending-requests`, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * @method getIncomingRequests
   * Fetches all incoming requests assigned to the warehouse.
   */
  getIncomingRequests(): Observable<ApiResponse<StockRequestApiModel[]>> {
    return this.http.get<ApiResponse<StockRequestApiModel[]>>(`${WAREHOUSE_API_URL}/requests`, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * @method dispatch
   * Marks a request as dispatched with optional ETA.
   * @param requestId The ID of the request
   * @param eta Optional estimated arrival time
   */
  dispatch(requestId: number, eta?: string | null): Observable<any> {
    const body = eta ? { estimatedArrival: eta } : {};
    return this.http.put(`${WAREHOUSE_API_URL}/${requestId}/dispatch`, body, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * @method markDelivered
   * Marks a request as delivered.
   * @param requestId The ID of the request
   */
  markDelivered(requestId: number): Observable<any> {
    return this.http.post(`${WAREHOUSE_API_URL}/${requestId}/mark-delivered`, {}, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * @method cancel
   * Cancels a request with optional reason.
   * @param requestId The ID of the request
   * @param reason Optional cancellation reason
   */
  cancel(requestId: number, reason?: string | null): Observable<any> {
    const body = reason ? { reason } : {};
    return this.http.put(`${WAREHOUSE_API_URL}/${requestId}/cancel`, body, {
      headers: this.getAuthHeaders()
    });
  }
}
