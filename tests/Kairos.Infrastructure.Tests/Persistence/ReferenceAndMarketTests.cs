using Kairos.Domain.Instruments;
using Kairos.Domain.Rates;
using Kairos.Domain.Reference;
using Kairos.Domain.ValueObjects;
using Kairos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kairos.Infrastructure.Tests.Persistence;

[Collection(SqlServerCollection.Name)]
public class ReferenceAndMarketTests(SqlServerFixture sql) : IAsyncLifetime
{
    private static readonly DateOnly Monday = new(2016, 6, 20);
    private static readonly DateOnly Tuesday = new(2016, 6, 21);

    private static readonly Instrument Equity = Instrument.Equity(Guid.NewGuid(), "AAA", "Energy");
    private static readonly Instrument EurUsd = Instrument.EurUsd(Guid.NewGuid());
    private static readonly Instrument Bond = Instrument.TreasuryBond(
        Guid.NewGuid(),
        "UST 10Y 2014",
        TreasuryBond.Issue(Tenor.TenYears, new DateOnly(2014, 1, 15), 0.0275m));

    private readonly CancellationToken none = CancellationToken.None;

    public async Task InitializeAsync()
    {
        await using var db = sql.CreateContext();
        await db.EquityDailyBars.ExecuteDeleteAsync();
        await db.EurUsdDailyBars.ExecuteDeleteAsync();
        await db.CurvePoints.ExecuteDeleteAsync();
        await db.Instruments.ExecuteDeleteAsync();
        await db.Books.ExecuteDeleteAsync();

        db.Instruments.AddRange(Equity, EurUsd, Bond);
        db.Books.Add(new Book(Guid.NewGuid(), "A Book"));

        foreach (var (date, close, bid) in new[] { (Monday, 100m, 1.1000m), (Tuesday, 110m, 1.2000m) })
        {
            db.EquityDailyBars.Add(new EquityDailyBarRow { InstrumentId = Equity.Id, Date = date, Open = 99m, High = 120m, Low = 90m, Close = close });
            db.EurUsdDailyBars.Add(new EurUsdDailyBarRow { Date = date, Open = bid, High = bid, Low = bid, Close = bid, CloseBid = bid, CloseAsk = bid + 0.0002m });
            db.CurvePoints.Add(new CurvePointRow { Date = date, Months = 120, Yield = 0.02m });
            db.CurvePoints.Add(new CurvePointRow { Date = date, Months = 24, Yield = 0.01m });
        }

        await db.SaveChangesAsync(none);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private IServiceProvider Services()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => sql.CreateContext());
        return services.BuildServiceProvider();
    }

    private MarketHistoryRepository History(KairosDbContext db) =>
        new(db, new ReferenceDataProvider(Services().GetRequiredService<IServiceScopeFactory>()));

    [Fact]
    public async Task A_bond_comes_back_with_its_terms_and_an_equity_without_any()
    {
        await using var db = sql.CreateContext();

        var bond = await db.Instruments.AsNoTracking().SingleAsync(item => item.Id == Bond.Id);
        var equity = await db.Instruments.AsNoTracking().SingleAsync(item => item.Id == Equity.Id);

        Assert.Equal(Tenor.TenYears, bond.Bond!.OriginalTenor);
        Assert.Equal(0.0275m, bond.Bond.CouponRate);
        Assert.Equal(new DateOnly(2024, 1, 15), bond.Bond.MaturityDate);
        Assert.Equal(AssetClass.Bond, bond.AssetClass);
        Assert.Null(equity.Bond);
        Assert.Equal("Energy", equity.Sector);
    }

    [Fact]
    public async Task The_reference_catalog_loads_every_kind_of_item()
    {
        var provider = new ReferenceDataProvider(Services().GetRequiredService<IServiceScopeFactory>());

        var catalog = await provider.GetCatalogAsync(none);

        Assert.Equal(3, catalog.Instruments.Count);
        Assert.Equal("EUR/USD", catalog.Instrument(EurUsd.Id).Symbol);
        Assert.Single(catalog.Books);
    }

    [Fact]
    public async Task A_close_is_assembled_from_the_three_market_tables()
    {
        await using var db = sql.CreateContext();

        var close = await History(db).GetCloseAsync(Monday, none);

        Assert.NotNull(close);
        Assert.Equal(100m, close.EquityPrices[Equity.Id]);
        Assert.Equal(1.1000m, close.EurUsd.Bid);
        Assert.Equal(1.1002m, close.EurUsd.Ask);
        Assert.Equal(0.015m, close.Curve.YieldAt(6m));
    }

    [Fact]
    public async Task A_date_that_is_not_a_market_day_has_no_close()
    {
        await using var db = sql.CreateContext();

        Assert.Null(await History(db).GetCloseAsync(Monday.AddDays(-1), none));
    }

    [Fact]
    public async Task Previous_closes_come_oldest_first_and_stop_before_the_date()
    {
        await using var db = sql.CreateContext();

        var closes = await History(db).GetClosesBeforeAsync(new DateOnly(2016, 6, 22), 5, none);
        var limited = await History(db).GetClosesBeforeAsync(Tuesday, 5, none);

        Assert.Equal([Monday, Tuesday], closes.Select(close => close.Date));
        Assert.Equal([Monday], limited.Select(close => close.Date));
    }

    [Fact]
    public async Task Daily_bars_are_read_according_to_the_asset_class()
    {
        await using var db = sql.CreateContext();
        var history = History(db);
        var nextDay = new DateOnly(2016, 6, 22);

        var equityBars = await history.GetDailyBarsBeforeAsync(Equity.Id, nextDay, 10, none);
        var fxBars = await history.GetDailyBarsBeforeAsync(EurUsd.Id, nextDay, 1, none);
        var bondBars = await history.GetDailyBarsBeforeAsync(Bond.Id, nextDay, 10, none);

        Assert.Equal([100m, 110m], equityBars.Select(bar => bar.Close));
        Assert.Equal(1.2000m, Assert.Single(fxBars).Close);
        Assert.Empty(bondBars);
    }
}
