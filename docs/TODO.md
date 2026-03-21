# ECommerce Platform — TODO & Progress

## Completed Phases

### Phase 1 — Foundation ✅
- [x] Solution structure (monorepo, .NET 10 SDK)
- [x] YARP gateway (`Gateway.Api`)
- [x] Keycloak JWT bearer authentication (`Authentication` building block)
- [x] PostgreSQL persistence via EF Core + Npgsql (`Persistence` building block)
- [x] MassTransit + RabbitMQ messaging (`Messaging` building block)
- [x] Serilog + OpenTelemetry observability (`Observability` building block)
- [x] Shared domain primitives: `Entity`, `AggregateRoot`, `ValueObject`, `Result`, `CorrelationContext` (`SharedKernel`)
- [x] Integration event contracts (`Contracts`)
- [x] Health checks per service (`/health`)
- [x] ProblemDetails error responses
- [x] OpenAPI/Swagger per service

### Phase 2 — Core Commerce & Checkout Flow ✅
- [x] Catalog service: product CRUD, categories, seed data, pagination
- [x] Cart service: get/create, add/update/remove/clear items
- [x] Orders service: place order, order state machine, idempotency
- [x] Inventory service: stock tracking, reservation on `OrderPlacedEvent`
- [x] Payments service: authorization on `InventoryReservedEvent`, simulated fake provider
- [x] Notifications service: log confirmation/cancellation notifications to PostgreSQL
- [x] End-to-end checkout event chain:
  `OrderPlaced → InventoryReserved → PaymentAuthorized → OrderConfirmed → Notification`
- [x] Cancellation chain:
  `InventoryReservationFailed / PaymentFailed → OrderCancelled → Notification`

### Phase 3 — Hardening & Completion ✅ *(this PR)*
- [x] **Inventory commit on `OrderConfirmedEvent`** — `QuantityOnHand` reduced on confirmation
- [x] **Inventory release on `OrderCancelledEvent`** — `QuantityReserved` returned on cancellation
- [x] **Payment capture on `OrderConfirmedEvent`** — payment moves from `Authorized` → `Captured`
- [x] **`OrderConfirmedEvent` and `OrderCancelledEvent` carry items list** — downstream services know which products were affected
- [x] **Cart checkout endpoint** (`POST /api/cart/checkout`) — converts cart to order via Orders API and clears cart
- [x] **Correlation ID middleware** — `X-Correlation-Id` header propagated through all services and logged
- [x] **Outbox background worker** (`OutboxWorker<TContext>`) — polls `OutboxMessages` table every 10 seconds and re-publishes any unprocessed messages (at-least-once delivery)
- [x] Per-service documentation files (`docs/services/`)
- [x] Updated root README and architecture OVERVIEW

---

## Phase 4 — Fulfillment & Admin (Future)
- [ ] Shipping service: shipment creation, tracking abstraction
- [ ] Admin order management endpoints (list all orders, override status)
- [ ] Inventory adjustment audit log
- [ ] Bearer token forwarding from Cart checkout → Orders API (service-to-service auth)
- [ ] `POST /api/payments/{orderId}/refund` endpoint

## Phase 5 — Observability & Resilience (Future)
- [ ] Replace `EnsureCreated` with proper EF Core migrations in all services
- [ ] OpenTelemetry exporter (Jaeger / OTLP)
- [ ] Rate limiting at gateway (per route, per user)
- [ ] Request aggregation in gateway (cart + catalog summary)
- [ ] Redis caching for Catalog reads
- [ ] Polly circuit breaker / retry policies for outbound HTTP (Cart → Orders checkout)
- [ ] Integration tests (at least for checkout flow)
- [ ] CI/CD pipeline (GitHub Actions: build, test, lint)

## Phase 6 — Production Readiness (Future)
- [ ] Real email delivery (SendGrid / SMTP)
- [ ] Real payment provider (Stripe)
- [ ] Angular client application
- [ ] Multi-environment configuration (staging, production)
- [ ] Secret management (Azure Key Vault / AWS Secrets Manager)
- [ ] Horizontal scaling considerations
