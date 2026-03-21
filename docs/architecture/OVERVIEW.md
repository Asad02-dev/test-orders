# Architecture Overview

## Service Responsibilities

| Service | Responsibility |
|---------|---------------|
| Gateway | Route external traffic, forward auth headers |
| Catalog | Product CRUD, categories, pricing |
| Cart | Customer shopping cart (get-or-create, add/update/remove items) |
| Orders | Order placement, idempotency, state machine, cancellation |
| Inventory | Stock tracking, reservations, restock |
| Payments | Payment authorization (local fake provider for dev) |
| Notifications | Email/notification delivery (log-based for dev) |

## Checkout Flow

```
Customer → Gateway → Orders.PlaceOrder
  → publishes OrderPlacedEvent
    → Inventory.OrderPlacedConsumer → tries reservation
      [success] → publishes InventoryReservedEvent
        → Payments processes payment
          [success] → publishes PaymentAuthorizedEvent → Order confirmed
          [failure] → publishes PaymentFailedEvent → Order cancelled
      [failure] → publishes InventoryReservationFailedEvent → Order cancelled
  → Orders publishes SendOrderConfirmationNotificationCommand
    → Notifications sends email
```

## Database Strategy

Each service uses its own PostgreSQL schema (e.g. `catalog`, `cart`, `orders`, `inventory`, `payments`). This provides logical isolation while using a single PostgreSQL instance for local development. Future evolution to separate databases per service requires only connection string changes and migration moves.

## Outbox Pattern

`OutboxMessage` table exists in each service's DbContext. Future implementation: background worker polls unprocessed outbox messages and publishes them — guaranteeing at-least-once delivery even if RabbitMQ is temporarily unavailable.

## Idempotency

- Orders use `IdempotencyKey` (unique index) — duplicate order submissions return the existing order.
- Payments use `idempotency-{orderId}` key pattern.

## Roadmap / Future Work

- [ ] Implement outbox background worker per service
- [ ] Add order saga/orchestrator for checkout flow
- [ ] Add EF Core migrations per service
- [ ] Add rate limiting at gateway
- [ ] Add caching layer (Redis) for Catalog reads
- [ ] Integrate real email provider (SendGrid/SMTP)
- [ ] Add Shipping service
- [ ] Add admin endpoints
- [ ] Add integration tests
- [ ] Add CI/CD pipeline
