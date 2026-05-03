import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ToastService } from '../../../../core/services/toast.service';
import { UserService } from '../../../../core/services/user.service';

interface User {
  id: string;
  name: string;
  email: string;
  role: string;
  status: 'Active' | 'Inactive';
  createdAt: string;
  selected?: boolean;
}

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="users-page">
      <div class="page-header">
        <div>
          <h1>User Management</h1>
          <p>Manage users, roles, and permissions</p>
        </div>
        <button class="btn-primary" (click)="openAddUserModal()">
          <span>➕</span> Add User
        </button>
      </div>

      <!-- Filters -->
      <div class="filters-section">
        <div class="search-box">
          <span class="search-icon">🔍</span>
          <input 
            type="text" 
            placeholder="Search by name or email..." 
            [(ngModel)]="searchQuery"
            (input)="applyFilters()"
          />
        </div>

        <select class="filter-select" [(ngModel)]="roleFilter" (change)="applyFilters()">
          <option value="">All Roles</option>
          <option value="Admin">Admin</option>
          <option value="Customer">Customer</option>
          <option value="Vendor">Vendor</option>
        </select>

        <select class="filter-select" [(ngModel)]="statusFilter" (change)="applyFilters()">
          <option value="">All Status</option>
          <option value="Active">Active</option>
          <option value="Inactive">Inactive</option>
        </select>

        <button class="btn-secondary" (click)="resetFilters()">
          <span>🔄</span> Reset
        </button>
      </div>

      <!-- Stats -->
      <div class="stats-row">
        <div class="stat-item">
          <span class="stat-label">Total Users</span>
          <span class="stat-value">{{ users.length }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">Active</span>
          <span class="stat-value text-success">{{ getActiveCount() }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">Inactive</span>
          <span class="stat-value text-danger">{{ getInactiveCount() }}</span>
        </div>
      </div>

      <!-- Table -->
      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>
                <input type="checkbox" (change)="toggleSelectAll($event)" />
              </th>
              <th>Name</th>
              <th>Email</th>
              <th>Role</th>
              <th>Status</th>
              <th>Created At</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let user of paginatedUsers">
              <td>
                <input type="checkbox" [(ngModel)]="user.selected" />
              </td>
              <td>
                <div class="user-cell">
                  <div class="user-avatar">{{ getInitial(user.name) }}</div>
                  <span class="user-name">{{ user.name }}</span>
                </div>
              </td>
              <td>{{ user.email }}</td>
              <td>
                <select 
                  class="role-select" 
                  [(ngModel)]="user.role"
                  (change)="updateUserRole(user)"
                >
                  <option value="Admin">Admin</option>
                  <option value="Customer">Customer</option>
                  <option value="Vendor">Vendor</option>
                </select>
              </td>
              <td>
                <span class="status-badge" [class]="'status-' + user.status.toLowerCase()">
                  {{ user.status }}
                </span>
              </td>
              <td>{{ formatDate(user.createdAt) }}</td>
              <td>
                <div class="action-buttons">
                  <button 
                    class="btn-icon" 
                    [title]="user.status === 'Active' ? 'Deactivate' : 'Activate'"
                    (click)="toggleUserStatus(user)"
                  >
                    {{ user.status === 'Active' ? '🔒' : '🔓' }}
                  </button>
                  <button class="btn-icon" title="Edit" (click)="editUser(user)">
                    ✏️
                  </button>
                  <button class="btn-icon text-danger" title="Delete" (click)="deleteUser(user)">
                    🗑️
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>

        <div class="empty-state" *ngIf="paginatedUsers.length === 0">
          <span class="empty-icon">👥</span>
          <h3>No users found</h3>
          <p>Try adjusting your filters or add a new user</p>
        </div>
      </div>

      <!-- Bulk Actions -->
      <div class="bulk-actions" *ngIf="getSelectedCount() > 0">
        <span class="selected-count">{{ getSelectedCount() }} selected</span>
        <button class="btn-secondary" (click)="bulkActivate()">
          <span>✅</span> Activate
        </button>
        <button class="btn-secondary" (click)="bulkDeactivate()">
          <span>🔒</span> Deactivate
        </button>
        <button class="btn-danger" (click)="bulkDelete()">
          <span>🗑️</span> Delete
        </button>
      </div>

      <!-- Pagination -->
      <div class="pagination">
        <button class="page-btn" [disabled]="currentPage === 1" (click)="previousPage()">
          Previous
        </button>
        <span class="page-info">Page {{ currentPage }} of {{ totalPages }}</span>
        <button class="page-btn" [disabled]="currentPage === totalPages" (click)="nextPage()">
          Next
        </button>
      </div>
    </div>
  `,
  styles: [`
    .users-page {
      max-width: 1400px;
      margin: 0 auto;
    }

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 32px;
    }

    .page-header h1 {
      font-size: 32px;
      font-weight: 700;
      color: #1e293b;
      margin: 0 0 8px 0;
    }

    .page-header p {
      color: #64748b;
      margin: 0;
      font-size: 14px;
    }

    .btn-primary {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px 24px;
      background: linear-gradient(90deg, #2563eb, #22c55e);
      color: white;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
    }

    .btn-primary:hover {
      background: #4338ca;
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(79, 70, 229, 0.3);
    }

    .filters-section {
      display: flex;
      gap: 12px;
      margin-bottom: 24px;
      flex-wrap: wrap;
    }

    .search-box {
      flex: 1;
      min-width: 300px;
      display: flex;
      align-items: center;
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 10px 16px;
    }

    .search-icon {
      margin-right: 8px;
      color: #94a3b8;
    }

    .search-box input {
      border: none;
      outline: none;
      flex: 1;
      font-size: 14px;
    }

    .filter-select {
      padding: 10px 16px;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      background: white;
      font-size: 14px;
      cursor: pointer;
    }

    .btn-secondary {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 20px;
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      color: #475569;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
      transition: all 0.2s;
    }

    .btn-secondary:hover {
      background: #f8fafc;
      border-color: #cbd5e1;
    }

    .stats-row {
      display: flex;
      gap: 24px;
      margin-bottom: 24px;
    }

    .stat-item {
      background: white;
      padding: 20px;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .stat-label {
      font-size: 13px;
      color: #64748b;
    }

    .stat-value {
      font-size: 28px;
      font-weight: 700;
      color: #1e293b;
    }

    .text-success {
      color: #10b981;
    }

    .text-danger {
      color: #ef4444;
    }

    .table-container {
      background: white;
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      margin-bottom: 24px;
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;
    }

    .data-table thead {
      background: #f8fafc;
      border-bottom: 2px solid #e2e8f0;
    }

    .data-table th {
      padding: 16px;
      text-align: left;
      font-size: 13px;
      font-weight: 600;
      color: #475569;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .data-table td {
      padding: 16px;
      border-bottom: 1px solid #f1f5f9;
      font-size: 14px;
      color: #1e293b;
    }

    .data-table tbody tr:hover {
      background: #f8fafc;
    }

    .user-cell {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .user-avatar {
      width: 36px;
      height: 36px;
      border-radius: 50%;
      background: linear-gradient(90deg, #2563eb, #22c55e);
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 600;
      font-size: 14px;
    }

    .user-name {
      font-weight: 500;
    }

    .role-select {
      padding: 6px 12px;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      font-size: 13px;
      cursor: pointer;
      background: white;
    }

    .status-badge {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 600;
    }

    .status-badge.status-active {
      background: #dcfce7;
      color: #16a34a;
    }

    .status-badge.status-inactive {
      background: #fee2e2;
      color: #dc2626;
    }

    .action-buttons {
      display: flex;
      gap: 8px;
    }

    .btn-icon {
      background: transparent;
      border: none;
      font-size: 16px;
      cursor: pointer;
      padding: 6px;
      border-radius: 4px;
      transition: all 0.2s;
    }

    .btn-icon:hover {
      background: #f1f5f9;
    }

    .empty-state {
      text-align: center;
      padding: 60px 20px;
    }

    .empty-icon {
      font-size: 64px;
      display: block;
      margin-bottom: 16px;
    }

    .empty-state h3 {
      font-size: 20px;
      color: #1e293b;
      margin: 0 0 8px 0;
    }

    .empty-state p {
      color: #64748b;
      margin: 0;
    }

    .bulk-actions {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 16px;
      background: white;
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.1);
      margin-bottom: 24px;
    }

    .selected-count {
      font-weight: 600;
      color: var(--primary-blue);
      margin-right: auto;
    }

    .btn-danger {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 20px;
      background: #ef4444;
      color: white;
      border: none;
      border-radius: 8px;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
      transition: all 0.2s;
    }

    .btn-danger:hover {
      background: #dc2626;
    }

    .pagination {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 16px;
    }

    .page-btn {
      padding: 8px 16px;
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 6px;
      cursor: pointer;
      font-size: 14px;
      transition: all 0.2s;
    }

    .page-btn:hover:not(:disabled) {
      background: #f8fafc;
      border-color: #cbd5e1;
    }

    .page-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .page-info {
      font-size: 14px;
      color: #64748b;
    }

    @media (max-width: 768px) {
      .filters-section {
        flex-direction: column;
      }

      .search-box {
        min-width: 100%;
      }

      .stats-row {
        flex-direction: column;
      }

      .table-container {
        overflow-x: auto;
      }
    }
  `]
})
export class AdminUsersComponent implements OnInit {
  private userService = inject(UserService);
  private toast = inject(ToastService);

  users: User[] = [];
  filteredUsers: User[] = [];
  searchQuery = '';
  roleFilter = '';
  statusFilter = '';
  currentPage = 1;
  pageSize = 10;
  totalPages = 1;
  loading = false;
  errorMessage = '';

  get paginatedUsers(): User[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredUsers.slice(start, start + this.pageSize);
  }

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.errorMessage = '';

    this.userService.getAllUsers().subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success && res.data) {
          this.users = res.data.map(u => ({
            id: String(u.id),
            name: u.fullName || `${u.firstName} ${u.lastName}`.trim(),
            email: u.email,
            role: u.role,
            status: u.isActive ? 'Active' : 'Inactive',
            createdAt: u.createdAt
          }));
          this.applyFilters();
        }
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = 'Failed to load users. Please try again.';
        console.error('Error loading users:', err);
      }
    });
  }

  applyFilters(): void {
    this.filteredUsers = this.users.filter(user => {
      const matchesSearch = !this.searchQuery ||
        user.name.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
        user.email.toLowerCase().includes(this.searchQuery.toLowerCase());
      const matchesRole = !this.roleFilter || user.role === this.roleFilter;
      const matchesStatus = !this.statusFilter || user.status === this.statusFilter;
      return matchesSearch && matchesRole && matchesStatus;
    });
    this.currentPage = 1;
    this.totalPages = Math.ceil(this.filteredUsers.length / this.pageSize) || 1;
  }

  resetFilters(): void {
    this.searchQuery = '';
    this.roleFilter = '';
    this.statusFilter = '';
    this.applyFilters();
  }

  getInitial(name: string): string {
    return name.charAt(0).toUpperCase();
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  getActiveCount(): number {
    return this.users.filter(u => u.status === 'Active').length;
  }

  getInactiveCount(): number {
    return this.users.filter(u => u.status === 'Inactive').length;
  }

  getSelectedCount(): number {
    return this.users.filter(u => u.selected).length;
  }

  toggleSelectAll(event: any): void {
    const checked = event.target.checked;
    this.paginatedUsers.forEach(user => user.selected = checked);
  }

  updateUserRole(user: User): void {
    const roleIdMap: Record<string, number> = {
      Admin: 1, Customer: 2, Vendor: 3, ProductManager: 4, ContentExecutive: 5
    };
    const roleId = roleIdMap[user.role];
    if (!roleId) return;

    this.userService.changeUserRole(Number(user.id), roleId).subscribe({
      next: () => console.log(`Role updated to ${user.role} for user ${user.id}`),
      error: (err) => {
        console.error('Failed to update role:', err);
        alert('Failed to update user role.');
        this.loadUsers(); // revert
      }
    });
  }

  toggleUserStatus(user: User): void {
    const newStatus = user.status === 'Active' ? false : true;
    this.userService.updateUserStatus(Number(user.id), newStatus).subscribe({
      next: () => {
        user.status = newStatus ? 'Active' : 'Inactive';
        this.toast.show(`User ${user.name} ${newStatus ? 'activated' : 'deactivated'} successfully.`, 'success');
      },
      error: (err) => {
        console.error('Failed to update status:', err);
      }
    });
  }

  editUser(user: User): void {
    this.toast.show('Edit user functionality coming soon.', 'info');
  }

  deleteUser(user: User): void {
    if (confirm(`Are you sure you want to delete ${user.name}? This action cannot be undone.`)) {
      this.userService.deleteUser(Number(user.id)).subscribe({
        next: () => {
          this.users = this.users.filter(u => u.id !== user.id);
          this.applyFilters();
        },
        error: (err) => {
          console.error('Failed to delete user:', err);
          alert('Failed to delete user.');
        }
      });
    }
  }

  openAddUserModal(): void {
    this.toast.show('Please register new users via the Auth signup flow.', 'info');
  }

  bulkActivate(): void {
    const selected = this.users.filter(u => u.selected);
    if (selected.length === 0) return;

    this.loading = true;
    const requests = selected.map(u => this.userService.updateUserStatus(Number(u.id), true));
    
    forkJoin(requests).subscribe({
      next: () => {
        selected.forEach(u => { u.status = 'Active'; u.selected = false; });
        this.loading = false;
        this.toast.show(`Activated ${selected.length} users.`, 'success');
      },
      error: (err) => {
        this.loading = false;
        console.error('Bulk activate failed:', err);
      }
    });
  }

  bulkDeactivate(): void {
    const selected = this.users.filter(u => u.selected);
    if (selected.length === 0) return;

    this.loading = true;
    const requests = selected.map(u => this.userService.updateUserStatus(Number(u.id), false));
    
    forkJoin(requests).subscribe({
      next: () => {
        selected.forEach(u => { u.status = 'Inactive'; u.selected = false; });
        this.loading = false;
        this.toast.show(`Deactivated ${selected.length} users.`, 'success');
      },
      error: (err) => {
        this.loading = false;
        console.error('Bulk deactivate failed:', err);
      }
    });
  }

  bulkDelete(): void {
    const selected = this.users.filter(u => u.selected);
    if (selected.length === 0) return;

    if (confirm(`Delete ${selected.length} users? This action cannot be undone.`)) {
      this.loading = true;
      const requests = selected.map(u => this.userService.deleteUser(Number(u.id)));
      
      forkJoin(requests).subscribe({
        next: () => {
          this.users = this.users.filter(u => !u.selected);
          this.applyFilters();
          this.loading = false;
          this.toast.show(`Deleted ${selected.length} users.`, 'success');
        },
        error: (err) => {
          this.loading = false;
          console.error('Bulk delete failed:', err);
        }
      });
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) this.currentPage--;
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) this.currentPage++;
  }
}

