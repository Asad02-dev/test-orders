using Messaging.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payments.Application.Consumers;
using Payments.Application.Services;
using Payments.Domain.Repositories;
using Payments.Infrastructure.Persistence;
using Persistence.Extensions;
using SharedKernel.Interfaces;

namespace Payments.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddPaymentsInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresDbContext<PaymentsDbContext>(configuration);
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PaymentsDbContext>());
        services.AddScoped<PaymentService>();
        services.AddRabbitMqMessagingWithConsumers(configuration, typeof(InventoryReservedConsumer).Assembly);
        services.AddOutboxWorker<PaymentsDbContext>();
        return services;
    }

    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}
