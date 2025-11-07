import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  Validators,
  AbstractControl,
  ValidationErrors,
  FormGroup
} from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { ToastrModule, ToastrService } from 'ngx-toastr';
import { Registration } from '../services/registration';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    ToastrModule
  ],
  templateUrl: './register.html',
  styleUrls: ['./register.css']
})
export class Register implements OnInit {
  registerForm!: FormGroup;
  isLoadingOverlay = false;
  showPassword = false;
  showConfirmPassword = false;

  constructor(
    private fb: FormBuilder,
    private registrationService: Registration,
    private toastr: ToastrService,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const role = this.authService.getUserRole();
    if (!['admin', 'manager'].includes(role ?? '')) {
      this.toastr.error('Access denied. Only admin or manager can register staff.', 'Unauthorized');
      this.router.navigate(['/landing']);
      return;
    }

    this.registerForm = this.fb.group({
      storeId: [null, [Validators.required]],
      name: ['', [Validators.required, Validators.maxLength(100)]],
      role: ['staff', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      passwordHash: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordsMatch });
  }

  passwordsMatch(group: AbstractControl): ValidationErrors | null {
    const password = group.get('passwordHash')?.value;
    const confirm = group.get('confirmPassword')?.value;
    return password && confirm && password === confirm ? null : { mismatch: true };
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  onSubmit(): void {
    if (this.registerForm.valid) {
      this.isLoadingOverlay = true;

      const formData = {
        storeId: this.registerForm.get('storeId')!.value,
        name: this.registerForm.get('name')!.value,
        role: this.registerForm.get('role')!.value,
        email: this.registerForm.get('email')!.value,
        passwordHash: this.registerForm.get('passwordHash')!.value
      };

      this.registrationService.registerStaff(formData).subscribe({
        next: () => {
          this.toastr.success('Staff registered successfully!', 'Success');
          setTimeout(() => {
            this.registerForm.reset();
            this.isLoadingOverlay = false;
            this.router.navigate(['/login']);
          }, 1000);
        },
        error: (err: { status: number }) => {
          this.isLoadingOverlay = false;
          if (err.status === 409) {
            this.toastr.error('Email already exists.', 'Conflict');
          } else if (err.status === 400) {
            this.toastr.error('Validation failed.', 'Bad Request');
          } else {
            this.toastr.error('Something went wrong.', 'Server Error');
          }
          console.error('❌ Registration failed:', err);
        }
      });
    } else {
      this.toastr.error('Please correct the highlighted errors.', 'Validation Failed');
    }
  }

  get f(): { [key: string]: AbstractControl } {
    return this.registerForm.controls;
  }
}
