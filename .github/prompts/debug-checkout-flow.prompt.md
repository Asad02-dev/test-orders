---
description: "Traces the full checkout saga chain (Order → Inventory → Payment → Confirmation) with correlation ID for debugging event flow issues."
agent: "agent"
tools: [read, search, execute]
argument-hint: "OrderId or CorrelationId to trace, or describe the symptom"
---

# Debug Checkout Flow

Trace the choreography-based saga for the ECommerce checkout flow. The event chain is:

```
OrderPlaced → Inventory reserves → InventoryReserved
  → Payments authorizes → PaymentAuthorized
    → Orders confirms → OrderConfirmed → [Inventory commits, Payments captures]

Failure: InventoryReservationFailed / PaymentFailed → OrderCancelled → [Inventory releases, Notification sent]
```

## Debugging Approach

1. **Identify the stuck point** — Which event in the chain was the last one processed?

2. **Check consumers** — Read each consumer in the chain to verify status guards and event handling:
   - [Orders] `InventoryReservedConsumer`, `InventoryReservationFailedConsumer`, `PaymentAuthorizedConsumer`, `PaymentFailedConsumer`
   - [Inventory] `OrderPlacedConsumer`, `OrderConfirmedConsumer`, `OrderCancelledConsumer`
   - [Payments] `InventoryReservedConsumer`, `OrderConfirmedConsumer`
   - [Notifications] `SendOrderConfirmationNotificationConsumer`, `SendOrderCancelledNotificationConsumer`

3. **Verify state machine transitions** — Check that the domain entity allows the transition from the current status. Key files:
   - `Orders.Domain/Entities/Order.cs` — state transitions
   - `Inventory.Domain/Entities/InventoryItem.cs` — stock reserve/commit/release
   - `Payments.Domain/Entities/Payment.cs` — authorize/capture transitions

4. **Check integration events** — Verify event payloads match what consumers expect:
   - `Contracts/Events/Order/` — OrderPlacedEvent, OrderConfirmedEvent, OrderCancelledEvent
   - `Contracts/Events/Inventory/` — InventoryReservedEvent, InventoryReservationFailedEvent
   - `Contracts/Events/Payment/` — PaymentAuthorizedEvent, PaymentFailedEvent

5. **Check outbox** — Verify events aren't stuck in the outbox table. Look at `OutboxWorker` and `OutboxMessage` mapping in each DbContext.

6. **Check idempotency** — Consumers use status guards. If a message is redelivered, the consumer may skip it because the entity has already transitioned.

7. **Correlation ID** — If a correlation ID is provided, search for it across service logs. The `X-Correlation-Id` header is propagated through all services via `CorrelationIdMiddleware`.

## Output

Provide:
- The exact point where the flow breaks or stalls
- The root cause (missing consumer, wrong status guard, event payload mismatch, outbox stuck, etc.)
- A fix or next debugging step
