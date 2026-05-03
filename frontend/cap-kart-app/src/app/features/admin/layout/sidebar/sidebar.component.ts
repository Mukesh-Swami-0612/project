import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';

interface MenuItem {
  label: string;
  icon: string;
  route: string;
  badge?: number;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <aside class="sidebar" [class.collapsed]="collapsed">
      <div class="sidebar-header">
        <div class="sidebar-logo" *ngIf="!collapsed">
          <img src="assets/images/logo-optimized.png" alt="CapKart Logo" class="logo-image">
        </div>
        <button class="collapse-btn" (click)="onToggle()" *ngIf="!collapsed">
          <span>☰</span>
        </button>
      </div>

      <nav class="sidebar-nav">
        <a *ngFor="let item of menuItems"
           [routerLink]="item.route"
           routerLinkActive="active"
           class="nav-item"
           [title]="item.label">
          <span class="nav-icon">{{ item.icon }}</span>
          <span class="nav-label" *ngIf="!collapsed">{{ item.label }}</span>
          <span class="badge" *ngIf="item.badge && !collapsed">{{ item.badge }}</span>
        </a>
      </nav>

      <div class="sidebar-footer">
        <button class="nav-item logout-btn" (click)="logout()" [title]="'Logout'">
          <span class="nav-icon">🚪</span>
          <span class="nav-label" *ngIf="!collapsed">Logout</span>
        </button>
      </div>
    </aside>
  `,
  styles: [`
    .sidebar {
      width: 260px;
      background: var(--sidebar-bg);
      color: var(--sidebar-text);
      display: flex;
      flex-direction: column;
      position: fixed;
      left: 0;
      top: 0;
      bottom: 0;
      transition: width 0.3s ease;
      z-index: 1000;
      box-shadow: 2px 0 8px rgba(0,0,0,0.1);
    }

    .sidebar.collapsed {
      width: 70px;
    }

    .sidebar-header {
      padding: 20px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      border-bottom: 1px solid rgba(255, 255, 255, 0.1);
    }

    .sidebar-logo {
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 16px;
    }

    .logo-image {
      max-width: 160px;
      height: auto;
      object-fit: contain;
    }

    .collapse-btn {
      background: transparent;
      border: none;
      color: #94a3b8;
      cursor: pointer;
      padding: 8px;
      border-radius: 6px;
      transition: all 0.2s;
    }

    .collapse-btn:hover {
      background: rgba(255, 255, 255, 0.1);
      color: var(--sidebar-text);
    }

    .sidebar-nav {
      flex: 1;
      padding: 16px 12px;
      overflow-y: auto;
    }

    .nav-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 16px;
      margin-bottom: 4px;
      border-radius: 8px;
      color: #cbd5e1;
      text-decoration: none;
      transition: all 0.2s;
      cursor: pointer;
      border: none;
      background: transparent;
      width: 100%;
      text-align: left;
      font-size: 14px;
      position: relative;
    }

    .nav-item:hover {
      background: rgba(255, 255, 255, 0.1);
      color: #fff;
    }

    .nav-item.active {
      background: linear-gradient(90deg, rgba(37, 99, 235, 0.15), transparent);
      color: #fff;
    }

    .nav-item.active::before {
      content: '';
      position: absolute;
      left: 0;
      top: 0;
      bottom: 0;
      width: 4px;
      background: var(--sidebar-active-indicator);
      border-radius: 0 4px 4px 0;
    }

    .nav-icon {
      font-size: 20px;
      min-width: 24px;
      text-align: center;
    }

    .nav-label {
      flex: 1;
      white-space: nowrap;
    }

    .badge {
      background: var(--error);
      color: white;
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 11px;
      font-weight: 600;
    }

    .sidebar-footer {
      padding: 16px 12px;
      border-top: 1px solid rgba(255, 255, 255, 0.1);
    }

    .logout-btn {
      color: #f87171;
    }

    .logout-btn:hover {
      background: rgba(239, 68, 68, 0.1);
      color: #fca5a5;
    }

    .collapsed .nav-label,
    .collapsed .badge {
      display: none;
    }

    .collapsed .nav-item {
      justify-content: center;
      padding: 12px;
    }

    @media (max-width: 768px) {
      .sidebar {
        transform: translateX(-100%);
      }
      
      .sidebar:not(.collapsed) {
        transform: translateX(0);
      }
    }
  `]
})
export class SidebarComponent {
  @Input() collapsed = false;
  @Output() toggleSidebar = new EventEmitter<void>();

  private authService = inject(AuthService);
  private router = inject(Router);

  menuItems: MenuItem[] = [
    { label: 'Dashboard', icon: '📊', route: '/admin/dashboard' },
    { label: 'Users', icon: '👥', route: '/admin/users' },
    { label: 'Products', icon: '📦', route: '/admin/products' },
    { label: 'Orders', icon: '🛍️', route: '/admin/orders' },
    { label: 'Approvals', icon: '✅', route: '/admin/approvals', badge: 5 },
    { label: 'Analytics', icon: '📈', route: '/admin/analytics' },
    { label: 'Audit Logs', icon: '📜', route: '/admin/audit' },
    { label: 'Reports', icon: '📋', route: '/admin/reports' },
    { label: 'Settings', icon: '⚙️', route: '/admin/settings' }
  ];

  onToggle(): void {
    this.toggleSidebar.emit();
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}
