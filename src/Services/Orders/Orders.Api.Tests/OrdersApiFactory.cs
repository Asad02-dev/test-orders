using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Infrastructure.Persistence;

namespace Orders.Api.Tests;

public class OrdersApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            services.RemoveAll(typeof(DbContextOptions<OrdersDbContext>));
            services.RemoveAll(typeof(OrdersDbContext));

            // Add in-memory database
            services.AddDbContext<OrdersDbContext>(options =>
            {
                options.UseInMemoryDatabase("OrdersTestDb_" + Guid.NewGuid());
            });
        });
    }
}
