using Kairos.Application;
using Kairos.Application.Abstractions;
using Kairos.Application.Portfolio;
using Kairos.Application.Replay;
using Kairos.Application.Risk;
using Kairos.Domain.Instruments;
using Kairos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kairos.Infrastructure.Tests.Replay;

[Collection(SqlServerCollection.Name)]
public sealed class ReplayEngineTests(SqlServerFixture sql) : IAsyncLifetime
{
    private static readonly DateOnly Monday = new(2016, 6, 20);
    private static readonly DateOnly Tuesday = new(2016, 6, 21);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);
    private static readonly Instrument Equity = Instrument.Equity(Guid.NewGuid(), "AAA", "Energy");

    private readonly BroadcasterSpy broadcaster = new();
    private ServiceProvider provider = null!;
    private IHostedService engine = null!;

    private IReplayControl Control => provider.GetRequiredService<IReplayControl>();

    public async Task InitializeAsync()
    {
        await using (var db = sql.CreateContext())
        {
            await db.Trades.ExecuteDeleteAsync();
            await db.EquityDailyBars.ExecuteDeleteAsync();
            await db.EurUsdDailyBars.ExecuteDeleteAsync();
            await db.EurUsdMinuteBars.ExecuteDeleteAsync();
            await db.CurvePoints.ExecuteDeleteAsync();
            await db.Instruments.ExecuteDeleteAsync();

            db.Instruments.Add(Equity);

            foreach (var date in new[] { Monday, Tuesday })
            {
                db.EquityDailyBars.Add(new EquityDailyBarRow { InstrumentId = Equity.Id, Date = date, Open = 100m, High = 110m, Low = 90m, Close = 105m });
                db.EurUsdDailyBars.Add(new EurUsdDailyBarRow { Date = date, Open = 1.1m, High = 1.1m, Low = 1.1m, Close = 1.1m, CloseBid = 1.1m, CloseAsk = 1.1002m });
                db.CurvePoints.Add(new CurvePointRow { Date = date, Months = 120, Yield = 0.02m });
            }

            await db.SaveChangesAsync();
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Kairos"] = sql.ConnectionString,
            ["Replay:TickMilliseconds"] = "20",
            ["Replay:DefaultDate"] = "2016-06-21",
            ["Replay:DefaultSpeed"] = "60",
            ["Replay:DefaultStartMinute"] = "800",
            ["Replay:IdleResetMinutes"] = "0",
        }).Build();

        var services = new ServiceCollection().AddLogging().AddSingleton<IConfiguration>(configuration);
        services.AddApplication().AddInfrastructure(configuration);
        services.AddSingleton<IMarketBroadcaster>(broadcaster);

        provider = services.BuildServiceProvider();
        engine = provider.GetServices<IHostedService>().Single(service => service is IReplayControl);
        await engine.StartAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await engine.StopAsync(CancellationToken.None);
        await provider.DisposeAsync();
    }

    [Fact]
    public async Task The_engine_starts_on_the_default_day_and_publishes_ticks()
    {
        var tick = await broadcaster.NextTickAsync().WaitAsync(Timeout);

        Assert.Equal(Tuesday, tick.Status.Date);
        Assert.True(tick.Status.Minute >= 800);
        Assert.Equal("AAA", Assert.Single(tick.Quotes).Symbol);
    }

    [Fact]
    public async Task A_paused_replay_stops_moving_and_can_be_moved_by_hand()
    {
        await broadcaster.NextTickAsync().WaitAsync(Timeout);

        Control.Pause();
        Control.Seek(900);
        await broadcaster.NextTickAsync().WaitAsync(Timeout);
        var tick = await broadcaster.NextTickAsync().WaitAsync(Timeout);

        Assert.False(tick.Status.IsPlaying);
        Assert.Equal(900, tick.Status.Minute);
    }

    [Fact]
    public async Task Speed_changes_show_in_the_status()
    {
        await broadcaster.NextTickAsync().WaitAsync(Timeout);

        Control.SetSpeed(600);

        Assert.Equal(600, Control.Status!.Speed);
    }

    [Fact]
    public async Task A_date_without_a_previous_close_is_refused()
    {
        await broadcaster.NextTickAsync().WaitAsync(Timeout);

        Assert.False(await Control.LoadDayAsync(Monday, null, CancellationToken.None));
        Assert.Equal(Tuesday, Control.Status!.Date);
    }

    [Fact]
    public async Task The_market_state_follows_the_replay()
    {
        await broadcaster.NextTickAsync().WaitAsync(Timeout);

        Assert.Equal(Tuesday, provider.GetRequiredService<IMarketState>().Current.Date);
    }

    private sealed class BroadcasterSpy : IMarketBroadcaster
    {
        private TaskCompletionSource<MarketTickDto> nextTick = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<MarketTickDto> NextTickAsync()
        {
            nextTick = new TaskCompletionSource<MarketTickDto>(TaskCreationOptions.RunContinuationsAsynchronously);
            return nextTick.Task;
        }

        public Task PublishTickAsync(MarketTickDto tick, CancellationToken cancellationToken)
        {
            nextTick.TrySetResult(tick);
            return Task.CompletedTask;
        }

        public Task PublishPositionsAsync(IReadOnlyList<PositionDto> positions, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task PublishRiskAsync(RiskReportDto report, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task PublishDataResetAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
