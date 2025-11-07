import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
 
export interface ApiResponse<T> {
  message: string;
  data: T;
}
 
@Injectable({ providedIn: 'root' })
export class DepletionService {
  private endpoint = '/api/ProductShelf/predict-depletion';
 
  constructor(private http: HttpClient, private auth: AuthService) {}
 
  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) throw new Error('Authentication token not found.');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }
 
  getPredictions(): Observable<ApiResponse<any[]>> {
    return this.http.get<ApiResponse<any[]>>(this.endpoint, { headers: this.getAuthHeaders() });
  }
}
 
 