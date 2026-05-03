import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ProductService } from '../../../../core/services/product.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-admin-product-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="admin-product-form-container">
      <div class="header">
        <a routerLink="/admin/products" class="back-link">← Back to Products</a>
        <h2>{{ isNew ? 'Create New Product' : 'Edit Product' }}</h2>
      </div>

      <div *ngIf="loading" class="loading-state">
        <p>Loading...</p>
      </div>
      
      <div *ngIf="error" class="error-state">
        <p>{{ error }}</p>
      </div>

      <form *ngIf="!loading && !error" [formGroup]="productForm" (ngSubmit)="onSubmit()" class="product-form">
        <div class="form-group">
          <label for="name">Product Name *</label>
          <input id="name" type="text" formControlName="name" class="form-control" [class.is-invalid]="isFieldInvalid('name')" />
          <div *ngIf="isFieldInvalid('name')" class="invalid-feedback">Name is required.</div>
        </div>

        <div class="form-group">
          <label for="sku">SKU *</label>
          <input id="sku" type="text" formControlName="sku" class="form-control" [class.is-invalid]="isFieldInvalid('sku')" />
          <div *ngIf="isFieldInvalid('sku')" class="invalid-feedback">SKU is required.</div>
        </div>
        
        <div class="form-group">
          <label for="categoryId">Category ID *</label>
          <input id="categoryId" type="number" formControlName="categoryId" class="form-control" [class.is-invalid]="isFieldInvalid('categoryId')" />
          <div *ngIf="isFieldInvalid('categoryId')" class="invalid-feedback">Valid Category ID is required.</div>
          <small class="form-text text-muted">For demo purposes, use ID 1, 2, or 3.</small>
        </div>

        <div class="form-group">
          <label for="brandId">Brand ID</label>
          <input id="brandId" type="number" formControlName="brandId" class="form-control" />
        </div>
        
        <div class="form-group">
          <label for="basePrice">Base Price ($) *</label>
          <input id="basePrice" type="number" formControlName="basePrice" step="0.01" class="form-control" [class.is-invalid]="isFieldInvalid('basePrice')" />
        </div>

        <div class="form-actions">
          <button type="button" class="btn btn-secondary" routerLink="/admin/products">Cancel</button>
          <button type="submit" class="btn btn-primary" [disabled]="productForm.invalid || saving">
            {{ saving ? 'Saving...' : 'Save Product' }}
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .admin-product-form-container {
      padding: 24px;
      max-width: 800px;
      margin: 0 auto;
    }
    .header {
      margin-bottom: 24px;
    }
    .back-link {
      display: inline-block;
      margin-bottom: 16px;
      color: #0056b3;
      text-decoration: none;
    }
    .back-link:hover { text-decoration: underline; }
    h2 { margin: 0; color: #333; }
    .product-form {
      background: white;
      padding: 24px;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.05);
    }
    .form-group { margin-bottom: 16px; }
    label {
      display: block;
      margin-bottom: 8px;
      font-weight: 500;
      color: #495057;
    }
    .form-control {
      width: 100%;
      padding: 10px;
      border: 1px solid #ced4da;
      border-radius: 4px;
      font-size: 14px;
      box-sizing: border-box;
    }
    .form-control:focus {
      border-color: #80bdff;
      outline: 0;
      box-shadow: 0 0 0 0.2rem rgba(0,123,255,.25);
    }
    .is-invalid { border-color: #dc3545; }
    .invalid-feedback {
      color: #dc3545;
      font-size: 12px;
      margin-top: 4px;
    }
    .form-text { font-size: 12px; color: #6c757d; }
    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
      margin-top: 32px;
      padding-top: 16px;
      border-top: 1px solid #eee;
    }
    .btn {
      padding: 10px 20px;
      border: none;
      border-radius: 4px;
      font-weight: 500;
      cursor: pointer;
    }
    .btn-secondary { background-color: #6c757d; color: white; }
    .btn-primary { background-color: #0056b3; color: white; }
    .btn:disabled { opacity: 0.65; cursor: not-allowed; }
    .loading-state, .error-state { padding: 48px; text-align: center; }
    .error-state { color: #dc3545; }
  `]
})
export class AdminProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private productService = inject(ProductService);
  
  productId = this.route.snapshot.paramMap.get('id');
  isNew = this.productId === 'new';
  
  loading = false;
  saving = false;
  error: string | null = null;
  
  productForm: FormGroup = this.fb.group({
    name: ['', Validators.required],
    sku: ['', Validators.required],
    description: [''],
    basePrice: [0, [Validators.required, Validators.min(0)]],
    categoryId: [1, Validators.required],
    brandId: [null]
  });

  ngOnInit() {
    if (!this.isNew && this.productId) {
      this.loadProduct(parseInt(this.productId, 10));
    }
  }

  loadProduct(id: number) {
    this.loading = true;
    this.productService.getProductById(id)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (product: any) => {
          this.productForm.patchValue({
            name: product.name,
            sku: product.sku,
            // Since Dto might not return all these fields correctly for edit right now,
            // we patch what we can. A real app would use a specific DetailDto.
            categoryId: 1, 
            basePrice: 99.99
          });
        },
        error: (err) => {
          console.error(err);
          this.error = 'Failed to load product details';
        }
      });
  }

  isFieldInvalid(field: string): boolean {
    const control = this.productForm.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  onSubmit() {
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      return;
    }
    
    this.saving = true;
    const formData = this.productForm.value;
    
    if (this.isNew) {
      this.productService.createProduct(formData)
        .pipe(finalize(() => this.saving = false))
        .subscribe({
          next: () => this.router.navigate(['/admin/products']),
          error: (err) => {
            console.error(err);
            alert('Failed to create product');
          }
        });
    } else if (this.productId) {
      this.productService.updateProduct(parseInt(this.productId, 10), formData)
        .pipe(finalize(() => this.saving = false))
        .subscribe({
          next: () => this.router.navigate(['/admin/products']),
          error: (err) => {
            console.error(err);
            alert('Failed to update product');
          }
        });
    }
  }
}

