import { Component, EventEmitter, Output, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <header class="topbar">
      <div class="topbar-left">
        <button class="menu-toggle" (click)="onToggleSidebar()">
          <span>☰</span>
        </button>
        
        <div class="search-box">
          <span class="search-icon">🔍</span>
          <input 
            type="text" 
            placeholder="Search products, orders, users..." 
            [(ngModel)]="searchQuery"
            (keyup.enter)="onSearch()"
          />
        </div>
      </div>

      <div class="topbar-right">
        <button class="icon-btn" [class.has-badge]="notifications > 0" (click)="toggleNotifications()">
          <span>🔔</span>
          <span class="badge" *ngIf="notifications > 0">{{ notifications }}</span>
        </button>

        <div class="user-menu" (click)="toggleUserMenu()">
          <div class="user-avatar">{{ userInitial }}</div>
          <div class="user-info">
            <div class="user-name">{{ userName }}</div>
            <div class="user-role">{{ userRole }}</div>
          </div>
          <span class="dropdown-icon">▼</span>
        </div>

        <div class="dropdown-menu" *ngIf="showUserMenu">
          <a class="dropdown-item" (click)="navigateTo('/admin/profile')">
            <span>👤</span> Profile
          </a>
          <a class="dropdown-item" (click)="navigateTo('/admin/settings')">
            <span>⚙️</span> Settings
          </a>
          <div class="dropdown-divider"></div>
          <a class="dropdown-item logout" (click)="logout()">
            <span>🚪</span> Logout
          </a>
        </div>

        <div class="notification-panel" *ngIf="showNotifications">
          <div class="notification-header">
            <h3>Notifications</h3>
            <button class="mark-read">Mark all as read</button>
          </div>
          <div class="notification-list">
            <div class="notification-item" *ngFor="let notif of notificationList">
              <div class="notif-icon">{{ notif.icon }}</div>
              <div class="notif-content">
                <div class="notif-title">{{ notif.title }}</div>
                <div class="notif-time">{{ notif.time }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </header>
  `,
  styles: [`
    .topbar {
      height: 70px;
      background: white;
      border-bottom: 1px solid #e5e7eb;
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 24px;
      position: sticky;
      top: 0;
      z-index: 100;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }

    .topbar-left {
      display: flex;
      align-items: center;
      gap: 20px;
      flex: 1;
    }

    .menu-toggle {
      background: transparent;
      border: none;
      font-size: 24px;
      cursor: pointer;
      padding: 8px;
      border-radius: 6px;
      color: #64748b;
      transition: all 0.2s;
    }

    .menu-toggle:hover {
      background: #f1f5f9;
      color: #1e293b;
    }

    .search-box {
      display: flex;
      align-items: center;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 8px 16px;
      max-width: 500px;
      flex: 1;
      transition: all 0.2s;
    }

    .search-box:focus-within {
      background: white;
      border-color: var(--primary-blue);
      box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.1);
    }

    .search-icon {
      margin-right: 8px;
      color: #94a3b8;
    }

    .search-box input {
      border: none;
      background: transparent;
      outline: none;
      flex: 1;
      font-size: 14px;
      color: #1e293b;
    }

    .search-box input::placeholder {
      color: #94a3b8;
    }

    .topbar-right {
      display: flex;
      align-items: center;
      gap: 16px;
      position: relative;
    }

    .icon-btn {
      position: relative;
      background: transparent;
      border: none;
      font-size: 20px;
      cursor: pointer;
      padding: 10px;
      border-radius: 8px;
      color: #64748b;
      transition: all 0.2s;
    }

    .icon-btn:hover {
      background: #f1f5f9;
      color: #1e293b;
    }

    .icon-btn .badge {
      position: absolute;
      top: 6px;
      right: 6px;
      background: #ef4444;
      color: white;
      font-size: 10px;
      padding: 2px 6px;
      border-radius: 10px;
      font-weight: 600;
    }

    .user-menu {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 8px 12px;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s;
    }

    .user-menu:hover {
      background: #f1f5f9;
    }

    .user-avatar {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: linear-gradient(90deg, #2563eb, #22c55e);
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 600;
      font-size: 16px;
    }

    .user-info {
      display: flex;
      flex-direction: column;
    }

    .user-name {
      font-size: 14px;
      font-weight: 600;
      color: #1e293b;
    }

    .user-role {
      font-size: 12px;
      color: #64748b;
    }

    .dropdown-icon {
      font-size: 10px;
      color: #94a3b8;
    }

    .dropdown-menu {
      position: absolute;
      top: 60px;
      right: 0;
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      box-shadow: 0 10px 25px rgba(0,0,0,0.1);
      min-width: 200px;
      z-index: 1000;
    }

    .dropdown-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 12px 16px;
      color: #1e293b;
      text-decoration: none;
      cursor: pointer;
      transition: all 0.2s;
      font-size: 14px;
    }

    .dropdown-item:hover {
      background: #f8fafc;
    }

    .dropdown-item.logout {
      color: #ef4444;
    }

    .dropdown-item.logout:hover {
      background: #fef2f2;
    }

    .dropdown-divider {
      height: 1px;
      background: #e5e7eb;
      margin: 4px 0;
    }

    .notification-panel {
      position: absolute;
      top: 60px;
      right: 60px;
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 8px;
      box-shadow: 0 10px 25px rgba(0,0,0,0.1);
      width: 360px;
      max-height: 500px;
      overflow: hidden;
      z-index: 1000;
    }

    .notification-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 16px;
      border-bottom: 1px solid #e5e7eb;
    }

    .notification-header h3 {
      margin: 0;
      font-size: 16px;
      font-weight: 600;
      color: #1e293b;
    }

    .mark-read {
      background: transparent;
      border: none;
      color: var(--primary-blue);
      font-size: 12px;
      cursor: pointer;
      padding: 4px 8px;
      border-radius: 4px;
    }

    .mark-read:hover {
      background: rgba(37, 99, 235, 0.1);
    }

    .notification-list {
      max-height: 400px;
      overflow-y: auto;
    }

    .notification-item {
      display: flex;
      gap: 12px;
      padding: 16px;
      border-bottom: 1px solid #f1f5f9;
      cursor: pointer;
      transition: all 0.2s;
    }

    .notification-item:hover {
      background: #f8fafc;
    }

    .notif-icon {
      font-size: 24px;
    }

    .notif-content {
      flex: 1;
    }

    .notif-title {
      font-size: 14px;
      color: #1e293b;
      margin-bottom: 4px;
    }

    .notif-time {
      font-size: 12px;
      color: #94a3b8;
    }

    @media (max-width: 768px) {
      .search-box {
        display: none;
      }

      .user-info {
        display: none;
      }
    }
  `]
})
export class TopbarComponent implements OnInit {
  @Output() toggleSidebar = new EventEmitter<void>();

  private authService = inject(AuthService);
  private router = inject(Router);

  searchQuery = '';
  notifications = 5;
  showUserMenu = false;
  showNotifications = false;
  userName = 'Admin User';
  userRole = 'Administrator';
  userInitial = 'A';

  notificationList = [
    { icon: '✅', title: 'New product approval pending', time: '5 min ago' },
    { icon: '🛍️', title: '10 new orders received', time: '15 min ago' },
    { icon: '👤', title: 'New user registration', time: '1 hour ago' },
    { icon: '📊', title: 'Weekly report is ready', time: '2 hours ago' },
    { icon: '⚠️', title: 'Low stock alert for 3 products', time: '3 hours ago' }
  ];

  ngOnInit(): void {
    const token = this.authService.getAccessToken();
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const email = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || payload['email'] || '';
        this.userName = email.split('@')[0] || 'Admin';
        this.userInitial = this.userName.charAt(0).toUpperCase();
      } catch {}
    }
    this.userRole = this.authService.getRole() || 'Administrator';
  }

  onToggleSidebar(): void {
    this.toggleSidebar.emit();
  }

  onSearch(): void {
    if (this.searchQuery.trim()) {
      console.log('Searching for:', this.searchQuery);
      // Implement global search functionality
    }
  }

  toggleUserMenu(): void {
    this.showUserMenu = !this.showUserMenu;
    this.showNotifications = false;
  }

  toggleNotifications(): void {
    this.showNotifications = !this.showNotifications;
    this.showUserMenu = false;
  }

  navigateTo(route: string): void {
    this.showUserMenu = false;
    this.router.navigate([route]);
  }

  logout(): void {
    this.showUserMenu = false;
    this.authService.logout();
  }
}
