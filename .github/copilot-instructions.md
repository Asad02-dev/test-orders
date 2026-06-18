# Copilot Instructions — ECommerce Microservices Platform

## Project Overview

.NET 10 API-first e-commerce platform built as a microservice monorepo. Six domain services behind a YARP gateway, communicating via MassTransit/RabbitMQ choreography-based saga. PostgreSQL per-service databases, Keycloak JWT auth, Serilog + OpenTelemetry observability.

## Build & Run

```bash
# Build entire solution
dotnet build ECommerce.slnx

# Run a specific service
dotnet run --project src/Services/Orders/Orders.Api

# Run the gateway
dotnet run --project src/Gateway/Gateway.Api
```

**Prerequisites**: .NET 10 SDK, PostgreSQL 16+ (localhost:5432), RabbitMQ 3.13+ (localhost:5672), Keycloak 24+ (localhost:8080).

**No test projects exist yet.** No `Directory.Build.props` or central package management. No EF Core migrations — services use `EnsureCreated()` at startup.

## Architecture & Layering

Each service follows strict **Clean Architecture** with four projects:

```
{Service}.Api            → Minimal API endpoints in Program.cs (top-level statements, no controllers)
{Service}.Application    → DTOs, application services, MassTransit consumers
{Service}.Domain         → Entities, enums, domain events, repository interfaces
{Service}.Infrastructure → DbContext, concrete repositories, DI wiring
```

Exception: `Notifications` has no Domain layer.

**Dependency rule** (never invert):
```
Domain → SharedKernel only
Application → Domain + Contracts + Messaging
Infrastructure → Application + Persistence + Messaging
Api → Infrastructure + Application + Authentication + Observability
```

### BuildingBlocks (shared libraries)

| Library | Purpose | Key extension method |
|---------|---------|---------------------|
| SharedKernel | `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `Result<T>`, `CorrelationContext` | — |
| Contracts | Integration event records shared across services | — |
| Messaging | MassTransit + RabbitMQ configuration | `AddRabbitMqMessaging()` / `AddRabbitMqMessagingWithConsumers()` |
| Persistence | EF Core + Npgsql setup, `OutboxMessage`, `OutboxWorker<T>` | `AddPostgresDbContext<T>()`, `AddOutboxWorker<T>()` |
| Authentication | Keycloak JWT bearer | `AddKeycloakAuthentication()` |
| Observability | Serilog + OpenTelemetry + correlation middleware | `AddObservability()`, `UseCorrelationId()` |

## Naming Conventions

| Element | Pattern | Example |
|---------|---------|---------|
| Project | `{Service}.{Layer}` | `Orders.Api`, `Orders.Domain` |
| Entity | Singular noun | `Order`, `OrderItem`, `Payment` |
| Enum | `{Entity}Status` in `Enums/` | `OrderStatus`, `PaymentStatus` |
| Domain event | `{Entity}{Action}DomainEvent` record in `Events/` | `OrderPlacedDomainEvent` |
| Integration event | `{Entity}{PastTense}Event` record in `Contracts/Events/{Subdomain}/` | `OrderPlacedEvent`, `InventoryReservedEvent` |
| Failure event | `{Entity}{Action}FailedEvent` | `PaymentFailedEvent` |
| Notification command | `Send{Action}NotificationCommand` | `SendOrderConfirmationNotificationCommand` |
| Consumer | `{EventName}Consumer` in `Application/Consumers/` | `InventoryReservedEventConsumer` |
| DTOs | All in one `{Entity}Dtos.cs` as records | `OrderDtos.cs` |
| Repository interface | `I{Entity}Repository` in `Domain/Repositories/` | `IOrderRepository` |
| Repository impl | `{Entity}Repository` in `Infrastructure/Persistence/` | `OrderRepository` |
| DbContext | `{Service}DbContext` in `Infrastructure/Persistence/` | `OrdersDbContext` |
| DI extension | `Add{Service}Infrastructure()` in `Infrastructure/Extensions/` | `AddOrdersInfrastructure()` |

## Domain Patterns

- **Rich domain model**: Private setters, factory methods (`Order.Create()`), encapsulated state transitions.
- **State machines enforced in domain**: Each transition method validates allowed source states, throws `InvalidOperationException` on invalid transitions.
- **No MediatR / CQRS**: Plain application service classes (`OrderService`) orchestrate domain calls + event publishing.
- **Result pattern**: Use `Result<T>` from SharedKernel for business failures instead of exceptions.
- **Concurrency**: `AggregateRoot<TId>.Version` is the EF Core concurrency token.
- **Idempotency**: Unique idempotency keys on Orders and Payments, plus consumer-level status guards.

## Data Access

- Each service owns its own PostgreSQL database (e.g., `ecommerce_orders`).
- `DbContext` implements `IUnitOfWork` directly — no separate wrapper.
- Schema isolation: `modelBuilder.HasDefaultSchema("{service}")`.
- Fluent API config inline in `OnModelCreating` (no separate `IEntityTypeConfiguration` classes).
- Enums stored as strings: `.HasConversion<string>()`.
- `Version` as concurrency token: `.IsConcurrencyToken()`.
- Unique index on idempotency keys.
- `OutboxMessage` entity mapped in every participating DbContext.

## Messaging & Event Flow

Choreography-based saga — no central orchestrator:

```
OrderPlaced → Inventory reserves stock → InventoryReserved
→ Payments authorizes → PaymentAuthorized
→ Orders confirms → OrderConfirmed → [Inventory commits, Payments captures]
```

Failure path: `InventoryReservationFailed` / `PaymentFailed` → `OrderCancelled` → [Inventory releases, Notification sent].

- All integration events extend `IntegrationEvent` (abstract record) with `EventId`, `OccurredOn`, `CorrelationId`.
- Events use `init`-only properties for MassTransit deserialization.
- Consumers live in `Application/Consumers/`, not Infrastructure.
- MassTransit retry: exponential, 3 retries, 1–15s interval.
- When type-name collisions occur between Contract DTOs and Application DTOs, use `using` aliases:
  ```csharp
  using ContractOrderItemDto = Contracts.Events.Order.OrderItemDto;
  ```

## Outbox Pattern

`OutboxWorker<TContext>` polls every 10s, batch size 20, max 5 retries. Registered via `services.AddOutboxWorker<TContext>()`. Ensures at-least-once delivery for integration events.

## API Patterns

- **Minimal APIs only** — `app.MapGroup("/api/{resource}")` with `MapGet`, `MapPost`, etc.
- All services expose `/health` endpoint.
- ProblemDetails for error responses.
- OpenAPI/Swagger enabled per service.
- YARP gateway on port 5100 routes `/api/{service}/**` to downstream ports.

## Service Ports

| Service | Port |
|---------|------|
| Gateway | 5100 |
| Catalog | 5101 (API: 5219) |
| Cart | 5102 (API: 5174) |
| Orders | 5103 (API: 5189) |
| Inventory | 5104 (API: 5110) |
| Payments | 5105 (API: 5257) |
| Notifications | 5106 (API: 5156) |

## Key Pitfalls

- **No EF migrations** — schema changes require manual `EnsureCreated()` or DB recreation.
- **Mixed package versions** — `net10.0` TFM but EF Core / Auth NuGet packages are 9.x series.
- **Domain events are collected but never dispatched** — `Entity<TId>` has `AddDomainEvent` but no dispatcher exists yet. Integration events are published directly from application services.
- **No test projects** — no unit or integration tests in the solution.
- **No central package management** — package versions are maintained per-project.
