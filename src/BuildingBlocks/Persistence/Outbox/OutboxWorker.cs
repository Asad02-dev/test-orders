using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Persistence.Outbox;

/// <summary>
/// Generic outbox background worker that polls the OutboxMessages table and publishes
/// any unprocessed messages via MassTransit. Guarantees at-least-once delivery even
/// if RabbitMQ was temporarily unavailable when the event was first raised.
/// </summary>
public class OutboxWorker<TContext> : BackgroundService
    where TContext : DbContext
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);
    private static readonly int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxWorker<TContext>> _logger;

    public OutboxWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxWorker<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxWorker<{Context}> started.", typeof(TContext).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxWorker<{Context}> error during batch processing.", typeof(TContext).Name);
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxWorker<{Context}> stopped.", typeof(TContext).Name);
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var messages = await db.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        _logger.LogDebug("OutboxWorker<{Context}> processing {Count} message(s).", typeof(TContext).Name, messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var type = Type.GetType(message.EventType);
                if (type is null)
                {
                    _logger.LogWarning("OutboxWorker: unknown type {EventType}. Skipping.", message.EventType);
                    message.ProcessedAt = DateTime.UtcNow;
                    message.Error = $"Unknown type: {message.EventType}";
                    continue;
                }

                var payload = JsonSerializer.Deserialize(message.Payload, type);
                if (payload is not null)
                    await publisher.Publish(payload, type, ct);

                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                _logger.LogWarning(ex,
                    "OutboxWorker<{Context}> failed to publish message {MessageId} (attempt {Attempt}).",
                    typeof(TContext).Name, message.Id, message.RetryCount);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
