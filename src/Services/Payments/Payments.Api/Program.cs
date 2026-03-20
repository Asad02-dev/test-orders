using Authentication.Extensions;
using Observability.Extensions;
using Payments.Application.Services;
using Payments.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("payments-api");
builder.Services.AddOpenApi();
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddPaymentsInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Payments.Infrastructure.Persistence.PaymentsDbContext>("payments-db");
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");

var paymentsGroup = app.MapGroup("/api/payments").WithTags("Payments").RequireAuthorization();

paymentsGroup.MapGet("/orders/{orderId:guid}", async (Guid orderId, PaymentService svc, CancellationToken ct) =>
{
    var payment = await svc.GetByOrderIdAsync(orderId, ct);
    return payment is null ? Results.NotFound() : Results.Ok(payment);
}).WithName("GetPaymentByOrderId");

app.Run();
