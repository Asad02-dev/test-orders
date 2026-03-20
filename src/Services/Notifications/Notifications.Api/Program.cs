using Authentication.Extensions;
using Notifications.Infrastructure.Extensions;
using Observability.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("notifications-api");
builder.Services.AddOpenApi();
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddNotificationsInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

app.MapGet("/api/notifications/status", () => Results.Ok(new { Status = "Online", Service = "Notifications" }))
   .WithTags("Notifications").WithName("GetNotificationsStatus");

app.Run();
