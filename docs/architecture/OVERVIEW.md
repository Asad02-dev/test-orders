# Architecture Overview

## Service Responsibilities

| Service | Responsibility |
|---------|---------------|
| Gateway | Route external traffic, forward auth headers |
| Catalog | Product CRUD, categories, pricing, seeded sample data |
| Cart | Customer shopping cart (get-or-create, add/update/remove/clear items, **checkout**) |
| Orders | Order placement with idempotency, state machine, cancellation, event-driven state transitions |
| Inventory | Stock tracking, **reserve on OrderPlaced**, **commit on OrderConfirmed**, **release on OrderCancelled** |
| Payments | Payment authorization triggered by `InventoryReservedEvent`, **capture on OrderConfirmed** (simulated local provider) |
| Notifications | Email/notification delivery (log-based with PostgreSQL persistence) |

## Checkout Flow (Phase 2+3 — Implemented)

```
Customer → Gateway → Cart.Checkout (POST /api/cart/checkout)
  → Cart reads cart items → calls Orders API (POST /api/orders)
    → Orders persists Order (Pending) → publishes OrderPlacedEvent
      → Inventory.OrderPlacedConsumer → tries reservation
        [success] → updates QuantityReserved, publishes InventoryReservedEvent
          → Orders.InventoryReservedConsumer → Order: ReservationConfirmed
          → Payments.InventoryReservedConsumer → processes payment
            [success] → publishes PaymentAuthorizedEvent
              → Orders.PaymentAuthorizedConsumer → Order: Confirmed
                → publishes OrderConfirmedEvent (with items)
                  → Inventory.OrderConfirmedConsumer → commits reservation (QuantityOnHand -= qty)
                  → Payments.OrderConfirmedConsumer  → captures payment (Authorized → Captured)
                → publishes SendOrderConfirmationNotificationCommand
                  → Notifications → logs confirmation to DB
            [failure] → publishes PaymentFailedEvent
              → Orders.PaymentFailedConsumer → Order: Cancelled
                → publishes OrderCancelledEvent (with items)
                  → Inventory.OrderCancelledConsumer → releases reservation (QuantityReserved -= qty)
                → publishes SendOrderCancelledNotificationCommand
                  → Notifications → logs cancellation to DB
        [failure] → publishes InventoryReservationFailedEvent
          → Orders.InventoryReservationFailedConsumer → Order: Cancelled
            → publishes OrderCancelledEvent (with items)
              → Inventory.OrderCancelledConsumer → releases reservation (no-op if not reserved)
            → publishes SendOrderCancelledNotificationCommand
              → Notifications → logs cancellation to DB
```

## Order State Machine

```
Pending
  ├─→ ReservationConfirmed  (on InventoryReservedEvent)
  │     └─→ PaymentAuthorized  (on PaymentAuthorizedEvent)
  │           └─→ Confirmed    (on PaymentAuthorizedEvent, happy path complete)
  └─→ Cancelled  (on InventoryReservationFailedEvent, PaymentFailedEvent, or manual cancel)
```

## Payment State Machine

```
Pending → Authorized → Captured
       ↘ Failed
```

## Integration Event Summary

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

## Correlation ID

All services include `CorrelationIdMiddleware` which:
- Reads `X-Correlation-Id` request header (or generates a new one)
- Adds `CorrelationId` to Serilog `LogContext` for structured logging
- Returns the correlation ID in the response header

## Outbox Pattern (Phase 3)

`OutboxWorker<TContext>` is a background service registered in Orders, Inventory, and Payments. It:
- Polls the `OutboxMessages` table every 10 seconds
- Publishes unprocessed messages via MassTransit
- Retries up to 5 times with error tracking
- Guarantees at-least-once delivery even if RabbitMQ is temporarily unavailable

## Notification Persistence

The Notifications service persists every notification request to the `notifications.NotificationLogs` table, including type, recipient email, subject, body, timestamp, and sent status.

## Roadmap / Future Work

See [`../TODO.md`](../TODO.md) for a full breakdown of completed and remaining work across all phases.
