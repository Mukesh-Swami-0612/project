import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { roleMatchGuard, roleGuard } from '../../core/guards/role.guard';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    canMatch: [authGuard, roleMatchGuard(['Admin'])],
    canActivate: [authGuard, roleGuard(['Admin'])],
    loadComponent: () => import('./layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/admin-dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent)
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./pages/admin-users/admin-users.component').then(m => m.AdminUsersComponent)
      },
      {
        path: 'products',
        loadComponent: () =>
          import('./pages/admin-products/admin-products.component').then(m => m.AdminProductsComponent)
      },
      {
        path: 'products/:id',
        loadComponent: () =>
          import('./pages/admin-product-detail/admin-product-detail.component').then(m => m.AdminProductDetailComponent)
      },
      {
        path: 'orders',
        loadComponent: () =>
          import('./pages/admin-orders/admin-orders.component').then(m => m.AdminOrdersComponent)
      },
      {
        path: 'orders/:id',
        loadComponent: () =>
          import('./pages/admin-order-detail/admin-order-detail.component').then(m => m.AdminOrderDetailComponent)
      },
      {
        path: 'approvals',
        loadComponent: () =>
          import('./pages/admin-approvals/admin-approvals.component').then(m => m.AdminApprovalsComponent)
      },
      {
        path: 'analytics',
        loadComponent: () =>
          import('./pages/admin-analytics/admin-analytics.component').then(m => m.AdminAnalyticsComponent)
      },
      {
        path: 'audit',
        loadComponent: () =>
          import('./pages/admin-audit/admin-audit.component').then(m => m.AdminAuditComponent)
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./pages/admin-reports/admin-reports.component').then(m => m.AdminReportsComponent)
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./pages/admin-settings/admin-settings.component').then(m => m.AdminSettingsComponent)
      }
    ]
  }
];

