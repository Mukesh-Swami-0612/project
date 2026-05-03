import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandlerFn, HttpEvent, HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

// 🔥 GLOBAL FLAG: Prevent multiple simultaneous refresh token calls
let isRefreshing = false;

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> => {
  const authService = inject(AuthService);

  // 🔥 EXCLUDE AUTH ENDPOINTS - These should never trigger refresh logic
  const excludedUrls = [
    '/auth/login',
    '/auth/signup',
    '/auth/register',
    '/auth/forgot-password',
    '/auth/reset-password',
    '/auth/verify-email',
    '/auth/resend-verification',
    '/auth/refresh'
  ];

  const isExcluded = excludedUrls.some(url => req.url.includes(url));
  const isRefreshRequest = req.url.includes('/auth/refresh');

  // 🔥 CRITICAL: Don't add token to excluded URLs (login, signup, etc.)
  const accessToken = authService.getAccessToken();
  const authReq = isExcluded ? req.clone({ withCredentials: true }) : req.clone({
    withCredentials: true,
    setHeaders: accessToken ? {
      Authorization: `Bearer ${accessToken}`
    } : {}
  });

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // 🔥 HANDLE 429 FIRST - Stop retry immediately
      if (error.status === 429) {
        console.error('Too many requests - stopping retry');
        return throwError(() => error);
      }

      // 🔥 HANDLE 401 - Only refresh if NOT excluded and NOT already refreshing
      if (error.status === 401 && !isExcluded && !isRefreshing) {
        isRefreshing = true;
        return authService.refreshToken().pipe(
          switchMap(() => {
            isRefreshing = false;
            // ✅ FIX: Get the NEW token and retry with it
            const newToken = authService.getAccessToken();
            const retryReq = req.clone({
              withCredentials: true,
              setHeaders: newToken ? {
                Authorization: `Bearer ${newToken}`
              } : {}
            });
            return next(retryReq);
          }),
          catchError(refreshError => {
            isRefreshing = false;
            authService.logout();
            return throwError(() => refreshError);
          })
        );
      }

      // 🔥 HANDLE 401 on refresh endpoint - Logout immediately
      if (error.status === 401 && isRefreshRequest) {
        isRefreshing = false;
        authService.logout();
      }

      return throwError(() => error);
    })
  );
};
