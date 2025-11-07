import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css']
})
export class Navbar {
  userRole: string | null = null;
  imageUrl: string = 'splash.png';
  navMenuOpen = false;

  constructor(private authService: AuthService) {
    this.userRole = this.authService.getUserRole();
  }

  toggleNavMenu() {
    this.navMenuOpen = !this.navMenuOpen;
  }
}
