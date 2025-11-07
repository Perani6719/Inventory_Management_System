import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
 
const ORGANIZE_API_URL = '/api/RestockTask/organize-product';
 
@Injectable({
  providedIn: 'root'
})
export class OrganizeProductService {
  constructor(private http: HttpClient, private auth: AuthService) {}
 
  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) {
      throw new Error('Authentication token not found.');
    }
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }
 
  /**
   * Organize products for a given staff and task.
   * @param staffId - ID of the staff member
   * @param taskId - ID of the restock task
   */
  organizeProduct(staffId: number, taskId: number): Observable<string> {
  const url = `${ORGANIZE_API_URL}?staffId=${staffId}&taskId=${taskId}`;
  return this.http.post(url, {}, {
    headers: this.getAuthHeaders(),
    responseType: 'text'
  });
}
}
 