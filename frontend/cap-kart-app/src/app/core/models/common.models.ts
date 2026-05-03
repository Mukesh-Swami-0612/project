/**
 * Shared common interfaces used across multiple services.
 * Import from here instead of duplicating in individual model files.
 */

/** Generic API response envelope returned by all backend endpoints */
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

/** Generic paginated result shape returned by list endpoints */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
