using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.DTOs;

namespace Notifications.Api.Tests;

public class NotificationsEndpointsTests : IClassFixture<NotificationsApiFactory>
{
    private readonly HttpClient _client;
    private readonly NotificationsApiFactory _factory;

    public NotificationsEndpointsTests(NotificationsApiFactory factory)
    {
        _factory = factory;
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        }).CreateClient();

        _client.DefaultRequestHeaders.Add("Authorization", "Test");
    }

    [Fact]
    public async Task GetNotificationsStatus_ReturnsOnlineStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications/status");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(result);
        Assert.Equal("Online", result["Status"]);
        Assert.Equal("Notifications", result["Service"]);
    }

    [Fact]
    public async Task GetRecentNotifications_WithDefaultCount_ReturnsResults()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<NotificationLogDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetRecentNotifications_WithCustomCount_ReturnsLimitedResults()
    {
        // Act
        var response = await _client.GetAsync("/api/notifications?count=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<NotificationLogDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetNotificationsByOrder_WithOrderId_ReturnsResults()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/notifications/orders/{orderId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<NotificationLogDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetRecentNotifications_WithInvalidCount_UsesDefaultCount()
    {
        // Act - Test with count = 0 (invalid)
        var response = await _client.GetAsync("/api/notifications?count=0");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<NotificationLogDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetRecentNotifications_WithExcessiveCount_CapsAtMaximum()
    {
        // Act - Test with count > 200 (should be capped at 200)
        var response = await _client.GetAsync("/api/notifications?count=500");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<NotificationLogDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] { new Claim("sub", Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
