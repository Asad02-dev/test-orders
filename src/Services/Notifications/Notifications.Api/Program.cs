using Authentication.Extensions;
using Microsoft.AspNetCore.Mvc;
using Notifications.Application.Services;
using Notifications.Infrastructure.Extensions;
using Observability.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("notifications-api");
builder.Services.AddOpenApi();
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddNotificationsInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Notifications.Infrastructure.Persistence.NotificationsDbContext>("notifications-db");
builder.Services.AddProblemDetails();

var app = builder.Build();

// Ensure schema exists on startup
await app.Services.EnsureDatabaseCreatedAsync();

app.UseRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

var notificationsGroup = app.MapGroup("/api/notifications").WithTags("Notifications");

notificationsGroup.MapGet("/status", () => Results.Ok(new { Status = "Online", Service = "Notifications" }))
    .WithName("GetNotificationsStatus");

notificationsGroup.MapGet("/", async (
    [FromQuery] int count,
    NotificationService svc,
    CancellationToken ct) =>
{
    if (count <= 0 || count > 200) count = 50;
    var logs = await svc.GetRecentAsync(count, ct);
    return Results.Ok(logs);
})
.WithName("GetRecentNotifications")
.WithSummary("Get recent notification logs")
.RequireAuthorization();

notificationsGroup.MapGet("/orders/{orderId:guid}", async (
    Guid orderId,
    NotificationService svc,
    CancellationToken ct) =>
{
    var logs = await svc.GetByOrderIdAsync(orderId, ct);
    return Results.Ok(logs);
})
.WithName("GetNotificationsByOrder")
.WithSummary("Get notification logs for a specific order")
.RequireAuthorization();

app.Run();
