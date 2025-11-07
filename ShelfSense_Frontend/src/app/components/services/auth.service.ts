// import { Injectable } from '@angular/core';
// import { HttpClient } from '@angular/common/http';
// import { Observable } from 'rxjs';

// @Injectable({
//   providedIn: 'root'
// })
// export class AuthService {
//   private apiUrl = 'https://localhost:7098/api/auth/login';

//   constructor(private http: HttpClient) {}

//   login(credentials: { email: string; password: string }): Observable<any> {
//     return this.http.post(this.apiUrl, credentials);
//   }

//   storeToken(token: string) {
//     localStorage.setItem('jwtToken', token);
//   }

//   storeRefreshToken(refreshToken: string) {
//     localStorage.setItem('refreshToken', refreshToken);
//   }

//   getToken(): string | null {
//     return localStorage.getItem('jwtToken');
//   }

//   getUserRole(): string | null {
//     const token = this.getToken();
//     if (!token) return null;

//     const payload = JSON.parse(atob(token.split('.')[1]));
//     return payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
//   }

//   logout() {
//     localStorage.clear();
//   }
// }

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval, Subscription } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'https://localhost:7098/api/auth';
  private refreshSubscription: Subscription | null = null;

  constructor(private http: HttpClient) {}

  // 🔐 Login API
  login(credentials: { email: string; password: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, credentials);
  }

  // 🔄 Refresh token API
  refreshToken(): Observable<any> {
    const refreshToken = this.getRefreshToken();
    return this.http.post(`${this.apiUrl}/refresh`, { refreshToken });
  }

  // 🗂️ Store access token and expiry
  storeToken(token: string): void {
    localStorage.setItem('jwtToken', token);
    const expiry = this.getTokenExpiry(token);
    localStorage.setItem('tokenExpiry', expiry.toString());
  }

  // 🗂️ Store refresh token
  storeRefreshToken(refreshToken: string): void {
    localStorage.setItem('refreshToken', refreshToken);
  }

  // 🔍 Get access token
  getToken(): string | null {
    return localStorage.getItem('jwtToken');
  }

  // 🔍 Get refresh token
  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  // 🧠 Decode JWT payload
  getDecodedTokenPayload(): Record<string, any> | null {
    try {
      const token = this.getToken();
      if (token) {
        const payload = JSON.parse(atob(token.split('.')[1]));
        return payload;
      }
      return null;
    } catch (e) {
      console.error('❌ Invalid token format:', e);
      return null;
    }
  }

  // 🔍 Extract role from token
  getUserRole(): string | null {
    const payload = this.getDecodedTokenPayload();
    return payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? null;
  }

  // ⏳ Extract expiry timestamp from token
  getTokenExpiry(token: string): number {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return payload.exp * 1000; // Convert to milliseconds
  }

  // 🧭 Start session monitor
  startTokenMonitor(onWarning: () => void, onRefresh: () => void): void {
    this.stopTokenMonitor();

    this.refreshSubscription = interval(1000).subscribe(() => {
      const expiry = Number(localStorage.getItem('tokenExpiry'));
      const now = Date.now();
      const timeLeft = expiry - now;

      // ⚠️ Warn 2 minutes before expiry
      if (timeLeft <= 120000 && timeLeft > 119000) {
        onWarning();
      }

      // 🔄 Refresh when expired
      if (timeLeft <= 0) {
        this.refreshToken().subscribe({
          next: (res) => {
            this.storeToken(res.token);
            this.storeRefreshToken(res.refreshToken);
            onRefresh(); // Reload or update UI
          },
          error: () => {
            console.error('Token refresh failed.');
          }
        });
      }
    });
  }

  // 🛑 Stop session monitor
  stopTokenMonitor(): void {
    this.refreshSubscription?.unsubscribe();
    this.refreshSubscription = null;
  }

  // 🚪 Logout and cleanup
  logout(): void {
    localStorage.clear();
    this.stopTokenMonitor();
  }
}
