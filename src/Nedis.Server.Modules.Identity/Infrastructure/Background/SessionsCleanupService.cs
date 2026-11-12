using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nedis.Server.Modules.Identity.Infrastructure.Data;

namespace Nedis.Server.Modules.Identity.Infrastructure.Background;

internal sealed partial class SessionsCleanupService(
    IServiceScopeFactory scopeFactory,
    TimeProvider time,
    ILogger<SessionsCleanupService> logger
) : BackgroundService
{
    private const int BatchSize = 2000;
    private static readonly TimeSpan DelayBetweenBatches = TimeSpan.FromMilliseconds(100);

    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, time, stoppingToken);

            using var timer = new PeriodicTimer(Interval, time);
            do
            {
                try
                {
                    await CleanupAsync(stoppingToken);
                }
                // Anything except shutdown is logged and retried on the next tick,
                // including an OperationCanceledException that did not come from stoppingToken.
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    LogCleanupError(logger, ex);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            LogCleanupCancelled(logger);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var now = time.GetUtcNow();

        int deletedTotal = 0;
        int deletedInBatch;

        do
        {
            deletedInBatch = await db.Sessions
                .Where(s => s.ExpiresAt <= now || s.RevokedAt != null)
                .OrderBy(s => s.ExpiresAt)
                .Take(BatchSize)
                .ExecuteDeleteAsync(ct);
            deletedTotal += deletedInBatch;

            if (deletedInBatch >= BatchSize)
            {
                await Task.Delay(DelayBetweenBatches, time, ct);
            }
        } while (deletedInBatch >= BatchSize);

        LogCleanupSuccess(logger, deletedTotal);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Session cleanup removed {Count} sessions")]
    private static partial void LogCleanupSuccess(ILogger logger, int count);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "An error occurred during session cleanup")]
    private static partial void LogCleanupError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Session cleanup task was cancelled")]
    private static partial void LogCleanupCancelled(ILogger logger);
}