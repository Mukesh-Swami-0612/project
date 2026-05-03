import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface FilterOption {
  label: string;
  value: any;
}

export interface FilterDefinition {
  key: string;
  label: string;
  type: 'select' | 'multiselect' | 'range' | 'date' | 'text';
  options?: FilterOption[];
  placeholder?: string;
}

@Component({
  selector: 'app-filter-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="filter-panel" [class.expanded]="isExpanded">
      <div class="filter-header">
        <h3>Filters</h3>
        <div class="filter-actions">
          <button class="btn-text" (click)="onReset()" *ngIf="hasActiveFilters()">
            Clear All
          </button>
          <button class="btn-icon" (click)="toggleExpanded()">
            {{ isExpanded ? '▲' : '▼' }}
          </button>
        </div>
      </div>

      <div class="filter-body" *ngIf="isExpanded">
        <div class="filter-group" *ngFor="let filter of filters">
          <label class="filter-label">{{ filter.label }}</label>
          
          <!-- Select Filter -->
          <select *ngIf="filter.type === 'select'"
                  [(ngModel)]="filterValues[filter.key]"
                  (change)="onFilterChange()"
                  class="filter-input">
            <option [value]="null">{{ filter.placeholder || 'All' }}</option>
            <option *ngFor="let option of filter.options" [value]="option.value">
              {{ option.label }}
            </option>
          </select>

          <!-- Text Filter -->
          <input *ngIf="filter.type === 'text'"
                 type="text"
                 [(ngModel)]="filterValues[filter.key]"
                 (input)="onFilterChange()"
                 [placeholder]="filter.placeholder || 'Enter value'"
                 class="filter-input">

          <!-- Range Filter -->
          <div *ngIf="filter.type === 'range'" class="range-inputs">
            <input type="number"
                   [(ngModel)]="filterValues[filter.key + '_min']"
                   (input)="onFilterChange()"
                   placeholder="Min"
                   class="filter-input range-input">
            <span class="range-separator">-</span>
            <input type="number"
                   [(ngModel)]="filterValues[filter.key + '_max']"
                   (input)="onFilterChange()"
                   placeholder="Max"
                   class="filter-input range-input">
          </div>

          <!-- Date Filter -->
          <input *ngIf="filter.type === 'date'"
                 type="date"
                 [(ngModel)]="filterValues[filter.key]"
                 (change)="onFilterChange()"
                 class="filter-input">
        </div>

        <div class="filter-footer">
          <button class="btn-primary" (click)="onApply()">
            Apply Filters
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .filter-panel {
      background: var(--surface-container-lowest);
      border: 1px solid var(--outline-variant);
      border-radius: var(--radius-md);
      overflow: hidden;
    }

    .filter-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: var(--spacing-md);
      background: var(--surface-container-low);
      border-bottom: 1px solid var(--outline-variant);
    }

    .filter-header h3 {
      font-size: 16px;
      font-weight: 600;
      color: var(--on-surface);
      margin: 0;
    }

    .filter-actions {
      display: flex;
      gap: var(--spacing-sm);
      align-items: center;
    }

    .btn-text {
      background: none;
      border: none;
      color: var(--primary);
      font-size: 14px;
      cursor: pointer;
      padding: 4px 8px;
    }

    .btn-text:hover {
      text-decoration: underline;
    }

    .btn-icon {
      background: none;
      border: none;
      color: var(--on-surface-variant);
      cursor: pointer;
      padding: 4px;
      font-size: 12px;
    }

    .filter-body {
      padding: var(--spacing-lg);
      display: flex;
      flex-direction: column;
      gap: var(--spacing-md);
    }

    .filter-group {
      display: flex;
      flex-direction: column;
      gap: var(--spacing-sm);
    }

    .filter-label {
      font-size: 14px;
      font-weight: 500;
      color: var(--on-surface);
    }

    .filter-input {
      padding: 10px 12px;
      border: 1px solid var(--outline-variant);
      border-radius: var(--radius-sm);
      font-size: 14px;
      background: var(--surface-container-lowest);
      color: var(--on-surface);
    }

    .filter-input:focus {
      outline: none;
      border-color: var(--primary);
      box-shadow: 0 0 0 2px rgba(0, 56, 123, 0.1);
    }

    .range-inputs {
      display: flex;
      align-items: center;
      gap: var(--spacing-sm);
    }

    .range-input {
      flex: 1;
    }

    .range-separator {
      color: var(--on-surface-variant);
    }

    .filter-footer {
      padding-top: var(--spacing-md);
      border-top: 1px solid var(--outline-variant);
    }

    .btn-primary {
      width: 100%;
      padding: 10px 16px;
      background: var(--primary);
      color: var(--on-primary);
      border: none;
      border-radius: var(--radius-sm);
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
    }

    .btn-primary:hover {
      background: var(--primary-container);
    }
  `]
})
export class FilterPanelComponent {
  @Input() filters: FilterDefinition[] = [];
  @Input() initialValues: Record<string, any> = {};
  @Output() filterChange = new EventEmitter<Record<string, any>>();
  @Output() apply = new EventEmitter<Record<string, any>>();

  isExpanded = true;
  filterValues: Record<string, any> = {};

  ngOnInit() {
    this.filterValues = { ...this.initialValues };
  }

  toggleExpanded(): void {
    this.isExpanded = !this.isExpanded;
  }

  onFilterChange(): void {
    this.filterChange.emit(this.filterValues);
  }

  onApply(): void {
    this.apply.emit(this.filterValues);
  }

  onReset(): void {
    this.filterValues = {};
    this.onFilterChange();
    this.onApply();
  }

  hasActiveFilters(): boolean {
    return Object.values(this.filterValues).some(value => 
      value !== null && value !== undefined && value !== ''
    );
  }
}
