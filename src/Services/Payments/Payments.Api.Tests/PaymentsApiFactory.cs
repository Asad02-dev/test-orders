using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Payments.Infrastructure.Persistence;

namespace Payments.Api.Tests;

public class PaymentsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<PaymentsDbContext>));
            services.RemoveAll(typeof(PaymentsDbContext));

            services.AddDbContext<PaymentsDbContext>(options =>
            {
                options.UseInMemoryDatabase("PaymentsTestDb_" + Guid.NewGuid());
            });
        });
    }
}
