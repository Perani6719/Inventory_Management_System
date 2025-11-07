import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from './auth.service';
import { Observable } from 'rxjs';
import { Product } from '../dashboard/add-product/add-product';
 
const PRODUCT_API_URL = '/api/Product';
const CATEGORY_API_URL = '/api/Category';
 
@Injectable({ providedIn: 'root' })
export class ProductService {
  constructor(private http: HttpClient, private auth: AuthService) {}
 
  /**
   * Retrieves the JWT token and sets it in the Authorization header.
   * Throws an error if token is missing.
   */
  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) {
      throw new Error('Authentication token not found.');
    }
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }
 
  /**
   * Creates a new product.
   * @param data Product payload
   */
  createProduct(data: Partial<Product>): Observable<any> {
    // If data is FormData, don't set the Content-Type header so browser sets multipart boundary
    const headers = data instanceof FormData ? this.getAuthHeaders() : this.getAuthHeaders();
    return this.http.post(PRODUCT_API_URL, data as any, { headers });
  }
 
  /**
   * Fetches all products as a raw array.
   */
  getAllProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(PRODUCT_API_URL, { headers: this.getAuthHeaders() });
  }
 
  /**
   * Updates an existing product by ID.
   * @param id Product ID
   * @param data Updated product payload
   */
  updateProduct(id: number, data: Partial<Product>): Observable<any> {
    const headers = data instanceof FormData ? this.getAuthHeaders() : this.getAuthHeaders();
    return this.http.put(`${PRODUCT_API_URL}/${id}`, data as any, { headers });
  }
 
  /**
   * Deletes a product by ID with confirmation header.
   * @param id Product ID
   */
  deleteProduct(id: number): Observable<any> {
    const headers = this.getAuthHeaders().set('X-Confirm-Delete', 'true');
    return this.http.delete(`${PRODUCT_API_URL}/${id}`, { headers });
  }
 
  /**
   * Fetches all categories for dropdown binding.
   */
  getAllCategories(): Observable<any> {
    return this.http.get(CATEGORY_API_URL, { headers: this.getAuthHeaders() });
  }
}
 
 