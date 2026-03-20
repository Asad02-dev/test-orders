using Catalog.Application.Services;
using Catalog.Domain.Repositories;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Extensions;
using SharedKernel.Interfaces;

namespace Catalog.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPostgresDbContext<CatalogDbContext>(configuration);
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());
        services.AddScoped<ProductService>();

        return services;
    }

    public static async Task MigrateDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await context.Database.MigrateAsync();
    }
}
