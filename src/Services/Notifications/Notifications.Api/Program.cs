using Authentication.Extensions;
using Microsoft.AspNetCore.Mvc;
using Notifications.Application.Services;
using Notifications.Infrastructure.Extensions;
using Observability.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("notifications-api");
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new();
        document.Components.SecuritySchemes = new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>
        {
            ["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your Keycloak JWT token"
            }
        };
        document.SecurityRequirements =
        [
            new Microsoft.OpenApi.OpenApiSecurityRequirement
            {
                [
                    new Microsoft.OpenApi.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.OpenApiReference
                        {
                            Id = "Bearer",
                            Type = Microsoft.OpenApi.ReferenceType.SecurityScheme
                        }
                    }
                ] = []
            }
        ];
        return Task.CompletedTask;
    });
});
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddNotificationsInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Notifications.Infrastructure.Persistence.NotificationsDbContext>("notifications-db");
builder.Services.AddProblemDetails();

var app = builder.Build();

// Ensure schema exists on startup
await app.Services.EnsureDatabaseCreatedAsync();

app.UseRequestLogging();
app.UseCorrelationId();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Notifications API"));
}

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
