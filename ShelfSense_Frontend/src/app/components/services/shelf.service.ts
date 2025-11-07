import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
// Assuming 'auth.service' is correctly located in the same directory or accessible path
import { AuthService } from './auth.service';

const SHELF_API_URL = '/api/Shelf';
const CATEGORY_API_URL = '/api/Category';

@Injectable({ providedIn: 'root' })
export class ShelfService {
  constructor(private http: HttpClient, private auth: AuthService) {}

  /**
   * Retrieves the JWT token and sets it in the Authorization header.
   * Throws an error if token is missing.
   */
  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) {
      // In a real application, you might redirect to the login page or handle this more gracefully
      throw new Error('Authentication token not found.');
    }
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  /**
   * Creates a new shelf.
   * @param data Shelf payload
   */
  createShelf(data: any): Observable<any> {
    return this.http.post(SHELF_API_URL, data, { headers: this.getAuthHeaders() });
  }

  /**
   * Fetches all shelves.
   */
  getAllShelves(): Observable<any> {
    return this.http.get(SHELF_API_URL, { headers: this.getAuthHeaders() });
  }

  /**
   * Updates an existing shelf by ID.
   * @param id Shelf ID
   * @param data Updated shelf payload
   */
  updateShelf(id: number, data: any): Observable<any> {
    return this.http.put(`${SHELF_API_URL}/${id}`, data, { headers: this.getAuthHeaders() });
  }

  /**
   * Deletes a shelf by ID with confirmation header.
   * @param id Shelf ID
   */
  deleteShelf(id: number): Observable<any> {
    const headers = this.getAuthHeaders().set('X-Confirm-Delete', 'true');
    return this.http.delete(`${SHELF_API_URL}/${id}`, { headers });
  }

  /**
   * Fetches all categories for dropdown binding.
   * NOTE: This assumes the API returns an array of Category objects.
   */
  getAllCategories(): Observable<any> {
    return this.http.get(CATEGORY_API_URL, { headers: this.getAuthHeaders() });
  }
}
