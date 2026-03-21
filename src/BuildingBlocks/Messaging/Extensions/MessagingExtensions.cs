using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Messaging.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        var rabbitMqSection = configuration.GetSection("RabbitMQ");
        var host = rabbitMqSection["Host"] ?? "localhost";
        var username = rabbitMqSection["Username"] ?? "guest";
        var password = rabbitMqSection["Password"] ?? "guest";
        var virtualHost = rabbitMqSection["VirtualHost"] ?? "/";

        services.AddMassTransit(x =>
        {
            configure?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(host, virtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.UseMessageRetry(r => r.Exponential(
                    retryLimit: 3,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(15),
                    intervalDelta: TimeSpan.FromSeconds(2)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    public static IServiceCollection AddRabbitMqMessagingWithConsumers(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] consumerAssemblies)
    {
        return services.AddRabbitMqMessaging(configuration, x =>
        {
            foreach (var assembly in consumerAssemblies)
            {
                x.AddConsumers(assembly);
            }
        });
    }
}
