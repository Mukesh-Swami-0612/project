import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../config/api.config';
import { ApiResponse } from '../models/common.models';

export interface UserResponseDto {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: string;
  isActive: boolean;
  isLocked: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

export interface UpdateUserStatusDto {
  isActive: boolean;
}

export interface ChangeRoleDto {
  roleId: number;
}

// Role IDs matching backend Roles enum
export const ROLE_IDS: Record<string, number> = {
  Admin: 1,
  Customer: 2,
  Vendor: 3,
  ProductManager: 4,
  ContentExecutive: 5
};

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly baseUrl = `${API_CONFIG.AUTH_API}/auth/users`;

  constructor(private http: HttpClient) {}

  /** GET /api/v1/auth/users */
  getAllUsers(): Observable<ApiResponse<UserResponseDto[]>> {
    return this.http.get<ApiResponse<UserResponseDto[]>>(this.baseUrl);
  }

  /** GET /api/v1/auth/users/{id} */
  getUserById(id: number): Observable<ApiResponse<UserResponseDto>> {
    return this.http.get<ApiResponse<UserResponseDto>>(`${this.baseUrl}/${id}`);
  }

  /** PUT /api/v1/auth/users/{id}/status */
  updateUserStatus(id: number, isActive: boolean): Observable<ApiResponse<null>> {
    const dto: UpdateUserStatusDto = { isActive };
    return this.http.put<ApiResponse<null>>(`${this.baseUrl}/${id}/status`, dto);
  }

  /** PUT /api/v1/auth/users/{id}/role */
  changeUserRole(id: number, roleId: number): Observable<ApiResponse<null>> {
    const dto: ChangeRoleDto = { roleId };
    return this.http.put<ApiResponse<null>>(`${this.baseUrl}/${id}/role`, dto);
  }

  /** PUT /api/v1/auth/users/{id}/unlock */
  unlockUser(id: number): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(`${this.baseUrl}/${id}/unlock`, {});
  }

  /** DELETE /api/v1/auth/users/{id} */
  deleteUser(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`);
  }
}
