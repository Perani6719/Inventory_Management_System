import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { WarehouseRequestsComponent } from './warehouse-requests';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service'; // adjust path if needed

@Component({
  selector: 'app-warehouse-dashboard',
  standalone: true,
  imports: [CommonModule, WarehouseRequestsComponent],
  templateUrl: './warehouse-dashboard.html',
  styleUrls: ['./warehouse-dashboard.css']
})
export class WarehouseDashboardComponent {
  constructor(private authService: AuthService, private router: Router) {}

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']); // ✅ Redirect to login route
  }
}
