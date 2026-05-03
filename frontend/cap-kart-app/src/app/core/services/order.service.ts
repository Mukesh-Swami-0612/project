import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Order, OrderHistory, OrderQueryParams } from '../models/order.model';
import { API_CONFIG } from '../config/api.config';

/**
 * Actual paginated response shape from WorkflowController.
 * Backend returns 'data' (not 'items') to match the PagedResult naming.
 */
export interface WorkflowPagedResult<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private apiUrl = `${API_CONFIG.WORKFLOW_API}/workflow/workflows`;

  constructor(private http: HttpClient) {}

  // ── QUERY / READ ─────────────────────────────────────────────────────────────

  /** GET /api/v1/workflow/workflows - Get all orders with optional filtering and pagination */
  getAllOrders(queryParams?: OrderQueryParams): Observable<WorkflowPagedResult<Order>> {
    let params = new HttpParams();

    if (queryParams) {
      if (queryParams.status) params = params.set('status', queryParams.status);
      if (queryParams.productId) params = params.set('productId', queryParams.productId.toString());
      if (queryParams.page) params = params.set('page', queryParams.page.toString());
      if (queryParams.pageSize) params = params.set('pageSize', queryParams.pageSize.toString());
    }

    return this.http.get<WorkflowPagedResult<Order>>(this.apiUrl, { params });
  }

  /** GET /api/v1/workflow/workflows/{id} - Get order by ID */
  getOrderById(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.apiUrl}/${id}`);
  }

  /** GET /api/v1/workflow/workflows/product/{productId} - Get order by product ID */
  getOrderByProductId(productId: number): Observable<Order> {
    return this.http.get<Order>(`${this.apiUrl}/product/${productId}`);
  }

  /** GET /api/v1/workflow/workflows/{id}/logs - Get order history/audit logs */
  getOrderHistory(id: string, page: number = 1, pageSize: number = 50): Observable<WorkflowPagedResult<OrderHistory>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<WorkflowPagedResult<OrderHistory>>(`${this.apiUrl}/${id}/logs`, { params });
  }

  /** GET /api/v1/workflow/workflows/stats - Get workflow statistics */
  getOrderStats(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/stats`);
  }

  // ── ACTIONS ──────────────────────────────────────────────────────────────────

  /** POST /api/v1/workflow/workflows/{id}/retry - Retry a failed workflow */
  retryOrder(id: string): Observable<Order> {
    return this.http.post<Order>(`${this.apiUrl}/${id}/retry`, {});
  }

  /** POST /api/v1/workflow/workflows/{id}/cancel - Cancel an order/workflow */
  cancelOrder(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/cancel`, {});
  }

  // ── EXPORT ───────────────────────────────────────────────────────────────────

  /** GET /api/v1/workflow/workflows/{id}/logs/export - Export order history to Excel */
  exportOrderHistory(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/logs/export`, { responseType: 'blob' });
  }
}
