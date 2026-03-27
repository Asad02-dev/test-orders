using Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Catalog.Api.Tests;

public class CatalogApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<CatalogDbContext>));
            services.RemoveAll(typeof(CatalogDbContext));

            // Build a dedicated EF Core internal service provider with only InMemory
            // to avoid conflicts with Npgsql registered by AddCatalogInfrastructure
            var inMemoryServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Capture DB name outside the lambda so all requests in this test host share the same database
            var dbName = "CatalogTestDb_" + Guid.NewGuid();
            services.AddDbContext<CatalogDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName)
                       .UseInternalServiceProvider(inMemoryServiceProvider);
            });
        });
    }
}
