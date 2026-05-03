import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { API_CONFIG } from '../config/api.config';
import {
  LoginRequest,
  SignupRequest,
  LoginResponse,
  ApiResponse,
  ForgotPasswordRequest
} from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${API_CONFIG.AUTH_API}/auth`;

  constructor(private http: HttpClient, private router: Router) {}

  login(payload: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.baseUrl}/login`, payload, {
      withCredentials: true // Keep for compatibility
    }).pipe(
      tap(res => {
        if (res.success && res.data) {
          // Store tokens and user data in localStorage
          localStorage.setItem('accessToken', res.data.accessToken);
          localStorage.setItem('refreshToken', res.data.refreshToken);
          localStorage.setItem('expiresAt', res.data.expiresAt);
          localStorage.setItem('role', res.data.role);
        }
      })
    );
  }

  signup(payload: SignupRequest): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/signup`, payload, {
      withCredentials: true
    });
  }

  forgotPassword(payload: ForgotPasswordRequest): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/forgot-password`, payload, {
      withCredentials: true
    });
  }

  resetPassword(payload: { email: string; token: string; newPassword: string }): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/reset-password`, payload, {
      withCredentials: true
    });
  }

  verifyEmail(payload: { email: string; code: string }): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/verify-email`, payload, {
      withCredentials: true
    });
  }

  resendVerification(email: string): Observable<ApiResponse<any>> {
    return this.http.post<ApiResponse<any>>(`${this.baseUrl}/resend-verification`, { email }, {
      withCredentials: true
    });
  }

  refreshToken(): Observable<ApiResponse<LoginResponse>> {
    return this.http.post<ApiResponse<LoginResponse>>(`${this.baseUrl}/refresh`, {}, {
      withCredentials: true
    }).pipe(
      tap(res => {
        if (res.success && res.data) {
          // Update tokens in localStorage
          localStorage.setItem('accessToken', res.data.accessToken);
          localStorage.setItem('refreshToken', res.data.refreshToken);
          localStorage.setItem('expiresAt', res.data.expiresAt);
        }
      })
    );
  }

  logout(): void {
    // Call backend logout to revoke tokens
    this.http.post(`${this.baseUrl}/logout`, {}, {
      withCredentials: true
    }).subscribe({
      error: (err) => console.error('Logout error:', err)
    });
    
    // Clear all localStorage data
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('expiresAt');
    localStorage.removeItem('role');
    
    this.router.navigate(['/auth']);
  }

  isLoggedIn(): boolean {
    const expiresAt = localStorage.getItem('expiresAt');
    if (!expiresAt) return false;
    return new Date(expiresAt) > new Date();
  }

  // Get tokens from localStorage
  getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  getRole(): string | null {
    return localStorage.getItem('role');
  }
}
