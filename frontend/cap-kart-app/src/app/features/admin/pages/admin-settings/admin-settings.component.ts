import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface Settings {
  // General
  applicationName: string;
  adminEmail: string;
  
  // Notifications
  enableEmailNotifications: boolean;
  enableSystemAlerts: boolean;
  
  // Security
  enable2FA: boolean;
  sessionTimeout: number;
}

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="settings-page">
      <div class="page-header">
        <div>
          <h1>Settings</h1>
          <p>Configure system preferences and security options</p>
        </div>
      </div>

      <!-- Success Message -->
      <div class="success-banner" *ngIf="showSuccessMessage">
        <span class="success-icon">✅</span>
        <div class="success-content">
          <strong>Settings saved successfully!</strong>
          <p>Your changes have been applied.</p>
        </div>
      </div>

      <!-- Settings Form -->
      <div class="settings-container">
        
        <!-- General Settings -->
        <div class="settings-card">
          <div class="card-header">
            <div class="header-icon">⚙️</div>
            <div>
              <h2>General Settings</h2>
              <p>Basic application configuration</p>
            </div>
          </div>
          <div class="card-body">
            <div class="form-group">
              <label for="appName">Application Name</label>
              <input
                id="appName"
                type="text"
                class="form-input"
                [(ngModel)]="settings.applicationName"
                placeholder="Enter application name"
              />
              <span class="form-hint">The name displayed across the application</span>
            </div>

            <div class="form-group">
              <label for="adminEmail">Admin Email</label>
              <input
                id="adminEmail"
                type="email"
                class="form-input"
                [(ngModel)]="settings.adminEmail"
                placeholder="admin@example.com"
              />
              <span class="form-hint">Primary contact email for system notifications</span>
            </div>
          </div>
        </div>

        <!-- Notification Settings -->
        <div class="settings-card">
          <div class="card-header">
            <div class="header-icon">🔔</div>
            <div>
              <h2>Notification Settings</h2>
              <p>Manage notification preferences</p>
            </div>
          </div>
          <div class="card-body">
            <div class="toggle-group">
              <div class="toggle-item">
                <div class="toggle-info">
                  <div class="toggle-label">Email Notifications</div>
                  <div class="toggle-description">Receive email alerts for important events</div>
                </div>
                <label class="toggle-switch">
                  <input
                    type="checkbox"
                    [(ngModel)]="settings.enableEmailNotifications"
                  />
                  <span class="toggle-slider"></span>
                </label>
              </div>

              <div class="toggle-item">
                <div class="toggle-info">
                  <div class="toggle-label">System Alerts</div>
                  <div class="toggle-description">Show in-app notifications for system events</div>
                </div>
                <label class="toggle-switch">
                  <input
                    type="checkbox"
                    [(ngModel)]="settings.enableSystemAlerts"
                  />
                  <span class="toggle-slider"></span>
                </label>
              </div>
            </div>
          </div>
        </div>

        <!-- Security Settings -->
        <div class="settings-card">
          <div class="card-header">
            <div class="header-icon">🔒</div>
            <div>
              <h2>Security Settings</h2>
              <p>Configure security and authentication options</p>
            </div>
          </div>
          <div class="card-body">
            <div class="toggle-group">
              <div class="toggle-item">
                <div class="toggle-info">
                  <div class="toggle-label">Two-Factor Authentication (2FA)</div>
                  <div class="toggle-description">Add an extra layer of security to your account</div>
                </div>
                <label class="toggle-switch">
                  <input
                    type="checkbox"
                    [(ngModel)]="settings.enable2FA"
                  />
                  <span class="toggle-slider"></span>
                </label>
              </div>
            </div>

            <div class="form-group">
              <label for="sessionTimeout">Session Timeout</label>
              <select
                id="sessionTimeout"
                class="form-select"
                [(ngModel)]="settings.sessionTimeout"
              >
                <option [value]="15">15 minutes</option>
                <option [value]="30">30 minutes</option>
                <option [value]="60">60 minutes</option>
              </select>
              <span class="form-hint">Automatically log out after period of inactivity</span>
            </div>
          </div>
        </div>

        <!-- Action Buttons -->
        <div class="actions-bar">
          <button class="btn-secondary" (click)="resetSettings()">
            <span>🔄</span> Reset to Defaults
          </button>
          <button class="btn-primary" (click)="saveSettings()">
            <span>💾</span> Save Settings
          </button>
        </div>

      </div>
    </div>
  `,
  styles: [`
    .settings-page {
      max-width: 1000px;
      margin: 0 auto;
    }

    .page-header {
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

    .success-banner {
      display: flex;
      align-items: flex-start;
      gap: 16px;
      padding: 16px 20px;
      background: #dcfce7;
      border: 1px solid #86efac;
      border-radius: 12px;
      margin-bottom: 24px;
      animation: slideDown 0.3s ease-out;
    }

    @keyframes slideDown {
      from {
        opacity: 0;
        transform: translateY(-10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .success-icon {
      font-size: 24px;
    }

    .success-content {
      flex: 1;
    }

    .success-content strong {
      display: block;
      color: #166534;
      font-size: 15px;
      margin-bottom: 4px;
    }

    .success-content p {
      color: #15803d;
      font-size: 13px;
      margin: 0;
    }

    .settings-container {
      display: flex;
      flex-direction: column;
      gap: 24px;
    }

    .settings-card {
      background: white;
      border-radius: 12px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
      overflow: hidden;
    }

    .card-header {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 24px;
      background: #f8fafc;
      border-bottom: 1px solid #e2e8f0;
    }

    .header-icon {
      font-size: 32px;
      line-height: 1;
    }

    .card-header h2 {
      font-size: 20px;
      font-weight: 600;
      color: #1e293b;
      margin: 0 0 4px 0;
    }

    .card-header p {
      font-size: 13px;
      color: #64748b;
      margin: 0;
    }

    .card-body {
      padding: 24px;
    }

    .form-group {
      margin-bottom: 24px;
    }

    .form-group:last-child {
      margin-bottom: 0;
    }

    label {
      display: block;
      font-size: 14px;
      font-weight: 600;
      color: #475569;
      margin-bottom: 8px;
    }

    .form-input,
    .form-select {
      width: 100%;
      padding: 12px 16px;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      font-size: 14px;
      color: #1e293b;
      background: white;
      transition: all 0.2s;
      box-sizing: border-box;
    }

    .form-input:focus,
    .form-select:focus {
      outline: none;
      border-color: var(--primary-blue);
      box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.1);
    }

    .form-hint {
      display: block;
      font-size: 12px;
      color: #94a3b8;
      margin-top: 6px;
    }

    .toggle-group {
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .toggle-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 16px;
      padding: 16px;
      background: #f8fafc;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
    }

    .toggle-info {
      flex: 1;
    }

    .toggle-label {
      font-size: 15px;
      font-weight: 600;
      color: #1e293b;
      margin-bottom: 4px;
    }

    .toggle-description {
      font-size: 13px;
      color: #64748b;
    }

    .toggle-switch {
      position: relative;
      display: inline-block;
      width: 52px;
      height: 28px;
      flex-shrink: 0;
    }

    .toggle-switch input {
      opacity: 0;
      width: 0;
      height: 0;
    }

    .toggle-slider {
      position: absolute;
      cursor: pointer;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background-color: #cbd5e1;
      transition: 0.3s;
      border-radius: 28px;
    }

    .toggle-slider:before {
      position: absolute;
      content: "";
      height: 20px;
      width: 20px;
      left: 4px;
      bottom: 4px;
      background-color: white;
      transition: 0.3s;
      border-radius: 50%;
    }

    input:checked + .toggle-slider {
      background-color: var(--primary-green);
    }

    input:checked + .toggle-slider:before {
      transform: translateX(24px);
    }

    .actions-bar {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
      padding: 24px;
      background: white;
      border-radius: 12px;
      box-shadow: 0 1px 3px rgba(0,0,0,0.05);
    }

    .btn-primary,
    .btn-secondary {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px 24px;
      border: none;
      border-radius: 8px;
      font-weight: 600;
      cursor: pointer;
      font-size: 14px;
      transition: all 0.2s;
    }

    .btn-primary {
      background: linear-gradient(90deg, #2563eb, #22c55e);
      color: white;
    }

    .btn-primary:hover {
      background: #4338ca;
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(79, 70, 229, 0.3);
    }

    .btn-secondary {
      background: white;
      color: #475569;
      border: 1px solid #e2e8f0;
    }

    .btn-secondary:hover {
      background: #f8fafc;
      border-color: #cbd5e1;
    }

    @media (max-width: 768px) {
      .card-header {
        flex-direction: column;
        align-items: flex-start;
      }

      .toggle-item {
        flex-direction: column;
        align-items: flex-start;
      }

      .toggle-switch {
        align-self: flex-end;
      }

      .actions-bar {
        flex-direction: column;
      }

      .btn-primary,
      .btn-secondary {
        width: 100%;
        justify-content: center;
      }
    }
  `]
})
export class AdminSettingsComponent {
  showSuccessMessage = false;

  settings: Settings = {
    applicationName: 'CapKart Admin',
    adminEmail: 'admin@capkart.com',
    enableEmailNotifications: true,
    enableSystemAlerts: true,
    enable2FA: false,
    sessionTimeout: 30
  };

  private defaultSettings: Settings = {
    applicationName: 'CapKart Admin',
    adminEmail: 'admin@capkart.com',
    enableEmailNotifications: true,
    enableSystemAlerts: true,
    enable2FA: false,
    sessionTimeout: 30
  };

  saveSettings(): void {
    // Mock save - no backend call
    this.showSuccessMessage = true;
    
    // Hide success message after 3 seconds
    setTimeout(() => {
      this.showSuccessMessage = false;
    }, 3000);

    console.log('Settings saved (mock):', this.settings);
  }

  resetSettings(): void {
    if (confirm('Are you sure you want to reset all settings to defaults?')) {
      this.settings = { ...this.defaultSettings };
      console.log('Settings reset to defaults');
    }
  }
}
