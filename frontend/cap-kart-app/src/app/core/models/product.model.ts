export { PagedResult } from './common.models';

export interface Product {
  id: number;
  name: string;
  sku: string;
  categoryName: string;
  brandName?: string;
  status: string;
  createdAt: string;
  rowVersion?: string;
}
