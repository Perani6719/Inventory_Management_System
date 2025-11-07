import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
 
// Define interfaces for clarity
export interface RestockTask {
  id: number;
  productId: number;
  shelfId: number;
  staffId: number;
  assignedAt: string;
  status: 'pending' | 'completed' | 'delayed';
}
 
export interface ApiResponse<T> {
  message: string;
  data: T;
}
 
const RESTOCK_API_URL = '/api/RestockTask';
 
@Injectable({ providedIn: 'root' })
export class RestockTaskService {
  constructor(private http: HttpClient, private auth: AuthService) {}
 
  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) {
      throw new Error('Authentication token not found.');
    }
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }
 
  /**
   * Assign tasks automatically from delivered stock.
   * Backend should handle logic and return a message.
   */
  assignTasksFromDeliveredStock(): Observable<string> {
    return this.http.post(`${RESTOCK_API_URL}/assign-tasks`, {}, {
      headers: this.getAuthHeaders(),
      responseType: 'text'
    });
  }
 
  /**
   * Get all restock tasks (for admin/manager view).
   */
  getAllTasks(): Observable<RestockTask[] | ApiResponse<RestockTask[]>> {
    return this.http.get<RestockTask[] | ApiResponse<RestockTask[]>>(`${RESTOCK_API_URL}`, {
      headers: this.getAuthHeaders()
    });
  }
 
  /**
   * Get task by ID (optional, if needed).
   */
  getTaskById(id: number): Observable<RestockTask | ApiResponse<RestockTask>> {
    return this.http.get<RestockTask | ApiResponse<RestockTask>>(`${RESTOCK_API_URL}/${id}`, {
      headers: this.getAuthHeaders()
    });
  }
 
  /**
   * Delete a task (optional, if needed).
   */
  deleteTask(id: number): Observable<any> {
    let headers = this.getAuthHeaders();
    headers = headers.set('X-Confirm-Delete', 'true');
    return this.http.delete(`${RESTOCK_API_URL}/${id}`, { headers });
  }
 
  // Removed getAllRestockTasks() method as AllRestockTasks now uses its own service
 
 
}
 