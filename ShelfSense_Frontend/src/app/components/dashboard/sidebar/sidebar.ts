import { Component, Input, Output, EventEmitter } from '@angular/core';
import { RouterModule, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.css']
})
export class Sidebar {
  @Input() isExpanded = true;
  @Output() toggleSidebar = new EventEmitter<void>();

  showInventory = false;
  showStockRequest = false;
  showAlerts = false;
  showStaffMenu = false;
  showRestockTaskMenu = false;
  role: string | null = null;

  constructor(private auth: AuthService, private router: Router) {
    this.role = this.auth.getUserRole();
  }

  toggleInventory() {
    this.showInventory = !this.showInventory;
  }

  toggleStockRequest() {
    this.showStockRequest = !this.showStockRequest;
  }

  toggleAlerts() {
    this.showAlerts = !this.showAlerts;
  }

  toggleRestockTaskMenu() {
    this.showRestockTaskMenu = !this.showRestockTaskMenu;
  }

  toggleStaffMenu() {
    this.showStaffMenu = !this.showStaffMenu;
  }

  isAdmin() {
    return this.role === 'admin';
  }

  isManager() {
    return this.role === 'manager';
  }

  isStaff() {
    return this.role === 'staff';
  }

  isWarehouse() {
    return this.role === 'warehouse';
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
