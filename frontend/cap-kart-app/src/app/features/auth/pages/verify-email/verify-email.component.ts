import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../../core/services/auth.service';
import { LogoComponent } from '../../../../shared/components/logo/logo.component';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [FormsModule, LogoComponent, CommonModule],
  templateUrl: './verify-email.component.html',
  styleUrl: './verify-email.component.css'
})
export class VerifyEmailComponent implements OnInit {
  email = '';
  otp = ['', '', '', '', '', ''];
  otpArray = new Array(6);
  error = '';
  loading = false;
  codeSent = false; // Track if code has been sent

  // Particle list for left-panel animation (matching auth component)
  particleList = [1,2,3,4,5,6,7,8,9,10,11,12];

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    // Get email from query parameters
    this.email = this.route.snapshot.queryParamMap.get('email') || '';
    
    if (!this.email) {
      // If no email, redirect back to signup
      this.router.navigate(['/signup']);
    }
  }

  sendCode(): void {
    this.error = '';
    this.loading = true;

    this.authService.resendVerification(this.email).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          this.codeSent = true;
        } else {
          this.error = res.message || 'Failed to send code';
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.message || 'Failed to send code';
      }
    });
  }

  onOtpInput(event: any, index: number): void {
    const input = event.target;
    const value = input.value;

    // Only allow digits
    if (value && !/^\d$/.test(value)) {
      this.otp[index] = '';
      return;
    }

    // Auto-focus to next box
    if (value && index < 5) {
      const nextInput = input.parentElement.children[index + 1] as HTMLInputElement;
      if (nextInput) {
        nextInput.focus();
      }
    }
  }

  onKeyDown(event: KeyboardEvent, index: number): void {
    const input = event.target as HTMLInputElement;

    // Handle backspace
    if (event.key === 'Backspace' && !this.otp[index] && index > 0) {
      const prevInput = input.parentElement?.children[index - 1] as HTMLInputElement;
      if (prevInput) {
        prevInput.focus();
      }
    }
  }

  getOtpCode(): string {
    return this.otp.join('');
  }

  verifyEmail(): void {
    this.error = '';

    const code = this.getOtpCode();

    // Validate code
    if (!code || code.length !== 6) {
      this.error = 'Please enter a valid 6-digit code';
      return;
    }

    this.loading = true;

    this.authService.verifyEmail({
      email: this.email,
      code: code
    }).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          // Redirect to login
          this.router.navigate(['/login']);
        } else {
          this.error = res.message || 'Verification failed';
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.message || 'Invalid or expired code';
      }
    });
  }

  resendCode(): void {
    this.error = '';
    this.loading = true;
    // Reset OTP fields
    this.otp = ['', '', '', '', '', ''];

    this.authService.resendVerification(this.email).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          this.codeSent = true;
          // Show success message (you can add a success field if needed)
          this.error = ''; // Clear any errors
        } else {
          this.error = res.message || 'Failed to resend code';
        }
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.message || 'Failed to resend code';
      }
    });
  }

  goToSignup(): void {
    this.router.navigate(['/signup']);
  }
}
