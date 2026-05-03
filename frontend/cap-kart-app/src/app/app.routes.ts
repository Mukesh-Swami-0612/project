import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/auth.component').then(m => m.AuthComponent),
    data: { mode: 'login' }
  },
  {
    path: 'signup',
    loadComponent: () => import('./features/auth/auth.component').then(m => m.AuthComponent),
    data: { mode: 'signup' }
  },

  {
    path: 'forgot-password',
    loadComponent: () => import('./features/auth/auth.component').then(m => m.AuthComponent),
    data: { mode: 'forgot' }
  },

  {
    path: 'verify-otp',
    loadComponent: () => import('./features/auth/pages/verify-otp/verify-otp.component').then(m => m.VerifyOtpComponent)
  },

  {
    path: 'verify-email',
    loadComponent: () => import('./features/auth/pages/verify-email/verify-email.component').then(m => m.VerifyEmailComponent)
  },

  {
    path: 'reset-password',
    loadComponent: () => import('./features/auth/pages/reset-password/reset-password.component').then(m => m.ResetPasswordComponent)
  },

  // PRD route hierarchy
  { path: 'customer', loadChildren: () => import('./features/customer/customer.routes').then(m => m.CUSTOMER_ROUTES) },
  { path: 'admin', loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES) },

  // Backward compatibility
  { path: 'dashboard', pathMatch: 'full', redirectTo: 'admin/dashboard' },

  { path: '**', redirectTo: 'login' }
];
