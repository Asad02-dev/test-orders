# Orders Service

## Responsibility
Manages the full order lifecycle: placement, state transitions (via async event consumers), and cancellation. Acts as the **saga coordinator** for the checkout workflow.

## Ports & Config
| Setting | Default |
|---------|---------|
| Port | 5103 |
| Database | `ecommerce_orders` (PostgreSQL) |

## Endpoints
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/orders` | ✅ | Get paginated orders for current user |
| GET | `/api/orders/{id}` | ✅ | Get order by ID |
| POST | `/api/orders` | ✅ | Place order (idempotent) |
| POST | `/api/orders/{id}/cancel` | ✅ | Cancel order |
| GET | `/health` | — | Health check |

### Place Order Request
```json
POST /api/orders
{
  "customerEmail": "customer@example.com",
  "customerName": "Jane Doe",
  "idempotencyKey": "unique-key",
  "items": [
    { "productId": "uuid", "productName": "Widget", "unitPrice": 29.99, "quantity": 2 }
  ]
}
```

## Order State Machine
```
Pending → ReservationConfirmed → PaymentAuthorized → Confirmed
      ↘ Cancelled (on any failure)
```

## Integration Events

### Published
| Event | When |
|-------|------|
| `OrderPlacedEvent` | After successful order creation |
| `OrderConfirmedEvent` (with items) | After payment authorized |
| `OrderCancelledEvent` (with items) | On cancellation |
| `SendOrderConfirmationNotificationCommand` | After order confirmed |
| `SendOrderCancelledNotificationCommand` | After order cancelled |

### Consumed
| Event | Action |
|-------|--------|
| `InventoryReservedEvent` | → `ReservationConfirmed` |
| `InventoryReservationFailedEvent` | → `Cancelled` |
| `PaymentAuthorizedEvent` | → `Confirmed` |
| `PaymentFailedEvent` | → `Cancelled` |

## Idempotency
- `IdempotencyKey` unique index — duplicate submissions return the existing order
- All event consumers check current status before transitioning

## Current Status (Phase 3)
- [x] Order placement with idempotency
- [x] Full state machine (Pending → Confirmed / Cancelled)
- [x] All event consumers wired
- [x] OrderConfirmedEvent and OrderCancelledEvent now carry items list
- [x] Outbox table present; OutboxWorker polls and re-publishes on startup
- [x] Correlation ID middleware

## Future Work
- [ ] EF Core migrations (currently uses `EnsureCreated`)
- [ ] Admin endpoint to list all orders (not filtered by customer)
- [ ] Order history / audit trail
