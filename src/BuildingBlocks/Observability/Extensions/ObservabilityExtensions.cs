using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Observability.Middleware;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using SharedKernel.Common;

namespace Observability.Extensions;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        builder.Host.UseSerilog((ctx, services, loggerConfig) =>
        {
            loggerConfig
                .ReadFrom.Configuration(ctx.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {ServiceName} {CorrelationId} {Message:lj}{NewLine}{Exception}");

            if (ctx.HostingEnvironment.IsProduction())
            {
                loggerConfig.MinimumLevel.Information();
            }
            else
            {
                loggerConfig.MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning);
            }
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        // Register CorrelationContext as scoped so it is available per request
        builder.Services.AddScoped<CorrelationContext>();

        return builder;
    }

    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.####}ms";
        });
        return app;
    }

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
