using Authentication.Extensions;
using Observability.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Observability
builder.AddObservability("gateway");

// Authentication
builder.Services.AddKeycloakAuthentication(builder.Configuration);

// YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Health checks
builder.Services.AddHealthChecks();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();
app.MapHealthChecks("/health");

app.Run();
