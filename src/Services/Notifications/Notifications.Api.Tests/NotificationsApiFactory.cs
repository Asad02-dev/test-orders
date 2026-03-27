using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Api.Tests;

public class NotificationsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<NotificationsDbContext>));
            services.RemoveAll(typeof(NotificationsDbContext));

            services.AddDbContext<NotificationsDbContext>(options =>
            {
                options.UseInMemoryDatabase("NotificationsTestDb_" + Guid.NewGuid());
            });
        });
    }
}
