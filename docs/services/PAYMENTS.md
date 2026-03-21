# Payments Service

## Responsibility
Processes payment authorization triggered by `InventoryReservedEvent` and captures payments when orders are confirmed. Uses a local fake provider for development.

## Ports & Config
| Setting | Default |
|---------|---------|
| Port | 5105 |
| Database | `ecommerce_payments` (PostgreSQL) |

## Endpoints
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/payments/orders/{orderId}` | ✅ | Get payment status for order |
| GET | `/health` | — | Health check |

## Payment State Machine
```
Pending → Authorized → Captured
       ↘ Failed
       (also Refunded from Authorized/Captured)
```

## Fake Provider Logic
- Payments ≤ 10,000 → Authorized
- Payments > 10,000 → Failed

## Integration Events

### Published
| Event | When |
|-------|------|
| `PaymentAuthorizedEvent` | Simulated authorization succeeds |
| `PaymentFailedEvent` | Simulated authorization fails |

### Consumed
| Event | Action |
|-------|--------|
| `InventoryReservedEvent` | Process payment authorization |
| `OrderConfirmedEvent` | **Capture authorized payment** (Phase 3) |

## Idempotency
- `idempotency-{orderId}` key prevents duplicate payment attempts for the same order

## Current Status (Phase 3)
- [x] Payment authorization on `InventoryReservedEvent`
- [x] **Payment capture on `OrderConfirmedEvent`** (Phase 3)
- [x] PostgreSQL persistence
- [x] Idempotency via key
- [x] Outbox table; OutboxWorker enabled
- [x] Correlation ID middleware

## Future Work
- [ ] Real payment provider integration (Stripe/PayPal)
- [ ] Refund endpoint
- [ ] Payment webhook handling
- [ ] EF Core migrations
