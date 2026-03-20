using Messaging.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Consumers;
using Notifications.Application.Services;

namespace Notifications.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<NotificationService>();
        services.AddRabbitMqMessagingWithConsumers(configuration, typeof(OrderConfirmationNotificationConsumer).Assembly);
        return services;
    }
}
