---
applyTo: "**/Services/**"
description: "Use when creating or editing microservice code under src/Services/. Covers 4-layer Clean Architecture, entity factory methods, status enums, DbContext inline config, DI wiring, consumers, and DTOs."
---

# Service Development Rules

## 4-Layer Project Structure

Every service (except Notifications) has exactly four projects:

```
{Service}.Api            → Program.cs with minimal API endpoints (top-level statements)
{Service}.Application    → DTOs/, Services/, Consumers/
{Service}.Domain         → Entities/, Enums/, Events/, Repositories/
{Service}.Infrastructure → Persistence/ (DbContext, Repository), Extensions/
```

**Dependency rule — never invert:**

```
Domain → SharedKernel only
Application → Domain + Contracts + Messaging
Infrastructure → Application + Persistence + Messaging
Api → Infrastructure + Application + Authentication + Observability
```

## Domain Layer

### Entity & Aggregate Pattern

- Extend `AggregateRoot<Guid>` for root entities, `Entity<Guid>` for children.
- **Private setters** on all properties.
- **Private parameterless constructor** for EF Core: `private Order() { }`
- **Static factory method** `Create(...)` with input validation — never use public constructors.
- Call `AddDomainEvent(...)` inside factory methods.
- Child entities use `internal static Create(...)`.

```csharp
public class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();
    public OrderStatus Status { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Order Create(Guid customerId, /* ... */)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(customerEmail);
        var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Pending, /* ... */ };
        order.AddDomainEvent(new OrderCreatedDomainEvent(order.Id, /* ... */));
        return order;
    }
}
```

### State Machine Transitions

- Each transition is a named method on the aggregate: `ConfirmReservation()`, `AuthorizePayment()`, `Confirm()`, `Cancel(string reason)`.
- **Guard the source state** — throw `InvalidOperationException` if the current status is invalid for the transition.
- Always update `UpdatedAt = DateTime.UtcNow` and increment `Version++`.

```csharp
public void ConfirmReservation()
{
    if (Status != OrderStatus.Pending)
        throw new InvalidOperationException($"Cannot confirm reservation for order in {Status} status.");
    Status = OrderStatus.ReservationConfirmed;
    UpdatedAt = DateTime.UtcNow;
    Version++;
}
```

### Enums

- Named `{Entity}Status` in `Enums/` folder.
- Explicit integer values starting from 0.

```csharp
public enum OrderStatus
{
    Pending = 0,
    ReservationConfirmed = 1,
    // ...
    Cancelled = 6,
    Failed = 7
}
```

### Domain Events

- Record types named `{Entity}{Action}DomainEvent` in `Events/` folder.
- Extend `DomainEvent` from SharedKernel.

### Repository Interfaces

- `I{Entity}Repository` in `Domain/Repositories/`, extending `IRepository<TEntity, TId>` from SharedKernel.
- Add service-specific query methods (e.g., `GetByIdempotencyKeyAsync`, `GetPagedAsync`).

## Application Layer

### DTOs

- All DTOs for one entity in a single file: `{Entity}Dtos.cs` as `record` types.
- Includes response DTOs, request DTOs, and paged result types.

### Application Services

- Plain class (no MediatR/CQRS): `{Entity}Service` registered as `Scoped`.
- Constructor injects repository, `IUnitOfWork`, `IPublishEndpoint`.
- Orchestrates: repo call → domain method → `SaveChangesAsync()` → `Publish()` integration event.
- Use `Result<T>` from SharedKernel for business failures — never throw for expected errors.

### Consumers

- Live in `Application/Consumers/`, NOT Infrastructure.
- Named `{EventName}Consumer`, implement `IConsumer<TEvent>`.
- Thin — delegate to the application service, log the event.
- Include status guards for idempotency (check current entity state before acting).

```csharp
public class InventoryReservedConsumer : IConsumer<InventoryReservedEvent>
{
    private readonly OrderService _orderService;
    private readonly ILogger<InventoryReservedConsumer> _logger;

    public async Task Consume(ConsumeContext<InventoryReservedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Inventory reserved for order {OrderId}.", evt.OrderId);
        await _orderService.HandleInventoryReservedAsync(evt.OrderId, context.CancellationToken);
    }
}
```

### Type-Name Collision Resolution

When Contract DTOs and Application DTOs have the same name, use `using` aliases:

```csharp
using ContractOrderItemDto = Contracts.Events.Order.OrderItemDto;
using AppOrderItemDto = Orders.Application.DTOs.OrderItemDto;
```

## Infrastructure Layer

### DbContext

- Named `{Service}DbContext`, implements `IUnitOfWork` directly.
- Schema isolation: `modelBuilder.HasDefaultSchema("{service}")`.
- **Inline Fluent API** in `OnModelCreating` — no separate `IEntityTypeConfiguration` classes.
- Enums as strings: `.HasConversion<string>()`.
- Concurrency token: `.Property(e => e.Version).IsConcurrencyToken()`.
- Idempotency key: `.HasIndex(e => e.IdempotencyKey).IsUnique()`.
- Always map `OutboxMessage` entity.
- Expose `DbSet<OutboxMessage> OutboxMessages`.

### Repository

- Concrete `{Entity}Repository` in `Infrastructure/Persistence/`.
- Uses the service's DbContext directly (no generic repository wrapper).

### DI Wiring Extension

- Single static class `InfrastructureExtensions` in `Infrastructure/Extensions/`.
- Single method `Add{Service}Infrastructure(IServiceCollection, IConfiguration)` that registers:
  1. `AddPostgresDbContext<TContext>(configuration)`
  2. Repository as scoped
  3. `IUnitOfWork` forwarded to DbContext: `sp.GetRequiredService<TContext>()`
  4. Application service as scoped
  5. `AddRabbitMqMessagingWithConsumers(configuration, consumerAssembly)`
  6. `AddOutboxWorker<TContext>()`
- Also include `EnsureDatabaseCreatedAsync()` extension on `IServiceProvider`.

## Api Layer

### Program.cs Structure

Follow this exact order:

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Observability first
builder.AddObservability("{service}-api");

// 2. OpenAPI
builder.Services.AddOpenApi();

// 3. Authentication
builder.Services.AddKeycloakAuthentication(builder.Configuration);

// 4. Service infrastructure (DbContext, repos, MassTransit, outbox)
builder.Services.Add{Service}Infrastructure(builder.Configuration);

// 5. Health checks with DB check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<{Service}DbContext>("{service}-db");

// 6. ProblemDetails
builder.Services.AddProblemDetails();

var app = builder.Build();

// 7. Ensure DB exists
await app.Services.EnsureDatabaseCreatedAsync();

// 8. Middleware pipeline (order matters)
app.UseRequestLogging();
app.UseCorrelationId();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseAuthentication();
app.UseAuthorization();

// 9. Health endpoint
app.MapHealthChecks("/health");

// 10. API endpoints via MapGroup
var group = app.MapGroup("/api/{resource}").WithTags("{Resource}").RequireAuthorization();
// MapGet, MapPost, etc.

app.Run();
```

### Endpoint Conventions

- `MapGroup("/api/{resource}")` with `.RequireAuthorization()`.
- Each endpoint has `.WithName()` and `.WithSummary()`.
- Use `[FromBody]`, `[FromQuery]`, route parameters.
- Return `Results.Ok()`, `Results.Created()`, `Results.NotFound()`, `Results.NoContent()`, `Results.Problem()`.
