import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../config/api.config';

export interface ReportQuery {
  from?: string;
  to?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface ReportData {
  totalCount: number;
  [key: string]: any;
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private apiUrl = `${API_CONFIG.REPORTING_API}/reports`;

  constructor(private http: HttpClient) {}

  private buildParams(query: ReportQuery): HttpParams {
    let params = new HttpParams();
    if (query.from) params = params.set('From', query.from);
    if (query.to) params = params.set('To', query.to);
    if (query.pageNumber) params = params.set('PageNumber', query.pageNumber.toString());
    if (query.pageSize) params = params.set('PageSize', query.pageSize.toString());
    return params;
  }

  // ── PRODUCT REPORTS ───────────────────────────────────────────────────────────

  getProductReport(query: ReportQuery = {}): Observable<ReportData> {
    return this.http.get<ReportData>(`${this.apiUrl}/products`, { params: this.buildParams(query) });
  }

  getProductTrends(query: ReportQuery = {}): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/products/trends`, { params: this.buildParams(query) });
  }

  getProductRejections(query: ReportQuery = {}): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/products/rejections`, { params: this.buildParams(query) });
  }

  getTopRejectedProducts(query: ReportQuery = {}): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/products/top-rejections`, { params: this.buildParams(query) });
  }

  // ── WORKFLOW REPORTS ──────────────────────────────────────────────────────────

  getWorkflowReport(query: ReportQuery = {}): Observable<ReportData> {
    return this.http.get<ReportData>(`${this.apiUrl}/workflows`, { params: this.buildParams(query) });
  }

  getWorkflowTrends(query: ReportQuery = {}): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/workflows/trends`, { params: this.buildParams(query) });
  }

  getWorkflowFailures(query: ReportQuery = {}): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/workflows/failures`, { params: this.buildParams(query) });
  }

  getWorkflowPerformance(query: ReportQuery = {}): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/workflows/performance`, { params: this.buildParams(query) });
  }

  // ── NOTIFICATION REPORTS ──────────────────────────────────────────────────────

  getNotificationReport(query: ReportQuery = {}): Observable<ReportData> {
    return this.http.get<ReportData>(`${this.apiUrl}/notifications`, { params: this.buildParams(query) });
  }

  getNotificationTrends(query: ReportQuery = {}): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/notifications/trends`, { params: this.buildParams(query) });
  }

  getNotificationFailures(query: ReportQuery = {}): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/notifications/failures`, { params: this.buildParams(query) });
  }

  getNotificationPerformance(query: ReportQuery = {}): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/notifications/performance`, { params: this.buildParams(query) });
  }

  // ── EXPORT ────────────────────────────────────────────────────────────────────

  exportReport(reportType: string, query: ReportQuery = {}): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/export`, { reportType, ...query }, { responseType: 'blob' });
  }
}

