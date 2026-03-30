---
applyTo: "**/Services/**,**/BuildingBlocks/**,**/Gateway/**"
description: "Platform architecture reference for debugging. Covers service topology, saga flow, shared libraries, domain patterns, data access, and consumer conventions."
---

# Platform Architecture — Debugging Reference

## Services & Ports

| Service | Port (YARP / API) | Database | Has Domain Layer |
|---------|-------------------|----------|------------------|
| Catalog | 5101 / 5219 | ecommerce_catalog | Yes |
| Cart | 5102 / 5174 | ecommerce_cart | Yes |
| Orders | 5103 / 5189 | ecommerce_orders | Yes |
| Inventory | 5104 / 5110 | ecommerce_inventory | Yes |
| Payments | 5105 / 5257 | ecommerce_payments | Yes |
| Notifications | 5106 / 5156 | ecommerce_notifications | No |

## 4-Layer Clean Architecture (per service)

```
{Service}.Api            → Minimal API endpoints (Program.cs, top-level statements)
{Service}.Application    → DTOs, application services, MassTransit consumers
{Service}.Domain         → Entities, enums, domain events, repository interfaces
{Service}.Infrastructure → DbContext, repositories, DI wiring (Extensions/)
```

**Dependency rule (never inverted):**
- Domain → SharedKernel only
- Application → Domain + Contracts + Messaging
- Infrastructure → Application + Persistence + Messaging
- Api → Infrastructure + Application + Authentication + Observability

## Choreography Saga Flow

```
OrderPlaced
  → Inventory reserves stock → InventoryReserved
    → Payments authorizes → PaymentAuthorized
      → Orders confirms → OrderConfirmed
        → [Inventory commits, Payments captures]

Failure path:
  InventoryReservationFailed / PaymentFailed
    → OrderCancelled
      → [Inventory releases, Notification sent]
```

## Shared BuildingBlocks

| Library | Key Extension | Location |
|---------|--------------|----------|
| SharedKernel | Entity, AggregateRoot, Result<T>, IUnitOfWork | src/BuildingBlocks/SharedKernel/ |
| Contracts | Integration events (IntegrationEvent base record) | src/BuildingBlocks/Contracts/Events/ |
| Messaging | `AddRabbitMqMessaging()` / `AddRabbitMqMessagingWithConsumers()` | src/BuildingBlocks/Messaging/ |
| Persistence | `AddPostgresDbContext<T>()`, `AddOutboxWorker<T>()`, OutboxMessage | src/BuildingBlocks/Persistence/ |
| Authentication | `AddKeycloakAuthentication()` | src/BuildingBlocks/Authentication/ |
| Observability | `AddObservability()`, `UseCorrelationId()`, `UseRequestLogging()` | src/BuildingBlocks/Observability/ |

## Key Domain Patterns

- **Rich domain model**: private setters, static `Create()` factories, state transition methods with guards.
- **State machines**: transitions throw `InvalidOperationException` on invalid source state.
- **Result<T>**: used for business failures (not exceptions).
- **Concurrency**: `AggregateRoot<TId>.Version` is EF Core concurrency token — `Version++` on every mutation.
- **Idempotency**: unique keys on Orders and Payments, consumer-level status guards.
- **No EF migrations**: services use `EnsureCreated()` at startup. Schema changes require DB recreation.
- **No domain event dispatcher**: `AddDomainEvent` exists but nothing dispatches them. Integration events published directly via `IPublishEndpoint`.

## Data Access Patterns

- DbContext implements `IUnitOfWork` directly (no separate wrapper).
- Schema isolation: `HasDefaultSchema("{service}")`.
- Inline Fluent API in `OnModelCreating` (no `IEntityTypeConfiguration`).
- Enums stored as strings: `.HasConversion<string>()`.
- `OutboxMessage` mapped in every participating DbContext.
- `OutboxWorker<TContext>` polls every 10s, batch 20, max 5 retries.

## Consumer Patterns

- Consumers in `Application/Consumers/`, NOT Infrastructure.
- Named `{EventName}Consumer`, implement `IConsumer<TEvent>`.
- Include status guards for idempotency.
- MassTransit retry: exponential, 3 retries, 1–15s.
- Type-name collisions resolved with `using` aliases.
