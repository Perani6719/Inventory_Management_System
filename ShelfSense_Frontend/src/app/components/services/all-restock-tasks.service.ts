import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from './auth.service';
import { Observable } from 'rxjs';
 
export interface RestockTask {
  taskId: number;
  productId: number;
  shelfId: number;
  assignedTo: number;
  assignedAt: string;
  status: string;
  quantityRestocked: number;
  completedAt?: string;
  alertId?: number;
}
 
const RESTOCK_TASK_API_URL = '/api/RestockTask';
 
@Injectable({ providedIn: 'root' })
export class AllRestockTasksService {
  constructor(private http: HttpClient, private auth: AuthService) {}
 
  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) {
      throw new Error('Authentication token not found.');
    }
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }
 
  getAllRestockTasks(): Observable<RestockTask[]> {
    return this.http.get<RestockTask[]>(RESTOCK_TASK_API_URL, { headers: this.getAuthHeaders() });
  }
}
 