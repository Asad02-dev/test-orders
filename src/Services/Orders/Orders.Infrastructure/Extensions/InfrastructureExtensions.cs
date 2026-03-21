using Messaging.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Consumers;
using Orders.Application.Services;
using Orders.Domain.Repositories;
using Orders.Infrastructure.Persistence;
using Persistence.Extensions;
using SharedKernel.Interfaces;

namespace Orders.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddOrdersInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPostgresDbContext<OrdersDbContext>(configuration);
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OrdersDbContext>());
        services.AddScoped<OrderService>();
        services.AddRabbitMqMessagingWithConsumers(configuration, typeof(InventoryReservedConsumer).Assembly);

        return services;
    }

    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}
