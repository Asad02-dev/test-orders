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

        return services;
    }
}
