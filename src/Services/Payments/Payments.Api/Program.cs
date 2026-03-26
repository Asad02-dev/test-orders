using Authentication.Extensions;
using Observability.Extensions;
using Payments.Application.Services;
using Payments.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability("payments-api");
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
builder.Services.AddPaymentsInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Payments.Infrastructure.Persistence.PaymentsDbContext>("payments-db");
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
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Payments API"));
}

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
