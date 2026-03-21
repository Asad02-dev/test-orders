using Inventory.Application.Consumers;
using Inventory.Application.Services;
using Inventory.Domain.Repositories;
using Inventory.Infrastructure.Persistence;
using Messaging.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Extensions;
using SharedKernel.Interfaces;

namespace Inventory.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresDbContext<InventoryDbContext>(configuration);
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<InventoryDbContext>());
        services.AddScoped<InventoryService>();
        services.AddRabbitMqMessagingWithConsumers(configuration, typeof(OrderPlacedConsumer).Assembly);
        return services;
    }
}
