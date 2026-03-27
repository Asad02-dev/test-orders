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

            // Add in-memory database
            services.AddDbContext<CatalogDbContext>(options =>
            {
                options.UseInMemoryDatabase("CatalogTestDb_" + Guid.NewGuid());
            });
        });
    }
}
