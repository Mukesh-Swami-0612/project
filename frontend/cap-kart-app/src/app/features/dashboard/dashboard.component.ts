import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CommonModule } from '@angular/common';
import { LogoComponent } from '../../shared/components/logo/logo.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, LogoComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);

  userEmail = '';
  userRole = '';

  ngOnInit(): void {
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/auth']);
      return;
    }
    
    const token = this.authService.getAccessToken();
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        this.userEmail = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress']
          || payload['email'] || '';
      } catch {}
    }
    this.userRole = this.authService.getRole() || 'User';
  }

  logout(): void {
    this.authService.logout();
  }
}
