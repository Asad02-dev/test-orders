using Cart.Application.Services;
using Cart.Domain.Repositories;
using Cart.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Extensions;
using SharedKernel.Interfaces;

namespace Cart.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddCartInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPostgresDbContext<CartDbContext>(configuration);
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CartDbContext>());
        services.AddScoped<CartService>();

        var ordersBaseAddress = configuration["Services:OrdersApi"]
            ?? "http://localhost:5103";
        services.AddHttpClient<CartCheckoutService>(client =>
        {
            client.BaseAddress = new Uri(ordersBaseAddress);
        });

        return services;
    }

    public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CartDbContext>();
        await context.Database.EnsureCreatedAsync();
    }
}
