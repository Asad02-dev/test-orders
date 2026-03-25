# Cart Service

## Responsibility
Manages customer shopping carts: add, update, remove, and clear items, and provides a **checkout endpoint** that converts the cart into an order.

## Ports & Config
| Setting | Default |
|---------|---------|
| Port | 5102 |
| Database | `ecommerce_cart` (PostgreSQL) |
| Orders API URL | `Services:OrdersApi` → `http://localhost:5103` |

## Endpoints
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/cart` | ✅ | Get or create current user's cart |
| POST | `/api/cart/items` | ✅ | Add item to cart |
| PUT | `/api/cart/items` | ✅ | Update item quantity |
| DELETE | `/api/cart/items/{productId}` | ✅ | Remove item |
| DELETE | `/api/cart` | ✅ | Clear cart |
| POST | `/api/cart/checkout` | ✅ | Place order from cart and clear it |
| GET | `/health` | — | Health check |

### Checkout Request
```json
POST /api/cart/checkout
{
  "customerEmail": "customer@example.com",
  "customerName": "Jane Doe",
  "idempotencyKey": "unique-checkout-key-uuid"
}
```
The service reads the authenticated user's cart, calls the Orders API to place an order, and clears the cart on success.

## Domain Model
- **Cart** — aggregate root; one per customer
- **CartItem** — product snapshot (id, name, unit price, quantity)

## Dependencies
- **Orders.Api** — HTTP call via typed `CartCheckoutService` to `POST /api/orders`
- No messaging consumers (stateless cart management)

## Configuration (`appsettings.json`)
```json
{
  "Services": {
    "OrdersApi": "http://localhost:5103"
  }
}
```

## Current Status (Phase 3)
- [x] Cart CRUD (get/create, add/update/remove/clear items)
- [x] PostgreSQL persistence
- [x] Checkout endpoint → Orders API service call
- [x] Correlation ID propagation

## Future Work
- [ ] Forward Keycloak bearer token from cart checkout to Orders API (for auth)
- [ ] Cart expiry / TTL
- [ ] Promo code hooks
