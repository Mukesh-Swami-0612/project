export { PagedResult } from './common.models';

export interface Order {
  id: string;
  productId: number;
  status: WorkflowStatus;
  currentStep: WorkflowStep;
  retryCount: number;
  maxRetries: number;
  nextRetryAt?: string;
  lastError?: string;
  createdAt: string;
  updatedAt: string;
  completedAt?: string;
  correlationId?: string;
}

export enum WorkflowStatus {
  InProgress = 'InProgress',
  Completed = 'Completed',
  Failed = 'Failed'
}

export enum WorkflowStep {
  Created = 'Created',
  ValidationPending = 'ValidationPending',
  ValidationCompleted = 'ValidationCompleted',
  ApprovalPending = 'ApprovalPending',
  Approved = 'Approved',
  Rejected = 'Rejected',
  Publishing = 'Publishing',
  Published = 'Published',
  Completed = 'Completed',
  Failed = 'Failed'
}

export interface OrderHistory {
  id: number;
  workflowId: string;
  step: WorkflowStep;
  status: string;
  message?: string;
  timestamp: string;
  metadata?: Record<string, any>;
}

export interface OrderQueryParams {
  status?: WorkflowStatus;
  productId?: number;
  page?: number;
  pageSize?: number;
}
