using Kairos.Domain.Instruments;
using Kairos.Infrastructure.Persistence;
using Kairos.Infrastructure.Replay;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kairos.Infrastructure.Tests.Replay;

[Collection(SqlServerCollection.Name)]
public class ReplayDayLoaderTests(SqlServerFixture sql) : IAsyncLifetime
{
    private const int SummerOpen = 13 * 60 + 30;

    private static readonly DateOnly Monday = new(2016, 6, 20);
    private static readonly DateOnly Tuesday = new(2016, 6, 21);
    private static readonly Instrument Equity = Instrument.Equity(Guid.NewGuid(), "AAA", "Energy");

    private readonly CancellationToken none = CancellationToken.None;

    public async Task InitializeAsync()
    {
        await using var db = sql.CreateContext();
        await db.EquityDailyBars.ExecuteDeleteAsync();
        await db.EurUsdDailyBars.ExecuteDeleteAsync();
        await db.EurUsdMinuteBars.ExecuteDeleteAsync();
        await db.CurvePoints.ExecuteDeleteAsync();
        await db.Instruments.ExecuteDeleteAsync();

        db.Instruments.Add(Equity);

        foreach (var (date, close, bid) in new[] { (Monday, 100m, 1.1000m), (Tuesday, 110m, 1.2000m) })
        {
            db.EquityDailyBars.Add(new EquityDailyBarRow { InstrumentId = Equity.Id, Date = date, Open = 101m, High = 115m, Low = 99m, Close = close });
            db.EurUsdDailyBars.Add(new EurUsdDailyBarRow { Date = date, Open = bid, High = bid, Low = bid, Close = bid, CloseBid = bid, CloseAsk = bid });
            db.CurvePoints.Add(new CurvePointRow { Date = date, Months = 120, Yield = date == Monday ? 0.02m : 0.04m });
        }

        db.EurUsdMinuteBars.Add(new EurUsdMinuteBarRow { Date = Tuesday, Minute = 600, Bid = 1.1500m, Ask = 1.1502m });
        await db.SaveChangesAsync(none);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private ReplayDayLoader Loader(KairosDbContext db)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => sql.CreateContext());
        var referenceData = new ReferenceDataProvider(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>());

        return new ReplayDayLoader(db, new MarketHistoryRepository(db, referenceData), referenceData);
    }

    [Fact]
    public async Task A_loaded_day_combines_the_previous_close_paths_and_real_ticks()
    {
        await using var db = sql.CreateContext();

        var day = await Loader(db).LoadAsync(Tuesday, none);

        Assert.NotNull(day);
        Assert.Equal(Monday, day.PreviousClose.Date);
        Assert.Equal(100m, day.SnapshotAt(0).EquityPrices[Equity.Id]);
        Assert.Equal(101m, day.SnapshotAt(SummerOpen).EquityPrices[Equity.Id]);
        Assert.Equal(110m, day.SnapshotAt(SummerOpen + 390).EquityPrices[Equity.Id]);
        Assert.Equal(1.1500m, day.SnapshotAt(600).EurUsd.Bid);
        Assert.Equal(0.03m, day.SnapshotAt(720).Curve.YieldAt(10m), 8);
    }

    [Fact]
    public async Task The_first_market_day_cannot_be_replayed()
    {
        await using var db = sql.CreateContext();

        Assert.Null(await Loader(db).LoadAsync(Monday, none));
    }

    [Fact]
    public async Task A_date_that_is_not_a_market_day_cannot_be_replayed()
    {
        await using var db = sql.CreateContext();

        Assert.Null(await Loader(db).LoadAsync(Tuesday.AddDays(1), none));
    }
}
