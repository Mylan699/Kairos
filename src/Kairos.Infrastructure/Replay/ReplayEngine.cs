using Kairos.Application.Abstractions;
using Kairos.Application.Portfolio;
using Kairos.Application.Replay;
using Kairos.Application.Risk;
using Kairos.Domain.Market;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kairos.Infrastructure.Replay;

/// <summary>
/// Plays a market day in accelerated time: on each tick it moves the clock, rebuilds the
/// market, values the book and pushes the result to connected screens.
/// </summary>
internal sealed class ReplayEngine(
    IServiceScopeFactory scopeFactory,
    LiveMarketState liveMarket,
    IMarketBroadcaster broadcaster,
    IOptions<ReplayOptions> options,
    TimeProvider timeProvider,
    ILogger<ReplayEngine> logger) : BackgroundService, IReplayControl
{
    private const int TicksBetweenPositions = 2;
    private const int TicksBetweenRisk = 4;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly ReplayOptions settings = options.Value;
    private readonly Lock gate = new();

    private ReplayDay? day;
    private ReplayClock? clock;
    private DateTimeOffset lastCommandAt;
    private long tickCount;

    public ReplayStatusDto? Status
    {
        get
        {
            lock (gate)
            {
                return BuildStatus();
            }
        }
    }

    public async Task<bool> LoadDayAsync(DateOnly date, int? startMinute, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<ReplayDayLoader>().LoadAsync(date, cancellationToken);

        if (loaded is null)
        {
            return false;
        }

        var start = startMinute ?? settings.DefaultStartMinute;

        lock (gate)
        {
            day = loaded;
            clock = new ReplayClock(start, clock?.Speed ?? settings.DefaultSpeed);
            lastCommandAt = timeProvider.GetUtcNow();
        }

        liveMarket.Publish(loaded.SnapshotAt(start));
        return true;
    }

    public void Play() => Command(current => current.Play());

    public void Pause() => Command(current => current.Pause());

    public void SetSpeed(int speed) => Command(current => current.Speed = speed);

    public void Seek(int minute) => Command(current => current.Seek(minute));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await LoadDefaultDayAsync(stoppingToken);

        var interval = TimeSpan.FromMilliseconds(settings.TickMilliseconds);
        using var timer = new PeriodicTimer(interval, timeProvider);

        while (await WaitForTickAsync(timer, stoppingToken))
        {
            try
            {
                await ReturnToDefaultWhenIdleAsync(stoppingToken);
                await TickAsync(interval, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // One bad tick must not stop the replay for everyone.
                logger.LogError(exception, "A replay tick failed");
            }
        }
    }

    private static async Task<bool> WaitForTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private void Command(Action<ReplayClock> action)
    {
        lock (gate)
        {
            if (clock is not null)
            {
                action(clock);
                lastCommandAt = timeProvider.GetUtcNow();
            }
        }
    }

    private ReplayStatusDto? BuildStatus() =>
        day is null || clock is null
            ? null
            : new ReplayStatusDto(day.Date, clock.Minute, clock.Speed, clock.IsPlaying, day.Session.OpenMinute, day.Session.CloseMinute);

    private async Task LoadDefaultDayAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await LoadDayAsync(settings.DefaultDate, settings.DefaultStartMinute, stoppingToken))
                {
                    return;
                }

                logger.LogWarning("Market data is not available yet for {Date}", settings.DefaultDate);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "The default replay day could not be loaded");
            }

            await Task.Delay(RetryDelay, timeProvider, stoppingToken);
        }
    }

    /// <summary>The replay is shared: a visitor who walks away must not leave it paused for the next one.</summary>
    private async Task ReturnToDefaultWhenIdleAsync(CancellationToken stoppingToken)
    {
        if (settings.IdleResetMinutes <= 0)
        {
            return;
        }

        bool shouldReset;
        lock (gate)
        {
            var isIdle = timeProvider.GetUtcNow() - lastCommandAt > TimeSpan.FromMinutes(settings.IdleResetMinutes);
            var isOnDefault = day?.Date == settings.DefaultDate && clock is { IsPlaying: true };
            shouldReset = isIdle && !isOnDefault;

            if (isIdle && isOnDefault)
            {
                lastCommandAt = timeProvider.GetUtcNow();
            }
        }

        if (shouldReset && await LoadDayAsync(settings.DefaultDate, settings.DefaultStartMinute, stoppingToken))
        {
            Command(current =>
            {
                current.Speed = settings.DefaultSpeed;
                current.Play();
            });
        }
    }

    private async Task TickAsync(TimeSpan interval, CancellationToken stoppingToken)
    {
        ReplayDay currentDay;
        int minute;
        ReplayStatusDto status;

        lock (gate)
        {
            clock!.Advance(interval);
            currentDay = day!;
            minute = clock.Minute;
            status = BuildStatus()!;
        }

        var market = currentDay.SnapshotAt(minute);
        liveMarket.Publish(market);
        tickCount++;

        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var catalog = await services.GetRequiredService<IReferenceDataProvider>().GetCatalogAsync(stoppingToken);
        var state = await services.GetRequiredService<PortfolioService>().GetStateAsync(stoppingToken);

        var positions = PortfolioValuator.ValuePositions(state, market, catalog);
        var summary = PortfolioValuator.Summarize(market.Date, positions);

        await broadcaster.PublishTickAsync(MarketTickBuilder.Build(status, market, currentDay.PreviousClose, catalog, summary), stoppingToken);

        if (tickCount % TicksBetweenPositions == 0)
        {
            await broadcaster.PublishPositionsAsync(positions, stoppingToken);
        }

        if (tickCount % TicksBetweenRisk == 0)
        {
            await broadcaster.PublishRiskAsync(await services.GetRequiredService<RiskService>().GetReportAsync(stoppingToken), stoppingToken);
        }
    }
}
