 
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from './auth.service';
import { Observable } from 'rxjs';

export interface Category {
  categoryId: number;
  categoryName: string;
  description?: string;
  imageUrl?: string;
  createdAt?: string;
}

export interface ApiResponse<T> {
  message: string;
  data: T;
}

const CATEGORY_API_URL = '/api/Category';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  constructor(private http: HttpClient, private auth: AuthService) {}

  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) throw new Error('Authentication token not found.');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  /**
   * @method createCategory
   * Sends a POST request with FormData to create a new category including image.
   * @param formData FormData containing categoryName, description, and image file
   */
  createCategory(formData: FormData): Observable<any> {
    return this.http.post(CATEGORY_API_URL, formData, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * @method updateCategory
   * Sends a PUT request with FormData to update an existing category including image.
   * @param id Category ID
   * @param formData FormData containing updated fields and optional image
   */
  updateCategory(id: number, formData: FormData): Observable<any> {
    return this.http.put(`${CATEGORY_API_URL}/${id}`, formData, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * @method getAllCategories
   * Fetches all categories from the backend.
   */
  getAllCategories(): Observable<Category[] | ApiResponse<Category[]>> {
    return this.http.get<Category[] | ApiResponse<Category[]>>(CATEGORY_API_URL, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * @method deleteCategory
   * Deletes a category by ID with confirmation header.
   */
  deleteCategory(id: number): Observable<any> {
    const headers = this.getAuthHeaders().set('X-Confirm-Delete', 'true');
    return this.http.delete(`${CATEGORY_API_URL}/${id}`, { headers });
  }
}

