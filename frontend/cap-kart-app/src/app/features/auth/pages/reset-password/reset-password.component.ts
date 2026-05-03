import { Component, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { LogoComponent } from '../../../../shared/components/logo/logo.component';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, LogoComponent],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.css'
})
export class ResetPasswordComponent implements OnInit {
  resetForm!: FormGroup;

  // Particle list for left-panel animation (matching auth component)
  particleList = [1,2,3,4,5,6,7,8,9,10,11,12];

  // UI state
  showPassword = false;
  showConfirmPassword = false;
  loading = false;
  error = '';
  success = '';

  passwordStrength = 0;
  passwordStrengthLabel = '';
  passwordStrengthClass = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.buildForm();

    // Pre-fill email and code from URL query parameters
    const email = this.route.snapshot.queryParamMap.get('email');
    const code = this.route.snapshot.queryParamMap.get('code');
    
    if (email) {
      this.resetForm.patchValue({ email });
      // Disable email field after setting value
      this.resetForm.get('email')?.disable();
    }
    
    if (code) {
      this.resetForm.patchValue({ code });
    }
  }

  private buildForm(): void {
    this.resetForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      code: [''], // Hidden field, no validation needed
      password: ['', [Validators.required, Validators.minLength(8), this.passwordComplexityValidator]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });

    // Watch password changes for strength meter
    this.resetForm.get('password')?.valueChanges.subscribe(val => {
      this.updatePasswordStrength(val || '');
    });
  }

  private passwordComplexityValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value as string;
    if (!value) return null;
    const hasUpper = /[A-Z]/.test(value);
    const hasLower = /[a-z]/.test(value);
    const hasDigit = /\d/.test(value);
    const hasSpecial = /[!@#$%^&*(),.?":{}|<>]/.test(value);
    if (!hasUpper || !hasLower || !hasDigit || !hasSpecial) {
      return { complexity: true };
    }
    return null;
  }

  private passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const password = group.get('password')?.value;
    const confirm = group.get('confirmPassword')?.value;
    if (confirm && password !== confirm) {
      group.get('confirmPassword')?.setErrors({ mismatch: true });
      return { mismatch: true };
    }
    if (group.get('confirmPassword')?.hasError('mismatch')) {
      group.get('confirmPassword')?.setErrors(null);
    }
    return null;
  }

  private updatePasswordStrength(password: string): void {
    let score = 0;
    if (password.length >= 8) score++;
    if (password.length >= 12) score++;
    if (/[A-Z]/.test(password)) score++;
    if (/[a-z]/.test(password)) score++;
    if (/\d/.test(password)) score++;
    if (/[!@#$%^&*(),.?":{}|<>]/.test(password)) score++;

    this.passwordStrength = Math.min(Math.round((score / 6) * 100), 100);

    if (score <= 1) {
      this.passwordStrengthLabel = 'Very Weak';
      this.passwordStrengthClass = 'very-weak';
    } else if (score <= 2) {
      this.passwordStrengthLabel = 'Weak';
      this.passwordStrengthClass = 'weak';
    } else if (score <= 3) {
      this.passwordStrengthLabel = 'Fair';
      this.passwordStrengthClass = 'fair';
    } else if (score <= 4) {
      this.passwordStrengthLabel = 'Good';
      this.passwordStrengthClass = 'good';
    } else {
      this.passwordStrengthLabel = 'Strong';
      this.passwordStrengthClass = 'strong';
    }
  }

  // Getters for template
  get f() { return this.resetForm.controls; }

  onResetPassword(): void {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.error = '';
    this.success = '';

    const payload = {
      email: this.resetForm.get('email')?.value, // Get value directly from control
      token: this.f['code'].value,
      newPassword: this.f['password'].value
    };

    this.authService.resetPassword(payload).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          this.success = 'Password reset successful! Redirecting to login...';
          this.resetForm.reset();
          setTimeout(() => this.router.navigate(['/login']), 2000);
        } else {
          this.error = res.message || 'Password reset failed.';
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.message || 'Invalid code or error. Please try again.';
      }
    });
  }

  togglePassword(): void { this.showPassword = !this.showPassword; }
  toggleConfirmPassword(): void { this.showConfirmPassword = !this.showConfirmPassword; }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  changeEmail(): void {
    this.router.navigate(['/forgot-password']);
  }
}
