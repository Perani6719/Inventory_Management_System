import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class Registration {
  private apiUrl = 'https://localhost:7098/api/staff';

  constructor(private http: HttpClient) {}

  registerStaff(data: any): Observable<any> {
    return this.http.post(this.apiUrl, data);
  }
}
