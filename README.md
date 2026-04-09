# ECommerce Product Management System

ASP.NET Core Microservices + Ocelot API Gateway + SQL Server + EF Core + JWT

## Services

| Service    | Port  | Database           |
|------------|-------|--------------------|
| Gateway    | 5000  | -                  |
| Auth       | 5001  | Ecom_AuthDB        |
| Catalog    | 5002  | Ecom_CatalogDB     |
| Workflow   | 5003  | Ecom_WorkflowDB    |
| Reporting  | 5004  | Ecom_ReportingDB   |

## Quick Start

```bash
cp .env.dev .env
docker-compose up --build
```

## Architecture

- 4 Microservices with Clean Architecture (API / Application / Domain / Infrastructure)
- Ocelot API Gateway with JWT validation
- RabbitMQ for async event-driven communication
- Outbox pattern for reliable event publishing
- CQRS read model in Catalog service
