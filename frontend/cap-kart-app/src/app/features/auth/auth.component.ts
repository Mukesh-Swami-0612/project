import { Component, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { SignupRequest } from '../../core/models/auth.models';
import { LogoComponent } from '../../shared/components/logo/logo.component';

type AuthMode = 'login' | 'signup' | 'forgot';

@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [ReactiveFormsModule, LogoComponent, CommonModule],
  templateUrl: './auth.component.html',
  styleUrl: './auth.component.css'
})
export class AuthComponent implements OnInit {
  mode: AuthMode = 'login';

  loginForm!: FormGroup;
  signupForm!: FormGroup;
  forgotForm!: FormGroup;

  // Particle list for left-panel animation
  particleList = [1,2,3,4,5,6,7,8,9,10,11,12];

  // UI state
  showLoginPassword = false;
  showSignupPassword = false;
  showConfirmPassword = false;

  loginLoading = false;
  signupLoading = false;
  forgotLoading = false;

  loginError = '';
  signupError = '';
  forgotError = '';
  forgotSuccess = '';
  signupSuccess = '';
  showVerifyOption = false; // For unverified email case

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
    // Redirect if already logged in
    if (this.authService.isLoggedIn()) {
      this.navigateAfterLogin();
      return;
    }

    this.buildForms();

    const mode = (this.route.snapshot.data?.['mode'] as AuthMode | undefined) ?? 'login';
    this.mode = mode;
  }

  private buildForms(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });

    this.signupForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(60)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8), this.passwordComplexityValidator]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });

    this.forgotForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });

    // Watch password changes for strength meter
    this.signupForm.get('password')?.valueChanges.subscribe(val => {
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

  // ── Mode switching ──
  setMode(mode: AuthMode): void {
    this.mode = mode;
    this.clearMessages();
  }

  private clearMessages(): void {
    this.loginError = '';
    this.signupError = '';
    this.forgotError = '';
    this.forgotSuccess = '';
    this.signupSuccess = '';
    this.showVerifyOption = false;
  }

  // ── Getters for template ──
  get lf() { return this.loginForm.controls; }
  get sf() { return this.signupForm.controls; }
  get ff() { return this.forgotForm.controls; }

  // ── Login ──
  onLogin(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }
    this.loginLoading = true;
    this.loginError = '';

    this.authService.login(this.loginForm.value).subscribe({
      next: (res) => {
        this.loginLoading = false;
        if (res.success) {
          this.navigateAfterLogin();
        } else {
          this.loginError = res.message || 'Login failed. Please try again.';
        }
      },
      error: (err) => {
        this.loginLoading = false;
        this.loginError = err?.error?.message || 'Invalid email or password.';
      }
    });
  }

  private navigateAfterLogin(): void {
    const role = this.authService.getRole();
    if (role === 'Admin') {
      this.router.navigate(['/admin/dashboard']);
      return;
    }
    this.router.navigate(['/customer/products']);
  }

  // ── Signup ──
  onSignup(): void {
    if (this.signupForm.invalid) {
      this.signupForm.markAllAsTouched();
      return;
    }
    this.signupLoading = true;
    this.signupError = '';
    this.signupSuccess = '';
    this.showVerifyOption = false;

    const payload: SignupRequest = {
      name: this.sf['name'].value,
      email: this.sf['email'].value,
      password: this.sf['password'].value,
      confirmPassword: this.sf['confirmPassword'].value
    };

    this.authService.signup(payload).subscribe({
      next: (res) => {
        this.signupLoading = false;
        if (res.success) {
          // CASE 1: New email → normal signup flow
          this.router.navigate(['/verify-email'], {
            queryParams: { email: this.sf['email'].value }
          });
        } else {
          this.signupError = res.message || 'Registration failed.';
        }
      },
      error: (err) => {
        this.signupLoading = false;
        const errorMessage = err?.error?.message || '';
        
        // CASE 2: Email exists but NOT verified → allow verification flow
        if (errorMessage.toLowerCase().includes('not verified') || 
            errorMessage.toLowerCase().includes('unverified')) {
          this.showVerifyOption = true;
          this.signupError = 'Email already registered but not verified.';
        }
        // CASE 3: Email exists and verified → show error
        else if (errorMessage.toLowerCase().includes('already exists') || 
                 errorMessage.toLowerCase().includes('already registered')) {
          this.signupError = 'Email already exists. Please login.';
          this.showVerifyOption = false;
        }
        // Default error
        else {
          this.signupError = errorMessage || 'Registration failed. Please try again.';
          this.showVerifyOption = false;
        }
      }
    });
  }

  // ── Navigate to Verify Email ──
  goToVerify(): void {
    this.router.navigate(['/verify-email'], {
      queryParams: { email: this.sf['email'].value }
    });
  }

  // ── Forgot Password ──
  onForgotPassword(): void {
    if (this.forgotForm.invalid) {
      this.forgotForm.markAllAsTouched();
      return;
    }
    this.forgotLoading = true;
    this.forgotError = '';
    this.forgotSuccess = '';

    const email = this.forgotForm.value.email;

    this.authService.forgotPassword(this.forgotForm.value).subscribe({
      next: (res) => {
        this.forgotLoading = false;
        // Navigate to verify OTP page with email
        this.router.navigate(['/verify-otp'], {
          queryParams: { email: email }
        });
      },
      error: (err) => {
        this.forgotLoading = false;
        this.forgotError = 'Something went wrong. Please try again.';
      }
    });
  }

  // ── Toggle password visibility ──
  toggleLoginPassword(): void { this.showLoginPassword = !this.showLoginPassword; }
  toggleSignupPassword(): void { this.showSignupPassword = !this.showSignupPassword; }
  toggleConfirmPassword(): void { this.showConfirmPassword = !this.showConfirmPassword; }
}
