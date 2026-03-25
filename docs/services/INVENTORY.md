# Inventory Service

## Responsibility
Tracks stock levels, processes reservations when orders are placed, commits reservations when orders are confirmed, and releases reservations when orders are cancelled.

## Ports & Config
| Setting | Default |
|---------|---------|
| Port | 5104 |
| Database | `ecommerce_inventory` (PostgreSQL) |

## Endpoints
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/inventory/products/{productId}` | — | Get stock level for product |
| GET | `/api/inventory/low-stock` | ✅ | List low-stock items |
| POST | `/api/inventory` | ✅ | Create inventory item |
| POST | `/api/inventory/products/{productId}/restock` | ✅ | Restock product |
| GET | `/health` | — | Health check |

## Stock Model
Each `InventoryItem` tracks:
- `QuantityOnHand` — physical stock
- `QuantityReserved` — held for pending orders
- `AvailableQuantity = QuantityOnHand - QuantityReserved`
- `ReorderThreshold` — triggers low-stock query

## Integration Events

### Published
| Event | When |
|-------|------|
| `InventoryReservedEvent` | Stock successfully reserved |
| `InventoryReservationFailedEvent` | Insufficient stock |

### Consumed
| Event | Action |
|-------|--------|
| `OrderPlacedEvent` | Reserve stock for each order item |
| `OrderConfirmedEvent` | Commit reservation: reduce `QuantityOnHand` |
| `OrderCancelledEvent` | Release reservation: reduce `QuantityReserved` |

## Current Status (Phase 3)
- [x] Stock tracking (OnHand, Reserved, Available)
- [x] Reservation on `OrderPlacedEvent`
- [x] **Commit reservation on `OrderConfirmedEvent`** (Phase 3)
- [x] **Release reservation on `OrderCancelledEvent`** (Phase 3)
- [x] Restock API
- [x] Low-stock query
- [x] Outbox table; OutboxWorker enabled
- [x] Correlation ID middleware

## Future Work
- [ ] Low-stock events / notifications
- [ ] Inventory adjustment audit log
- [ ] EF Core migrations
