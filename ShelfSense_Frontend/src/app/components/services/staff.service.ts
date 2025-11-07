import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from './auth.service';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface Staff {
  staffId: number;
  storeId: number;
  name: string;
  role: 'staff' | 'manager';
  email: string;
  greenPayId?: string;
  // imageUrl?: string;
  createdAt?: string;
}

export interface ApiResponse<T> {
  message: string;
  data: T;
}

// ✅ Replace with your actual backend endpoint
const STAFF_API_URL = '/api/Staff';

@Injectable({ providedIn: 'root' })
export class StaffService {
  constructor(private http: HttpClient, private auth: AuthService) {}

  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) throw new Error('Authentication token not found.');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  /**
   * ✅ Create new staff with image support
   * @param formData FormData containing staff fields and optional image
   */
  createStaff(formData: FormData): Observable<any> {
    return this.http.post(STAFF_API_URL, formData, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * ✅ Update existing staff by ID
   * @param id Staff ID
   * @param formData FormData with updated fields and optional image
   */
  updateStaff(id: number, formData: FormData): Observable<any> {
    return this.http.put(`${STAFF_API_URL}/${id}`, formData, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * ✅ Get all staff records, normalized to Staff[]
   */
  getAllStaff(): Observable<Staff[]> {
    return this.http.get<Staff[] | ApiResponse<Staff[]>>(STAFF_API_URL, {
      headers: this.getAuthHeaders()
    }).pipe(
      map(res => Array.isArray(res) ? res : res.data ?? [])
    );
  }

  /**
   * ✅ Get staff by ID
   */
  getStaffById(id: number): Observable<Staff> {
    return this.http.get<Staff>(`${STAFF_API_URL}/${id}`, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * ✅ Delete staff by ID with confirmation header
   */
  deleteStaff(id: number): Observable<any> {
    const headers = this.getAuthHeaders().set('X-Confirm-Delete', 'true');
    return this.http.delete(`${STAFF_API_URL}/${id}`, { headers });
  }
}
