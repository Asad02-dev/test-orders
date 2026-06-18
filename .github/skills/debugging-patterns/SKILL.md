---
name: ecommerce-bug-patterns
description: "Common bug categories, symptoms, investigation paths, and root causes for the ECommerce microservices platform. Load when diagnosing any bug."
---

# ECommerce Bug Pattern Catalog

## 1. Domain State Machine Violations
- **Symptom**: `InvalidOperationException` on status transitions.
- **Look at**: Entity transition methods, current `Status` value, the consumer/service calling the transition.
- **Common cause**: Events arriving out of order, missing status guard in consumer, wrong transition method called.

## 2. EF Core / Database Issues
- **Symptom**: `DbUpdateConcurrencyException`, missing columns, mapping errors.
- **Look at**: `OnModelCreating` in DbContext, entity property types, `Version` concurrency token, `HasConversion<string>()` for enums.
- **Common cause**: Schema drift (no migrations — must recreate DB), missing property mapping, forgetting `Version++` in domain transitions.

## 3. MassTransit / Consumer Failures
- **Symptom**: Messages in error queue, consumer exceptions, events never arriving.
- **Look at**: Consumer class, event record definition in Contracts, `AddRabbitMqMessagingWithConsumers()` assembly registration, `InfrastructureExtensions`.
- **Common cause**: Event type mismatch, missing consumer registration, deserialization failure (init-only property issue), wrong assembly passed to MassTransit scanner.

## 4. Saga Flow Breaks
- **Symptom**: Order stuck in intermediate state, downstream service never processes.
- **Look at**: The full chain — which event was published, which consumer should receive it, is the consumer registered, does the consumer publish the next event?
- **Trace path**: Orders → Inventory → Payments → Orders → [Inventory, Payments, Notifications].

## 5. Outbox Pattern Issues
- **Symptom**: Events published but never delivered, or delivered multiple times.
- **Look at**: `OutboxMessage` mapping in DbContext, `OutboxWorker` registration, `SaveChangesAsync()` call order (must save outbox message in same transaction as domain state).
- **Common cause**: OutboxMessage not mapped in DbContext, worker not registered, message serialization failure.

## 6. DI / Wiring Errors
- **Symptom**: `InvalidOperationException` at startup — service not registered.
- **Look at**: `InfrastructureExtensions.Add{Service}Infrastructure()`, `Program.cs` service registration order.
- **Common cause**: Missing `services.AddScoped<>()`, IUnitOfWork not forwarded to DbContext, application service not registered.

## 7. API Endpoint Issues
- **Symptom**: 404, 401, 500 from API calls.
- **Look at**: `Program.cs` — MapGroup path, middleware order, `.RequireAuthorization()`, route parameters.
- **Common cause**: Wrong route prefix, middleware order (auth before routing), missing `[FromBody]`/`[FromQuery]`, ProblemDetails not wired.

## 8. Gateway / YARP Routing
- **Symptom**: Gateway returns 502 or routes to wrong service.
- **Look at**: `src/Gateway/Gateway.Api/appsettings.json` — YARP routes and clusters, downstream port numbers.
- **Common cause**: Wrong cluster address, route pattern mismatch, service not running on expected port.

## 9. Authentication / Authorization
- **Symptom**: 401 Unauthorized or 403 Forbidden.
- **Look at**: `AddKeycloakAuthentication()` config, `appsettings.json` Keycloak section, `.RequireAuthorization()` on endpoints, token claims.
- **Common cause**: Wrong audience/issuer in config, Keycloak not running, token expired, missing role claim.

## 10. Concurrency & Idempotency
- **Symptom**: `DbUpdateConcurrencyException`, duplicate processing.
- **Look at**: `Version` property increment in domain transitions, idempotency key unique index, consumer status guards.
- **Common cause**: Missing `Version++`, consumer processing same event twice without status guard.

## Investigation Trace Paths

| Symptom | Trace Path |
|---------|------------|
| API error | `Program.cs` → Application Service → Domain Entity → Repository → DbContext |
| Message not consumed | Consumer → Application Service → event type registration |
| Saga stuck | Trace event chain across services (see saga flow) |
| Startup crash | `Program.cs` → `InfrastructureExtensions` → DI registrations |
| Config error | `appsettings.json` → extension method reading config section |
