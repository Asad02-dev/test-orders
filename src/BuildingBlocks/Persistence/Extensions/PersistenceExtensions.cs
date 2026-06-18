using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Outbox;

namespace Persistence.Extensions;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPostgresDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionStringName = "DefaultConnection")
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");

        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            }));

        return services;
    }

    public static IServiceCollection AddInMemoryDbContext<TContext>(
        this IServiceCollection services,
        string databaseName)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        return services;
    }

    public static IServiceCollection AddOutboxWorker<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddHostedService<OutboxWorker<TContext>>();
        return services;
    }
}
