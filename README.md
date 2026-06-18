# ECommerce Platform — .NET 10 Microservices

A production-oriented API-first e-commerce platform built with .NET 10, following a full microservice architecture.

## Architecture Overview

- **Gateway**: YARP reverse proxy — routes all external traffic to downstream services
- **Keycloak**: Local identity provider — JWT bearer authentication across all services
- **PostgreSQL**: Single database engine (separate schema per service)
- **RabbitMQ + MassTransit**: Async event-driven messaging between services
- **Serilog + OpenTelemetry**: Structured logging and distributed tracing
- **Correlation ID**: `X-Correlation-Id` header propagated through all services

## Solution Structure

```
src/
  Gateway/Gateway.Api             # YARP reverse proxy (port 5100)
  BuildingBlocks/
    SharedKernel                  # Domain primitives: Entity, AggregateRoot, Result, ValueObject, CorrelationContext
    Contracts                     # Integration event contracts shared across services
    Messaging                     # MassTransit/RabbitMQ wiring
    Persistence                   # EF Core + Npgsql helpers, OutboxMessage, OutboxWorker
    Authentication                # Keycloak JWT bearer extension
    Observability                 # Serilog + OpenTelemetry + CorrelationIdMiddleware
  Services/
    Catalog/   (Api:5101, Application, Domain, Infrastructure)
    Cart/      (Api:5102, Application, Domain, Infrastructure)
    Orders/    (Api:5103, Application, Domain, Infrastructure)
    Inventory/ (Api:5104, Application, Domain, Infrastructure)
    Payments/  (Api:5105, Application, Domain, Infrastructure)
    Notifications/ (Api:5106, Application, Infrastructure)
docs/
  architecture/   # OVERVIEW.md
  services/       # Per-service docs (CART, ORDERS, INVENTORY, PAYMENTS, NOTIFICATIONS, CATALOG, GATEWAY)
  TODO.md         # Phase progress and future work
```

## Phase 3 — Hardening (This PR — Implemented)

This phase completes the remaining high-value items from the approved plan:

1. **Inventory commit on `OrderConfirmedEvent`** — `QuantityOnHand` is reduced when an order is confirmed
2. **Inventory release on `OrderCancelledEvent`** — reserved stock is returned when an order is cancelled
3. **Payment capture on `OrderConfirmedEvent`** — payment transitions from `Authorized` → `Captured`
4. **`OrderConfirmedEvent` and `OrderCancelledEvent` carry items list** — downstream services know which products were affected
5. **Cart checkout endpoint** (`POST /api/cart/checkout`) — converts cart to an order via service call to Orders API, then clears the cart
6. **Correlation ID middleware** — `X-Correlation-Id` propagated through all requests and logged
7. **Outbox background worker** (`OutboxWorker<TContext>`) — polls `OutboxMessages` table every 10 s; re-publishes unprocessed messages (at-least-once delivery)
8. **Per-service documentation** — `docs/services/` with endpoint tables, state machines, and roadmap per service
9. **Updated architecture docs and TODO tracker**

## Phase 2 — Checkout Flow (Previously Implemented)

1. **Orders** — Place order → persists to DB → publishes `OrderPlacedEvent`
2. **Inventory** — Consumes `OrderPlacedEvent` → attempts stock reservation → publishes `InventoryReservedEvent` or `InventoryReservationFailedEvent`
3. **Payments** — Consumes `InventoryReservedEvent` → simulates payment authorization → publishes `PaymentAuthorizedEvent` or `PaymentFailedEvent`
4. **Orders** — Consumes inventory/payment events → advances order state machine
5. **Notifications** — Consumes notification commands → logs to PostgreSQL

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

Each service uses its own database schema within a single PostgreSQL instance. Schemas are created automatically on first startup via `EnsureCreated()`.

Default credentials: `postgres` / `postgres` on `localhost:5432`.

| Service       | Database                  |
|---------------|---------------------------|
| Catalog       | ecommerce_catalog         |
| Cart          | ecommerce_cart            |
| Orders        | ecommerce_orders          |
| Inventory     | ecommerce_inventory       |
| Payments      | ecommerce_payments        |
| Notifications | ecommerce_notifications   |

Create these databases before first run:

```sql
CREATE DATABASE ecommerce_catalog;
CREATE DATABASE ecommerce_cart;
CREATE DATABASE ecommerce_orders;
CREATE DATABASE ecommerce_inventory;
CREATE DATABASE ecommerce_payments;
CREATE DATABASE ecommerce_notifications;
```

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

## Sample End-to-End Checkout (Local)

1. Authenticate with Keycloak and obtain a JWT bearer token.
2. Add products to the catalog:
   ```
   POST /api/products  { "name": "Widget", "price": 29.99, "category": "Tools" }
   ```
3. Create inventory for the product:
   ```
   POST /api/inventory  { "productId": "...", "productName": "Widget", "quantity": 100 }
   ```
4. Add items to the cart:
   ```
   POST /api/cart/items  { "productId": "...", "productName": "Widget", "unitPrice": 29.99, "quantity": 2 }
   ```
5. Checkout from the cart:
   ```
   POST /api/cart/checkout  { "customerEmail": "me@example.com", "customerName": "Jane", "idempotencyKey": "unique-uuid" }
   ```
6. Observe the event chain in the logs and check order/payment/notification status:
   ```
   GET /api/orders/{orderId}
   GET /api/payments/orders/{orderId}
   GET /api/notifications/orders/{orderId}
   ```

## Integration Events

| Event | Publisher | Consumers |
|-------|-----------|-----------|
| `OrderPlacedEvent` | Orders | Inventory |
| `InventoryReservedEvent` | Inventory | Payments, Orders |
| `InventoryReservationFailedEvent` | Inventory | Orders |
| `PaymentAuthorizedEvent` | Payments | Orders |
| `PaymentFailedEvent` | Payments | Orders |
| `OrderConfirmedEvent` (with items) | Orders | Inventory (commit), Payments (capture) |
| `OrderCancelledEvent` (with items) | Orders | Inventory (release) |
| `SendOrderConfirmationNotificationCommand` | Orders | Notifications |
| `SendOrderCancelledNotificationCommand` | Orders | Notifications |

## Service Endpoints Summary

### Catalog (`/api/products`)
- `GET /api/products` — list products (paginated, filterable by category)
- `GET /api/products/{id}` — get product by ID
- `POST /api/products` — create product [auth]
- `PUT /api/products/{id}` — update product [auth]
- `DELETE /api/products/{id}` — deactivate product [auth]

### Cart (`/api/cart`)
- `GET /api/cart` — get or create cart for current user [auth]
- `POST /api/cart/items` — add item to cart [auth]
- `PUT /api/cart/items` — update item quantity [auth]
- `DELETE /api/cart/items/{productId}` — remove item [auth]
- `DELETE /api/cart` — clear cart [auth]
- `POST /api/cart/checkout` — **checkout: convert cart to order and clear** [auth]

### Orders (`/api/orders`)
- `GET /api/orders` — list orders for current user (paginated) [auth]
- `GET /api/orders/{id}` — get order by ID [auth]
- `POST /api/orders` — place order (idempotent via `IdempotencyKey`) [auth]
- `POST /api/orders/{id}/cancel` — cancel order [auth]

### Inventory (`/api/inventory`)
- `GET /api/inventory/products/{productId}` — get stock level
- `GET /api/inventory/low-stock` — get low-stock items [auth]
- `POST /api/inventory` — create inventory item [auth]
- `POST /api/inventory/products/{productId}/restock` — restock [auth]

### Payments (`/api/payments`)
- `GET /api/payments/orders/{orderId}` — get payment status for order [auth]

### Notifications (`/api/notifications`)
- `GET /api/notifications/status` — service health status
- `GET /api/notifications` — list recent notification logs [auth]
- `GET /api/notifications/orders/{orderId}` — notification history for order [auth]

## Health Checks

Each service exposes `GET /health`. The gateway exposes `GET /health`.

## Docs

See [`docs/architecture/OVERVIEW.md`](docs/architecture/OVERVIEW.md) for detailed architecture and event flows.  
See [`docs/services/`](docs/services/) for per-service documentation.  
See [`docs/TODO.md`](docs/TODO.md) for phase progress and future work.
