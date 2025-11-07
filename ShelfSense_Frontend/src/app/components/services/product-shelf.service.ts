import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from './auth.service';
import { Observable, throwError } from 'rxjs';
import { catchError, retryWhen, scan, delay } from 'rxjs/operators';
 
export interface ProductShelfEntry {
  maxCapacity: any;
  productShelfId: number;
  productId: number;
  shelfId: number;
  quantity: number;
  lastRestockedAt: string;
  // optional human-friendly names populated by UI after fetching
  productName?: string;
  shelfName?: string;
}
 
export interface ProductShelfCreateRequest {
  productId: number;
  shelfId: number;
  quantity: number;
}
 
export interface ProductShelfAutoAssignRequest {
  productId: number;
  categoryId: number;
  initialQuantity: number;
}
 
export interface ProductShelfResponse {
  message: string;
  data: ProductShelfEntry[];
}
 
@Injectable({ providedIn: 'root' })
export class ProductShelfService {
  private readonly API_URL = '/api/ProductShelf';
 
  constructor(private http: HttpClient, private auth: AuthService) {}
 
  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) throw new Error('Authentication token not found.');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }
 
  /**
   * Generic retry strategy with fixed backoff.
   * @param maxRetry maximum retry attempts
   * @param backoffMs delay between retries in ms
   */
  private retryStrategy(maxRetry = 2, backoffMs = 1000) {
    return (attempts: Observable<any>) => attempts.pipe(
      scan((retryCount, err) => {
        if (retryCount >= maxRetry) {
          throw err;
        }
        return retryCount + 1;
      }, 0),
      delay(backoffMs)
    );
  }
 
  private handleError<T>(operation = 'operation') {
    return (error: any): Observable<T> => {
      // Log to console for developers; could also send to remote logging endpoint
      console.error(`${operation} failed:`, error);
 
      // Create friendly message
      const msg = error?.error?.message || error?.message || 'Server error';
      return throwError(() => new Error(`${operation} failed: ${msg}`));
    };
  }
 
  getAll(): Observable<ProductShelfResponse> {
    return this.http.get<ProductShelfResponse>(this.API_URL, { headers: this.getAuthHeaders() })
      .pipe(
        retryWhen(this.retryStrategy(3, 1000)), // retry GET up to 3 times with 1s backoff
        catchError(this.handleError<ProductShelfResponse>('ProductShelfService.getAll'))
      );
  }
 
  autoAssign(data: ProductShelfAutoAssignRequest): Observable<any> {
    // POST is not strictly idempotent - avoid aggressive retry. Provide error handling.
    return this.http.post(`${this.API_URL}/auto-assign`, data, { headers: this.getAuthHeaders() })
      .pipe(
        catchError(this.handleError<any>('ProductShelfService.autoAssign'))
      );
  }
 
  update(id: number, data: ProductShelfCreateRequest): Observable<any> {
    return this.http.put(`${this.API_URL}/${id}`, data, { headers: this.getAuthHeaders() })
      .pipe(
        retryWhen(this.retryStrategy(1, 1000)), // allow a single retry for PUT
        catchError(this.handleError<any>('ProductShelfService.update'))
      );
  }
 
  delete(id: number): Observable<any> {
    const headers = this.getAuthHeaders().set('X-Confirm-Delete', 'true');
    return this.http.delete(`${this.API_URL}/${id}`, { headers })
      .pipe(
        retryWhen(this.retryStrategy(1, 1000)),
        catchError(this.handleError<any>('ProductShelfService.delete'))
      );
  }
}
 
 