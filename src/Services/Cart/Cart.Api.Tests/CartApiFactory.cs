using Cart.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cart.Api.Tests;

public class CartApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<CartDbContext>));
            services.RemoveAll(typeof(CartDbContext));

            services.AddDbContext<CartDbContext>(options =>
            {
                options.UseInMemoryDatabase("CartTestDb_" + Guid.NewGuid());
            });
        });
    }
}
