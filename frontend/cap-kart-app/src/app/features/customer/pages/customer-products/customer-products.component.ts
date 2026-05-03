import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ProductService } from '../../../../core/services/product.service';
import { Product } from '../../../../core/models/product.model';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-customer-products',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="product-listing-container">
      <div class="header">
        <h2>Products</h2>
        <div class="search-box">
          <input 
            type="text" 
            [(ngModel)]="searchQuery" 
            (keyup.enter)="onSearch()"
            placeholder="Search products..." 
            class="search-input" />
          <button (click)="onSearch()" class="search-btn">Search</button>
        </div>
      </div>

      <div *ngIf="loading" class="loading-state">
        <p>Loading products...</p>
      </div>

      <div *ngIf="error" class="error-state">
        <p>{{ error }}</p>
      </div>

      <div *ngIf="!loading && !error && products.length === 0" class="empty-state">
        <p>No products found matching your criteria.</p>
      </div>

      <div class="product-grid" *ngIf="!loading && products.length > 0">
        <div class="product-card" *ngFor="let product of products" [routerLink]="['/customer/products', product.id]">
          <div class="product-image-placeholder">
            <span>{{ product.name.charAt(0) }}</span>
          </div>
          <div class="product-info">
            <span class="category-badge">{{ product.categoryName }}</span>
            <h3 class="product-title">{{ product.name }}</h3>
            <p class="product-sku">SKU: {{ product.sku }}</p>
            <p class="product-brand" *ngIf="product.brandName">{{ product.brandName }}</p>
            <div class="product-footer">
              <span class="view-details-btn">View Details</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .product-listing-container {
      padding: 24px;
      max-width: 1200px;
      margin: 0 auto;
    }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
      flex-wrap: wrap;
      gap: 16px;
    }
    h2 {
      margin: 0;
      color: #333;
    }
    .search-box {
      display: flex;
      gap: 8px;
    }
    .search-input {
      padding: 8px 12px;
      border: 1px solid #ddd;
      border-radius: 4px;
      width: 250px;
      font-size: 14px;
    }
    .search-btn {
      padding: 8px 16px;
      background-color: #0056b3;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 500;
    }
    .search-btn:hover {
      background-color: #004494;
    }
    .product-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
      gap: 24px;
    }
    .product-card {
      border: 1px solid #eee;
      border-radius: 8px;
      overflow: hidden;
      cursor: pointer;
      transition: transform 0.2s, box-shadow 0.2s;
      background: white;
    }
    .product-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 10px 20px rgba(0,0,0,0.1);
    }
    .product-image-placeholder {
      height: 180px;
      background: #f8f9fa;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 48px;
      color: #dee2e6;
      border-bottom: 1px solid #eee;
    }
    .product-info {
      padding: 16px;
    }
    .category-badge {
      display: inline-block;
      padding: 4px 8px;
      background: #e9ecef;
      color: #495057;
      border-radius: 4px;
      font-size: 12px;
      margin-bottom: 8px;
    }
    .product-title {
      margin: 0 0 8px 0;
      font-size: 18px;
      color: #212529;
    }
    .product-sku, .product-brand {
      margin: 0 0 4px 0;
      font-size: 14px;
      color: #6c757d;
    }
    .product-footer {
      margin-top: 16px;
      display: flex;
      justify-content: flex-end;
    }
    .view-details-btn {
      font-size: 14px;
      color: #0056b3;
      font-weight: 500;
    }
    .loading-state, .error-state, .empty-state {
      text-align: center;
      padding: 48px;
      background: #f8f9fa;
      border-radius: 8px;
      color: #6c757d;
    }
    .error-state {
      color: #dc3545;
      background: #f8d7da;
    }
  `]
})
export class CustomerProductsComponent implements OnInit {
  private productService = inject(ProductService);
  
  products: Product[] = [];
  loading = false;
  error: string | null = null;
  searchQuery = '';
  
  // Pagination
  currentPage = 1;
  pageSize = 12;
  totalCount = 0;

  ngOnInit() {
    this.loadProducts();
  }

  loadProducts() {
    this.loading = true;
    this.error = null;
    
    // For customers, ideally we only show published products.
    // If the API supports passing status in the query, we pass it.
    this.productService.getProducts(this.searchQuery, this.currentPage, this.pageSize, 'Published')
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (result: any) => {
          this.products = result.items;
          this.totalCount = result.totalCount;
        },
        error: (err: any) => {
          console.error('Failed to load products', err);
          this.error = 'Could not load products. Please try again later.';
        }
      });
  }

  onSearch() {
    this.currentPage = 1; // Reset to first page on search
    this.loadProducts();
  }
}

