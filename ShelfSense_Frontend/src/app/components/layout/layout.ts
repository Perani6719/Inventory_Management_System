import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Sidebar } from '../dashboard/sidebar/sidebar';
import { Navbar } from '../navbar/navbar';
import { FooterComponent } from '../dashboard/footer/footer';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, Sidebar, Navbar, FooterComponent],
  templateUrl: './layout.html',
  styleUrls: ['./layout.css']
})
export class Layout {
  sidebarOpen = true;

  constructor(private authService: AuthService) {}

  isWarehouse(): boolean {
    return this.authService.getUserRole()?.toLowerCase() === 'warehouse';
  }

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }
}
