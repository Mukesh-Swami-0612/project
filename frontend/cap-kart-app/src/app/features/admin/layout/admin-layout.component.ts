import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar/sidebar.component';
import { TopbarComponent } from './topbar/topbar.component';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, SidebarComponent, TopbarComponent],
  template: `
    <div class="admin-layout">
      <app-sidebar [collapsed]="sidebarCollapsed" (toggleSidebar)="toggleSidebar()"></app-sidebar>
      <div class="main-content" [class.expanded]="sidebarCollapsed">
        <app-topbar (toggleSidebar)="toggleSidebar()"></app-topbar>
        <div class="content-area">
          <router-outlet></router-outlet>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .admin-layout {
      display: flex;
      height: 100vh;
      overflow: hidden;
      background: #f5f7fa;
    }

    .main-content {
      flex: 1;
      display: flex;
      flex-direction: column;
      margin-left: 260px;
      transition: margin-left 0.3s ease;
      overflow: hidden;
    }

    .main-content.expanded {
      margin-left: 70px;
    }

    .content-area {
      flex: 1;
      overflow-y: auto;
      padding: 24px;
    }

    @media (max-width: 768px) {
      .main-content {
        margin-left: 0;
      }
    }
  `]
})
export class AdminLayoutComponent {
  sidebarCollapsed = false;

  toggleSidebar(): void {
    this.sidebarCollapsed = !this.sidebarCollapsed;
  }
}
