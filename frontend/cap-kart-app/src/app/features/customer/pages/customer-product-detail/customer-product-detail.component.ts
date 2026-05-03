import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../../../core/services/product.service';
import { Product } from '../../../../core/models/product.model';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-customer-product-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="product-detail-container">
      <div class="breadcrumb">
        <a routerLink="/customer/products">← Back to Products</a>
      </div>

      <div *ngIf="loading" class="loading-state">
        <p>Loading product details...</p>
      </div>

      <div *ngIf="error" class="error-state">
        <p>{{ error }}</p>
      </div>

      <div *ngIf="product && !loading && !error" class="product-content">
        <div class="product-gallery">
          <div class="main-image">
            <span>{{ product.name.charAt(0) }}</span>
          </div>
        </div>
        
        <div class="product-info">
          <div class="meta">
            <span class="category">{{ product.categoryName }}</span>
            <span class="brand" *ngIf="product.brandName">{{ product.brandName }}</span>
          </div>
          
          <h1 class="title">{{ product.name }}</h1>
          <p class="sku">SKU: {{ product.sku }}</p>
          
          <!-- Mock Price as backend doesn't seem to return it in ProductDto yet -->
          <div class="price-section">
            <h2 class="price">$99.99</h2> 
          </div>
          
          <div class="description-section">
            <h3>Description</h3>
            <p>This is a great product from our catalog. Detailed description will go here once the backend provides it.</p>
          </div>
          
          <div class="actions">
            <button class="add-to-cart-btn" (click)="addToCart()">Add to Cart</button>
          </div>
          
          <div class="details-list">
            <h3>Product Details</h3>
            <ul>
              <li><strong>Status:</strong> {{ product.status }}</li>
              <li><strong>Added:</strong> {{ product.createdAt | date:'mediumDate' }}</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .product-detail-container {
      padding: 24px;
      max-width: 1200px;
      margin: 0 auto;
    }
    .breadcrumb {
      margin-bottom: 24px;
    }
    .breadcrumb a {
      color: #0056b3;
      text-decoration: none;
      font-weight: 500;
    }
    .breadcrumb a:hover {
      text-decoration: underline;
    }
    .product-content {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 48px;
      background: #fff;
      padding: 32px;
      border-radius: 8px;
      box-shadow: 0 2px 10px rgba(0,0,0,0.05);
    }
    @media (max-width: 768px) {
      .product-content {
        grid-template-columns: 1fr;
      }
    }
    .main-image {
      width: 100%;
      aspect-ratio: 1;
      background: #f8f9fa;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 120px;
      color: #dee2e6;
      border: 1px solid #eee;
    }
    .meta {
      display: flex;
      gap: 12px;
      margin-bottom: 16px;
    }
    .category {
      background: #e9ecef;
      padding: 4px 12px;
      border-radius: 4px;
      font-size: 14px;
      font-weight: 500;
    }
    .brand {
      color: #6c757d;
      font-size: 14px;
      font-weight: 500;
      align-self: center;
    }
    .title {
      margin: 0 0 8px 0;
      font-size: 32px;
      color: #212529;
    }
    .sku {
      color: #6c757d;
      margin: 0 0 24px 0;
      font-size: 14px;
    }
    .price-section {
      margin-bottom: 32px;
    }
    .price {
      font-size: 36px;
      color: #0056b3;
      margin: 0;
    }
    .description-section {
      margin-bottom: 32px;
      color: #495057;
      line-height: 1.6;
    }
    .description-section h3 {
      font-size: 18px;
      margin-bottom: 8px;
      color: #212529;
    }
    .actions {
      margin-bottom: 40px;
    }
    .add-to-cart-btn {
      width: 100%;
      padding: 16px;
      background-color: #28a745;
      color: white;
      border: none;
      border-radius: 4px;
      font-size: 18px;
      font-weight: 600;
      cursor: pointer;
      transition: background-color 0.2s;
    }
    .add-to-cart-btn:hover {
      background-color: #218838;
    }
    .details-list {
      border-top: 1px solid #eee;
      padding-top: 24px;
    }
    .details-list h3 {
      font-size: 18px;
      margin-bottom: 16px;
    }
    .details-list ul {
      list-style: none;
      padding: 0;
      margin: 0;
    }
    .details-list li {
      padding: 8px 0;
      border-bottom: 1px solid #f8f9fa;
      color: #495057;
    }
    .loading-state, .error-state {
      text-align: center;
      padding: 64px;
      background: #fff;
      border-radius: 8px;
      box-shadow: 0 2px 10px rgba(0,0,0,0.05);
    }
    .error-state {
      color: #dc3545;
    }
  `]
})
export class CustomerProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private productService = inject(ProductService);
  
  productId = this.route.snapshot.paramMap.get('id') ?? '';
  product: Product | null = null;
  loading = false;
  error: string | null = null;

  ngOnInit() {
    if (this.productId) {
      this.loadProduct(parseInt(this.productId, 10));
    }
  }

  loadProduct(id: number) {
    this.loading = true;
    this.error = null;
    
    this.productService.getProductById(id)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (data: any) => {
          this.product = data;
        },
        error: (err: any) => {
          console.error('Failed to load product', err);
          this.error = 'Could not load product details. It may not exist.';
        }
      });
  }
  
  addToCart() {
    alert('Added to cart!');
  }
}

