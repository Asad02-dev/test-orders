# Catalog Service

## Responsibility
Manages the product catalog: CRUD for products, categories, and pricing. Includes seed data on first startup.

## Ports & Config
| Setting | Default |
|---------|---------|
| Port | 5101 |
| Database | `ecommerce_catalog` (PostgreSQL) |

## Endpoints
| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `/api/products` | — | List products (paginated, filter by category) |
| GET | `/api/products/{id}` | — | Get product by ID |
| POST | `/api/products` | ✅ | Create product |
| PUT | `/api/products/{id}` | ✅ | Update product |
| DELETE | `/api/products/{id}` | ✅ | Deactivate product |
| GET | `/health` | — | Health check |

## Domain Model
- **Product** — id, name, description, price, category, stock display metadata, active flag

## Current Status
- [x] Full CRUD with PostgreSQL persistence
- [x] Seed data on first run
- [x] Pagination and category filter
- [x] Correlation ID middleware

## Future Work
- [ ] Product images / media
- [ ] Category management endpoints
- [ ] Price history / discounts
- [ ] Inventory integration for stock display
- [ ] EF Core migrations
