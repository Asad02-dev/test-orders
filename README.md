# ECommerce Platform — .NET 10 Microservices

A production-oriented API-first e-commerce platform built with .NET 10, following a full microservice architecture.

## Architecture Overview

- **Gateway**: YARP reverse proxy — routes all external traffic to downstream services
- **Keycloak**: Local identity provider — JWT bearer authentication across all services
- **PostgreSQL**: Single database engine (separate schema per service)
- **RabbitMQ + MassTransit**: Async event-driven messaging between services
- **Serilog + OpenTelemetry**: Structured logging and distributed tracing

## Solution Structure

```
src/
  Gateway/Gateway.Api             # YARP reverse proxy (port 5100)
  BuildingBlocks/
    SharedKernel                  # Domain primitives: Entity, AggregateRoot, Result, ValueObject
    Contracts                     # Integration event contracts shared across services
    Messaging                     # MassTransit/RabbitMQ wiring
    Persistence                   # EF Core + Npgsql helpers, OutboxMessage
    Authentication                # Keycloak JWT bearer extension
    Observability                 # Serilog + OpenTelemetry extension
  Services/
    Catalog/   (Api:5101, Application, Domain, Infrastructure)
    Cart/      (Api:5102, Application, Domain, Infrastructure)
    Orders/    (Api:5103, Application, Domain, Infrastructure)
    Inventory/ (Api:5104, Application, Domain, Infrastructure)
    Payments/  (Api:5105, Application, Domain, Infrastructure)
    Notifications/ (Api:5106, Application, Infrastructure)
tests/
docs/architecture/
```

## Local Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 16+](https://www.postgresql.org/download/) running on `localhost:5432`
- [RabbitMQ 3.13+](https://www.rabbitmq.com/install-windows.html) running on `localhost:5672`
- [Keycloak 24+](https://www.keycloak.org/downloads) running on `localhost:8080`

## Keycloak Setup

1. Start Keycloak: `bin/kc.bat start-dev` (Windows) or `bin/kc.sh start-dev` (Linux/macOS)
2. Open `http://localhost:8080` → Admin Console (admin/admin)
3. Create realm: `ecommerce`
4. Create client: `ecommerce-client` (public, standard flow)
5. Add roles: `customer`, `admin`, `manager`

## PostgreSQL Setup

Create one database per service (or use schemas within one database):

```sql
CREATE DATABASE ecommerce_catalog;
CREATE DATABASE ecommerce_cart;
CREATE DATABASE ecommerce_orders;
CREATE DATABASE ecommerce_inventory;
CREATE DATABASE ecommerce_payments;
```

Default credentials: `postgres` / `postgres` on `localhost:5432`. Update `appsettings.json` per service as needed.

## RabbitMQ Setup

Default guest/guest on `localhost:5672`. No additional setup needed for local development.

## Running Services Locally

Open separate terminals for each service:

```bash
# Gateway (routes to all services)
dotnet run --project src/Gateway/Gateway.Api

# Core services
dotnet run --project src/Services/Catalog/Catalog.Api
dotnet run --project src/Services/Cart/Cart.Api
dotnet run --project src/Services/Orders/Orders.Api
dotnet run --project src/Services/Inventory/Inventory.Api
dotnet run --project src/Services/Payments/Payments.Api
dotnet run --project src/Services/Notifications/Notifications.Api
```

Access OpenAPI docs per service at `http://localhost:{port}/openapi`.

## Integration Events

| Event | Publisher | Consumers |
|-------|-----------|-----------|
| `OrderPlacedEvent` | Orders | Inventory |
| `InventoryReservedEvent` | Inventory | Payments |
| `InventoryReservationFailedEvent` | Inventory | Orders |
| `PaymentAuthorizedEvent` | Payments | Orders |
| `PaymentFailedEvent` | Payments | Orders |
| `SendOrderConfirmationNotificationCommand` | Orders | Notifications |
| `SendOrderCancelledNotificationCommand` | Orders | Notifications |

## Health Checks

Each service exposes `GET /health`. The gateway exposes `GET /health`.

## Docs

See [`docs/architecture/`](docs/architecture/) for detailed architecture, service responsibilities, and roadmap.
