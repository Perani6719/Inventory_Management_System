// import { Component, signal } from '@angular/core';
// import { RouterOutlet } from '@angular/router';
// import { Sidebar } from "./components/dashboard/sidebar/sidebar";


// @Component({
//   selector: 'app-root',
//   standalone: true,
//   imports: [RouterOutlet, Sidebar],
//   templateUrl: './app.html',
//   styleUrl: './app.css'
// })
// export class App {
//   protected readonly title = signal('ShelfSense_UI');
// }

import { Component, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from './components/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly title = signal('ShelfSense_UI');

  constructor(
    private authService: AuthService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    const token = this.authService.getToken();
    if (token) {
      this.authService.startTokenMonitor(
        () => this.toastr.info('⚠️ Your session will expire soon.', 'Heads up'),
        () => window.location.reload()
      );
    }
  }
}
