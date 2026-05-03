import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../config/api.config';

export interface AuditLogFilterDto {
  email?: string;
  action?: string;
  fromDate?: string;
  toDate?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface AuditLogDto {
  id: number;
  userEmail: string;
  action: string;
  details?: string;
  ipAddress?: string;
  timestamp: string;
  success: boolean;
}

export interface AuditLogsResponse {
  total: number;
  page: number;
  pageSize: number;
  data: AuditLogDto[];
}

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly baseUrl = `${API_CONFIG.REPORTING_API}/admin/audit-logs`;

  constructor(private http: HttpClient) {}

  /** GET /api/v1/admin/audit-logs */
  getLogs(filter: AuditLogFilterDto = {}): Observable<AuditLogsResponse> {
    let params = new HttpParams();

    if (filter.email) params = params.set('Email', filter.email);
    if (filter.action) params = params.set('Action', filter.action);
    if (filter.fromDate) params = params.set('FromDate', filter.fromDate);
    if (filter.toDate) params = params.set('ToDate', filter.toDate);
    if (filter.pageNumber) params = params.set('PageNumber', filter.pageNumber.toString());
    if (filter.pageSize) params = params.set('PageSize', filter.pageSize.toString());

    return this.http.get<AuditLogsResponse>(this.baseUrl, { params });
  }

  /** GET /api/v1/admin/audit-logs/export — returns a file download URL */
  getExportUrl(filter: AuditLogFilterDto = {}): string {
    let params = new HttpParams();
    if (filter.email) params = params.set('Email', filter.email);
    if (filter.action) params = params.set('Action', filter.action);
    if (filter.fromDate) params = params.set('FromDate', filter.fromDate);
    if (filter.toDate) params = params.set('ToDate', filter.toDate);
    return `${this.baseUrl}/export?${params.toString()}`;
  }

  /** Export audit logs — triggers direct download */
  exportLogs(filter: AuditLogFilterDto = {}): Observable<Blob> {
    let params = new HttpParams();
    if (filter.email) params = params.set('Email', filter.email);
    if (filter.action) params = params.set('Action', filter.action);
    if (filter.fromDate) params = params.set('FromDate', filter.fromDate);
    if (filter.toDate) params = params.set('ToDate', filter.toDate);

    return this.http.get(`${this.baseUrl}/export`, {
      params,
      responseType: 'blob'
    });
  }
}
