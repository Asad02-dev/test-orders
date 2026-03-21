# Architecture Overview

## Service Responsibilities

| Service | Responsibility |
|---------|---------------|
| Gateway | Route external traffic, forward auth headers |
| Catalog | Product CRUD, categories, pricing, seeded sample data |
| Cart | Customer shopping cart (get-or-create, add/update/remove/clear items) |
| Orders | Order placement with idempotency, state machine, cancellation, event-driven state transitions |
| Inventory | Stock tracking, reservations on `OrderPlacedEvent`, restock API |
| Payments | Payment authorization triggered by `InventoryReservedEvent` (simulated local provider) |
| Notifications | Email/notification delivery (log-based with PostgreSQL persistence) |

## Checkout Flow (Phase 2 — Implemented)

```
Customer → Gateway → Orders.PlaceOrder
  → persists Order (Pending) → publishes OrderPlacedEvent
    → Inventory.OrderPlacedConsumer → tries reservation
      [success] → updates stock, publishes InventoryReservedEvent (with CustomerId + TotalAmount)
        → Orders.InventoryReservedConsumer → Order status: ReservationConfirmed
        → Payments.InventoryReservedConsumer → processes payment
          [success] → publishes PaymentAuthorizedEvent
            → Orders.PaymentAuthorizedConsumer → Order status: Confirmed
              → publishes OrderConfirmedEvent
              → publishes SendOrderConfirmationNotificationCommand
                → Notifications → logs notification to DB
          [failure] → publishes PaymentFailedEvent
            → Orders.PaymentFailedConsumer → Order status: Cancelled
              → publishes SendOrderCancelledNotificationCommand
                → Notifications → logs notification to DB
      [failure] → publishes InventoryReservationFailedEvent
        → Orders.InventoryReservationFailedConsumer → Order status: Cancelled
          → publishes SendOrderCancelledNotificationCommand
            → Notifications → logs notification to DB
```

## Order State Machine

```
Pending
  ├─→ ReservationConfirmed  (on InventoryReservedEvent)
  │     └─→ PaymentAuthorized  (on PaymentAuthorizedEvent)
  │           └─→ Confirmed  (on PaymentAuthorizedEvent, final happy path)
  ├─→ Cancelled  (on InventoryReservationFailedEvent, PaymentFailedEvent, or manual cancel)
  └─→ (future) Failed
```

## Database Strategy

Each service uses its own PostgreSQL schema within a single PostgreSQL instance. Schema and tables are created automatically via `EnsureCreated()` on startup.

| Service       | Schema         | Key Entities                    |
|---------------|----------------|---------------------------------|
| Catalog       | catalog        | Product, OutboxMessage          |
| Cart          | cart           | Cart, CartItem, OutboxMessage   |
| Orders        | orders         | Order, OrderItem, OutboxMessage |
| Inventory     | inventory      | InventoryItem, OutboxMessage    |
| Payments      | payments       | Payment, OutboxMessage          |
| Notifications | notifications  | NotificationLog                 |

## Idempotency

- **Orders**: `IdempotencyKey` unique index — duplicate submissions return the existing order
- **Payments**: `idempotency-{orderId}` key pattern — duplicate payment processing is a no-op
- **Order state transitions**: all event consumers check current status before transitioning — safe to re-deliver events

## Notification Persistence

The Notifications service persists every notification request to the `notifications.NotificationLogs` table, including:
- Type (OrderConfirmation / OrderCancelled)
- Recipient email
- Subject and body
- Timestamp and sent status

## Outbox Pattern

`OutboxMessage` table exists in each service's DbContext (Catalog, Cart, Orders, Inventory, Payments). Future implementation: background worker polls unprocessed outbox messages and publishes them — guaranteeing at-least-once delivery even if RabbitMQ is temporarily unavailable.

## Roadmap / Future Work

### Phase 3 — Hardening
- [ ] Implement outbox background worker per service (reliable event publishing)
- [ ] Add proper EF Core migrations (replace `EnsureCreated`)
- [ ] Add rate limiting at gateway
- [ ] Add request aggregation in gateway (cart + order summary in one call)
- [ ] Integrate real email provider (SendGrid/SMTP)

### Phase 4 — Fulfillment & Admin
- [ ] Add Shipping service (shipment creation, tracking)
- [ ] Add admin endpoints (order management, inventory admin)
- [ ] Inventory commit on order confirmed (reduce QuantityOnHand)
- [ ] Inventory release on order cancelled

### Phase 5 — Observability & Performance
- [ ] Add OpenTelemetry exporter (Jaeger/OTLP)
- [ ] Add caching layer (Redis) for Catalog reads
- [ ] Add Polly resilience policies (circuit breaker, retry)
- [ ] Add integration tests
- [ ] Add CI/CD pipeline
