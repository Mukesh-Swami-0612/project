import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Product, PagedResult } from '../models/product.model';
import { API_CONFIG } from '../config/api.config';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private apiUrl = `${API_CONFIG.CATALOG_API}/products`;

  constructor(private http: HttpClient) {}

  // ── QUERY / READ ─────────────────────────────────────────────────────────────

  getProducts(search?: string, page: number = 1, pageSize: number = 10, status?: string): Observable<PagedResult<Product>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) params = params.set('search', search);
    if (status) params = params.set('status', status);

    return this.http.get<PagedResult<Product>>(`${this.apiUrl}/query`, { params });
  }

  getProductById(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  getProductStatus(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}/status`);
  }

  // ── CREATE / UPDATE / DELETE ─────────────────────────────────────────────────

  createProduct(product: any): Observable<Product> {
    return this.http.post<Product>(this.apiUrl, product);
  }

  updateProduct(id: number, product: any): Observable<Product> {
    return this.http.put<Product>(`${this.apiUrl}/${id}`, product);
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  // ── LIFECYCLE MANAGEMENT ──────────────────────────────────────────────────────

  /** POST /api/v1/products/{id}/submit-for-review */
  submitForReview(id: number): Observable<Product> {
    return this.http.post<Product>(`${this.apiUrl}/${id}/submit-for-review`, {});
  }

  /** POST /api/v1/products/{id}/approve */
  approveProduct(id: number): Observable<Product> {
    return this.http.post<Product>(`${this.apiUrl}/${id}/approve`, {});
  }

  /** POST /api/v1/products/{id}/reject  (body = reason string) */
  rejectProduct(id: number, reason: string): Observable<Product> {
    return this.http.post<Product>(`${this.apiUrl}/${id}/reject`, JSON.stringify(reason), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  /** POST /api/v1/products/{id}/publish */
  publishProduct(id: number): Observable<Product> {
    return this.http.post<Product>(`${this.apiUrl}/${id}/publish`, {});
  }

  /** POST /api/v1/products/{id}/archive */
  archiveProduct(id: number): Observable<Product> {
    return this.http.post<Product>(`${this.apiUrl}/${id}/archive`, {});
  }

  /** POST /api/v1/products/{id}/transition */
  transitionStatus(id: number, targetStatus: string, comments?: string): Observable<Product> {
    return this.http.post<Product>(`${this.apiUrl}/${id}/transition`, { targetStatus, comments });
  }
}


