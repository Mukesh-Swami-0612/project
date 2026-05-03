import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { roleMatchGuard } from '../../core/guards/role.guard';

export const CUSTOMER_ROUTES: Routes = [
  {
    path: '',
    canMatch: [authGuard, roleMatchGuard(['User', 'ProductManager', 'ContentExecutive', 'Admin'])],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'products' },
      {
        path: 'products',
        loadComponent: () =>
          import('./pages/customer-products/customer-products.component').then(m => m.CustomerProductsComponent)
      },
      {
        path: 'products/:id',
        loadComponent: () =>
          import('./pages/customer-product-detail/customer-product-detail.component').then(m => m.CustomerProductDetailComponent)
      },
      {
        path: 'cart-preview',
        loadComponent: () =>
          import('./pages/customer-cart-preview/customer-cart-preview.component').then(m => m.CustomerCartPreviewComponent)
      },
      {
        path: 'order-history',
        loadComponent: () =>
          import('./pages/customer-order-history/customer-order-history.component').then(m => m.CustomerOrderHistoryComponent)
      }
    ]
  }
];

