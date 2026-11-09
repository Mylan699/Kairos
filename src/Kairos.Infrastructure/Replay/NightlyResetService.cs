using Kairos.Application.Replay;
using Kairos.Infrastructure.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kairos.Infrastructure.Replay;

/// <summary>Restores the initial trades every night, so that the public demo stays clean.</summary>
internal sealed class NightlyResetService(
    IServiceScopeFactory scopeFactory,
    IMarketBroadcaster broadcaster,
    IOptions<ReplayOptions> options,
    TimeProvider timeProvider,
    ILogger<NightlyResetService> logger) : BackgroundService
{
    public static DateTimeOffset NextRun(DateTimeOffset now, int hourUtc)
    {
        var today = new DateTimeOffset(now.UtcDateTime.Date.AddHours(hourUtc), TimeSpan.Zero);
        return today > now ? today : today.AddDays(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = timeProvider.GetUtcNow();

            try
            {
                await Task.Delay(NextRun(now, options.Value.NightlyResetHourUtc) - now, timeProvider, stoppingToken);

                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<TradeSeeder>().ResetAsync(stoppingToken);
                await broadcaster.PublishDataResetAsync(stoppingToken);

                logger.LogInformation("The demo data was reset");
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The nightly reset failed");
            }
        }
    }
}
