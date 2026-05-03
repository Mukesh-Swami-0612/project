import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../config/api.config';

export interface DashboardSummary {
  totalProducts: number;
  publishedProducts: number;
  pendingApprovals: number;
  rejectedProducts: number;
  totalNotifications: number;
  totalWorkflows: number;
  recentActivity?: any[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly baseUrl = `${API_CONFIG.REPORTING_API}/reports/dashboard`;

  constructor(private http: HttpClient) {}

  /** GET /api/v1/reports/dashboard */
  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(this.baseUrl);
  }
}
