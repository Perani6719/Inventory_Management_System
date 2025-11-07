import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../../guards/auth-guard';

export interface Alert {
  id: number;
  message: string;
  createdAt: string;
}

export interface DashboardStats {
  totalProducts: number;
  pendingRestocks: number;
  activeStaff: number;
}

@Injectable({ providedIn: 'root' })
export class HomeService {
  private alertsUrl = '/api/alerts';
  private statsUrl = '/api/dashboard/stats';

  constructor(private http: HttpClient, private auth: AuthService) {}

  private getAuthHeaders(): HttpHeaders {
    const token = this.auth.getToken();
    if (!token) throw new Error('Authentication token not found.');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  /**
   * ✅ Fetch recent alerts for the dashboard
   */
  getRecentAlerts(): Observable<Alert[]> {
    return this.http.get<Alert[]>(this.alertsUrl, {
      headers: this.getAuthHeaders()
    });
  }

  /**
   * ✅ Fetch dashboard statistics
   */
  getDashboardStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(this.statsUrl, {
      headers: this.getAuthHeaders()
    });
  }
}
