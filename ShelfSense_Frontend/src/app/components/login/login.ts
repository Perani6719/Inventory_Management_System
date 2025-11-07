import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup, FormControl } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { AuthService } from '../services/auth.service';
import { ToastrModule, ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    HttpClientModule,
    RouterModule,
    ToastrModule
  ],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class Login implements OnInit {
  loginForm: FormGroup<{
    email: FormControl<string>;
    password: FormControl<string>;
  }>;

  isLoadingOverlay = false;
  showPassword = false;
  showSplash = true;
  showLoginForm = false;

  gradientStyle: { [key: string]: string } = {
    ['background']: 'radial-gradient(circle at center, #e8f5e9, #c8e6c9, #a5d6a7)',
    ['min-height']: '100vh',
    ['display']: 'flex',
    ['justify-content']: 'center',
    ['align-items']: 'center'
  };

  private gradientColors = [
    'radial-gradient(circle at center, #e8f5e9, #c8e6c9, #a5d6a7)',
    'radial-gradient(circle at center, #fff3e0, #ffe0b2, #ffcc80)',
    'radial-gradient(circle at center, #e3f2fd, #bbdefb, #90caf9)',
    'radial-gradient(circle at center, #fce4ec, #f8bbd0, #f48fb1)'
  ];

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private toastr: ToastrService
  ) {
    this.loginForm = this.fb.group({
      email: this.fb.control('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
      password: this.fb.control('', { nonNullable: true, validators: [Validators.required, Validators.minLength(6)] })
    });
  }

  ngOnInit(): void {
    setTimeout(() => {
      this.showSplash = false;
      this.showLoginForm = true;
    }, 2500);

    let index = 0;
    setInterval(() => {
      this.gradientStyle['background'] = this.gradientColors[index];
      index = (index + 1) % this.gradientColors.length;
    }, 3000);
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    if (this.loginForm.valid) {
      this.isLoadingOverlay = true;
      const { email, password } = this.loginForm.getRawValue();

      this.authService.login({ email, password }).subscribe({
        next: (res) => {
          this.authService.storeToken(res.token);
          this.authService.storeRefreshToken(res.refreshToken);
          this.authService.startTokenMonitor(
            () => this.toastr.info('⚠️ Session expiring soon!', 'Heads up'),
            () => window.location.reload()
          );

          const role = this.authService.getUserRole()?.toLowerCase();
          this.toastr.success(`Welcome back! Role: ${role}`, 'Login Successful');

          setTimeout(() => {
            switch (role) {
              case 'admin':
              case 'manager':
              case 'staff':
                this.router.navigate(['/home']);
                break;
              case 'warehouse':
                this.router.navigate(['/warehouse-dashboard']);
                break;
              default:
                this.toastr.error('Unauthorized role detected.', 'Access Denied');
                this.router.navigate(['/login']);
                break;
            }

            this.loginForm.reset();
            this.isLoadingOverlay = false;
          }, 1000);
        },
        error: (err) => {
          this.toastr.error(err.error?.message || 'Login failed. Try again.', 'Error');
          this.isLoadingOverlay = false;
        }
      });
    } else {
      this.toastr.error('Please correct the highlighted errors.', 'Validation Failed');
    }
  }

  handleRegisterClick(event: Event): void {
    event.preventDefault();
    this.toastr.info('Please login first to access registration.', 'Info');
  }

  get f() {
    return this.loginForm.controls;
  }
}

 