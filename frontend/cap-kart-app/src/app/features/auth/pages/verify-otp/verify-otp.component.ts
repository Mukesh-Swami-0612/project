import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { LogoComponent } from '../../../../shared/components/logo/logo.component';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [FormsModule, LogoComponent, CommonModule],
  templateUrl: './verify-otp.component.html',
  styleUrl: './verify-otp.component.css'
})
export class VerifyOtpComponent implements OnInit {
  email = '';
  otp = ['', '', '', '', '', ''];
  otpArray = new Array(6);
  error = '';

  // Particle list for left-panel animation (matching auth component)
  particleList = [1,2,3,4,5,6,7,8,9,10,11,12];

  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    // Get email from query parameters
    this.email = this.route.snapshot.queryParamMap.get('email') || '';
    
    if (!this.email) {
      // If no email, redirect back to forgot password
      this.router.navigate(['/forgot-password']);
    }
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

  verifyOtp(): void {
    this.error = '';

    const code = this.getOtpCode();

    // Validate code
    if (!code || code.length !== 6) {
      this.error = 'Please enter a valid 6-digit code';
      return;
    }

    // Redirect to reset-password with email and code
    this.router.navigate(['/reset-password'], {
      queryParams: {
        email: this.email,
        code: code
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/forgot-password']);
  }

  resendCode(): void {
    // Navigate back to forgot password with email pre-filled
    this.router.navigate(['/forgot-password'], {
      queryParams: { email: this.email }
    });
  }
}
