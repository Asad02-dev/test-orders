---
applyTo: "**/BuildingBlocks/**"
description: "Use when creating or editing shared BuildingBlock libraries. Covers extension method patterns, inter-library dependencies, and conventions for adding new shared libraries."
---

# BuildingBlocks Development Rules

## Library Overview

| Library | Purpose | Depends On |
|---------|---------|------------|
| SharedKernel | `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `Result<T>`, `CorrelationContext`, `IUnitOfWork`, `IRepository` | — (no deps) |
| Contracts | Integration event records shared across services | — (no deps) |
| Messaging | MassTransit + RabbitMQ configuration | Contracts |
| Persistence | EF Core + Npgsql setup, `OutboxMessage`, `OutboxWorker<T>` | SharedKernel |
| Authentication | Keycloak JWT bearer | — (standalone) |
| Observability | Serilog + OpenTelemetry + correlation middleware | SharedKernel |

**Dependency constraints — never violate:**
- SharedKernel, Contracts, Authentication have **zero** BuildingBlock deps.
- Messaging depends only on Contracts.
- Persistence depends only on SharedKernel.
- Observability depends only on SharedKernel.
- BuildingBlocks **never** reference service projects.

## Extension Method Conventions

Each library exposes exactly one `Extensions/` folder containing one static class.

### Naming Pattern

| Registration | Middleware |
|-------------|-----------|
| `Add{Feature}()` on `IServiceCollection` | `Use{Feature}()` on `IApplicationBuilder` |
| `Add{Feature}()` on `WebApplicationBuilder` | — |

### Current Extensions

```csharp
// Messaging
services.AddRabbitMqMessaging(configuration);
services.AddRabbitMqMessagingWithConsumers(configuration, assemblies);

// Persistence
services.AddPostgresDbContext<TContext>(configuration);
services.AddInMemoryDbContext<TContext>(databaseName);
services.AddOutboxWorker<TContext>();

// Authentication
services.AddKeycloakAuthentication(configuration);

// Observability
builder.AddObservability("service-name");   // on WebApplicationBuilder
app.UseRequestLogging();                    // on IApplicationBuilder
app.UseCorrelationId();                     // on IApplicationBuilder
```

### Extension Method Pattern

All extension methods follow this structure:

```csharp
public static class {Feature}Extensions
{
    public static IServiceCollection Add{Feature}(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Read config section
        // Register services
        return services;  // always return for chaining
    }
}
```

- Read configuration from named sections (e.g., `configuration.GetSection("RabbitMQ")`).
- Return `this` type for fluent chaining.
- Use generic type parameters for DbContext: `where TContext : DbContext`.
- Provide sensible defaults for optional config values.

## Adding a New BuildingBlock Library

1. **Create project** at `src/BuildingBlocks/{Name}/{Name}.csproj` targeting `net10.0` as a class library.
2. **Add `Extensions/` folder** with a single `{Name}Extensions.cs` static class.
3. **Expose `Add{Name}()` extension method** on `IServiceCollection` or `WebApplicationBuilder`.
4. **If middleware is needed**, add `Middleware/` folder with `{Name}Middleware.cs` and expose `Use{Name}()`.
5. **Add project to `ECommerce.slnx`** solution file.
6. **Reference from service Infrastructure or Api projects** — never from Domain or Application.

## Contracts / Integration Events

### Event Structure

All events extend `IntegrationEvent` (abstract record with `EventId`, `OccurredOn`, `CorrelationId`).

```csharp
public record OrderPlacedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    // ... init-only properties with defaults for MassTransit deserialization
}
```

### Organization

- Events live in `Contracts/Events/{Subdomain}/` — one folder per bounded context.
- Shared DTO types (e.g., `OrderItemDto`) live alongside their events in the same folder.
- Naming: `{Entity}{PastTense}Event` for success, `{Entity}{Action}FailedEvent` for failure, `Send{Action}NotificationCommand` for commands.

### Adding a New Event

1. Create record in `Contracts/Events/{Subdomain}/`.
2. Extend `IntegrationEvent`.
3. Use `init`-only properties (required for MassTransit deserialization).
4. Include `CorrelationId` passthrough from upstream events.

## SharedKernel Domain Primitives

| Type | Purpose | Key Members |
|------|---------|-------------|
| `Entity<TId>` | Base entity | `Id`, `AddDomainEvent()`, `ClearDomainEvents()` |
| `AggregateRoot<TId>` | Aggregate root (extends Entity) | `Version` (concurrency token) |
| `ValueObject` | Immutable value type | `GetEqualityComponents()` |
| `DomainEvent` | Abstract domain event record | `EventId`, `OccurredOn` |
| `Result<T>` | Railway error handling | `IsSuccess`, `IsFailure`, `Value`, `Error` |
| `IUnitOfWork` | Unit of work contract | `SaveChangesAsync()` |
| `IRepository<T, TId>` | Generic repository contract | `GetByIdAsync()`, `AddAsync()`, `Update()`, `Delete()` |

Do not add service-specific types to SharedKernel — it must remain domain-agnostic.

## Outbox Pattern

`OutboxWorker<TContext>` is a `BackgroundService` that:
- Polls `OutboxMessages` every 10 seconds, batch of 20.
- Retries up to 5 times with error tracking.
- Uses `Type.GetType()` to deserialize and `IPublishEndpoint.Publish()` to republish.
- Each service DbContext must map the `OutboxMessage` entity.
