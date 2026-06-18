using Messaging.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Consumers;
using Notifications.Application.Repositories;
using Notifications.Application.Services;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<NotificationService>();
        services.AddRabbitMqMessagingWithConsumers(configuration, typeof(OrderConfirmationNotificationConsumer).Assembly);
        return services;
    }

    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}
