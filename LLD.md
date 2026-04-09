# Low Level Design (LLD)

## Project
ECommerce Product Management System

## Version
1.0

## Date
April 9, 2026

## 1. Purpose
This document defines the low-level design of the Ecommerce Product Management backend platform implemented using ASP.NET Core microservices, Ocelot API Gateway, SQL Server, JWT authentication, and RabbitMQ-based asynchronous messaging.

## 2. Scope
The design covers:
- API Gateway routing and authentication boundaries
- Service-level modules and key classes
- API contracts (core endpoints)
- Database schema and constraints
- Request/event sequences
- Error handling contract and status code mapping

This LLD describes backend implementation present in the current repository.

## 3. System Components
- API Gateway (`Ecom.Gateway`)
- Auth Service (`Ecom.Auth`)
- Catalog Service (`Ecom.Catalog`)
- Workflow Service (`Ecom.Workflow`)
- Reporting Service (`Ecom.Reporting`)
- Shared contracts/infrastructure libraries
- SQL Server databases (database-per-service model)
- RabbitMQ event broker

## 4. Architecture and Responsibilities

### 4.1 API Gateway
- Single entry point for client traffic
- JWT validation for protected routes
- Route forwarding to downstream services
- Request logging with correlation id support

### 4.2 Auth Service
- User registration and login
- Email verification and password reset flows
- Access token and refresh token lifecycle
- Role-based user management (admin functions)
- Auth audit trail

### 4.3 Catalog Service
- Product CRUD
- Category and brand retrieval
- Media management per product
- Product variant management
- Read-model endpoints for storefront preview

### 4.4 Workflow Service
- Pricing updates
- Inventory updates
- Submit/approve/reject/publish/archive transitions
- Publish-readiness checks
- Outbox-based reliable event publishing

### 4.5 Reporting Service
- Dashboard summary APIs
- Audit query APIs
- CSV export APIs
- RabbitMQ consumers to persist event-driven audit records

## 5. Module-Level Design

### 5.1 Auth Service Design
Core layers:
- API: controllers + middleware
- Application: business services and DTO logic
- Domain: entities/rules
- Infrastructure: repositories, EF Core persistence, messaging

Key controllers:
- `AuthController`
- `UsersController`
- `AdminAuditController`

Key services:
- `AuthService`
- `TokenService`
- `UserManagementService`

Security behaviors:
- BCrypt password hashing
- JWT bearer auth
- Refresh token rotation and revocation
- Rate limiting for sensitive endpoints
- Lockout handling on repeated failed login attempts

### 5.2 Catalog Service Design
Core layers:
- API
- Application
- Domain
- Infrastructure

Key controllers:
- `ProductsController`
- `MediaController`
- `CategoriesController`
- `BrandsController`
- `StorefrontController`

Key services:
- `ProductService`
- `StorefrontService`
- `AuditService`

Repository behaviors:
- Product search + pagination
- Unique SKU validation
- Soft delete handling
- Audit logging on create/update/delete

### 5.3 Workflow Service Design
Key controllers:
- `PricingController`
- `InventoryController`
- `ApprovalController`
- `PublishController`

Key services:
- `PricingService`
- `InventoryService`
- `ApprovalService`
- `PublishService`

Messaging reliability:
- Domain operations write `OutboxEvents`
- Background `OutboxProcessor` publishes to RabbitMQ
- Processed outbox rows are marked complete

Validation rules:
- `SalePrice <= MRP`
- `Quantity >= 0`
- Reject action requires comments
- Publish action requires readiness checks

### 5.4 Reporting Service Design
Key controllers:
- `DashboardController`
- `AuditController`
- `ReportsController`

Key services:
- `DashboardService`
- `AuditService`
- `ReportExportService`

Messaging:
- Consumers inherit from `RabbitMqConsumerBase`
- Consumers process `product.approved`, `product.published`, `pricing.updated`, `inventory.updated`
- Processed events are persisted to reporting audit table

## 6. API Contract (Core)

### 6.1 Gateway Entry Routes
- `/gateway/auth/*` -> Auth service
- `/gateway/catalog/*` -> Catalog service
- `/gateway/workflow/*` -> Workflow service
- `/gateway/admin/*` -> Reporting service

### 6.2 Auth APIs
- `POST /gateway/auth/signup`
- `POST /gateway/auth/login`
- `POST /gateway/auth/refresh`
- `POST /gateway/auth/logout`
- `GET /gateway/auth/me`
- `POST /gateway/auth/forgot-password`
- `POST /gateway/auth/reset-password`
- `POST /gateway/auth/change-password`
- `GET /gateway/auth/users` (admin)
- `PUT /gateway/auth/users/{id}/status` (admin)

### 6.3 Catalog APIs
- `GET /gateway/catalog/v1/products`
- `GET /gateway/catalog/v1/products/{id}`
- `POST /gateway/catalog/v1/products`
- `PUT /gateway/catalog/v1/products/{id}`
- `DELETE /gateway/catalog/v1/products/{id}`
- `GET /gateway/catalog/v1/categories`
- `GET /gateway/catalog/v1/brands`
- `GET /gateway/catalog/v1/products/{productId}/media`
- `POST /gateway/catalog/v1/products/{productId}/media`
- `GET /gateway/catalog/v1/storefront/products`
- `GET /gateway/catalog/v1/storefront/products/{id}`

### 6.4 Workflow APIs
- `PUT /gateway/workflow/workflow/products/{id}/pricing`
- `PUT /gateway/workflow/workflow/products/{id}/inventory`
- `POST /gateway/workflow/workflow/products/{id}/submit`
- `PUT /gateway/workflow/workflow/products/{id}/approve`
- `PUT /gateway/workflow/workflow/products/{id}/reject`
- `GET /gateway/workflow/workflow/products/{id}/checklist`
- `POST /gateway/workflow/workflow/products/{id}/publish`
- `POST /gateway/workflow/workflow/products/{id}/archive`

### 6.5 Reporting APIs
- `GET /gateway/admin/reports/dashboard`
- `POST /gateway/admin/reports/export`
- `GET /gateway/admin/audit/products/{id}`
- `GET /gateway/admin/audit`

## 7. Role and Authorization Matrix

### 7.1 Roles
- Admin
- ProductManager
- ContentExecutive
- Customer (preview use case)

### 7.2 Access Rules
- Authenticated read of catalog data: allowed to authorized users
- Product create: Admin, ProductManager
- Product edit: Admin, ProductManager, ContentExecutive
- Product delete: Admin only
- Workflow approval/publish/archive: Admin only
- Reporting dashboard/audit/export: Admin only
- User management: Admin only

## 8. Database Design (Implementation View)

### 8.1 Auth DB (`Ecom_AuthDB`)
Main tables:
- `Roles`
- `Users`
- `RefreshTokens`
- `EmailVerificationTokens`
- `AuditLogs`

Key constraints:
- Unique: `Roles.RoleName`
- Unique: `Users.Email`
- Unique: token fields in refresh and verification tables
- FK: `Users.RoleId -> Roles.Id`
- FK: token tables -> `Users.Id`

### 8.2 Catalog DB (`Ecom_CatalogDB`)
Main tables:
- `Products`
- `Categories`
- `Brands`
- `ProductStatuses`
- `ProductVariants`
- `MediaAssets`
- `ProductReadModels`

Key constraints:
- Unique: `Products.SKU`
- FK: `Products.CategoryId -> Categories.Id`
- FK: `Products.BrandId -> Brands.Id`
- FK: `Products.StatusId -> ProductStatuses.Id`
- FK: `ProductVariants.ProductId -> Products.Id`
- FK: `MediaAssets.ProductId -> Products.Id`
- Self FK: `Categories.ParentCategoryId -> Categories.Id`

### 8.3 Workflow DB (`Ecom_WorkflowDB`)
Main tables:
- `Prices`
- `Inventories`
- `InventoryLogs`
- `Approvals`
- `OutboxEvents`

Key constraints:
- Check: `SalePrice <= MRP`
- Check: `Quantity >= 0`
- Unique: `Inventories.ProductVariantId`
- Unique: `OutboxEvents.EventKey`

### 8.4 Reporting DB (`Ecom_ReportingDB`)
Main tables:
- `AuditLogs`

Key constraints:
- Indexed by entity lookup: `(EntityName, EntityId)`

## 9. Sequence Design

### 9.1 Login Sequence
1. Client calls gateway login endpoint.
2. Gateway forwards request to auth service.
3. Auth service validates user and password.
4. Auth service applies lockout/rate/security checks.
5. Auth service creates JWT + refresh token.
6. Response returned to client with role and redirect mapping.

### 9.2 Product Publish Sequence
1. Product created/updated in catalog service.
2. Pricing and inventory saved in workflow service.
3. Product submitted for review.
4. Admin approves product.
5. Publish action triggers outbox event write.
6. Outbox processor publishes event to RabbitMQ.
7. Reporting consumer processes event and stores audit/report data.

### 9.3 Report Export Sequence
1. Admin requests export from gateway.
2. Reporting service validates filters and report type.
3. Reporting repository queries audit logs.
4. Service generates CSV and returns file response.

## 10. Error Codes and Handling

### 10.1 Status Codes
- `200` Success
- `201` Created
- `204` No Content
- `400` Validation/Bad Request
- `401` Unauthorized
- `403` Forbidden
- `404` Not Found
- `409` Conflict
- `429` Too Many Requests
- `500` Internal Error
- `503` Service Unavailable

### 10.2 Error Response Contract
Typical error payload should contain:
- `status`
- `message`
- `correlationId`
- optional `errors` array for validation issues

### 10.3 Service Notes
- Auth service has custom exception middleware and standardized error payloads.
- Cross-service standardization is recommended to keep error contracts uniform at gateway level.

## 11. Non-Functional Considerations
- Security: JWT, RBAC, hashed passwords, rate limiting
- Reliability: outbox pattern for event delivery
- Observability: health checks, request logs, correlation ids
- Performance: pagination, read-model pattern for storefront reads
- Maintainability: clean architecture per microservice

## 12. Known Gaps and Improvement Areas
- Ensure event exchange/routing naming remains consistent across publishers/consumers.
- Validate and enforce target code coverage across all services in CI.
- Complete frontend (Angular) integration against gateway contracts if part of delivery scope.
- Add centralized API error contract policy across all microservices.

## 13. Conclusion
The implementation follows a modular low-level design with clear separation of concerns, secure access controls, reliable asynchronous processing, and service-specific persistence. The platform is structured for enterprise-style extensibility and maintainability.
