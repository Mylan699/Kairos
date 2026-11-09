using Kairos.Application.Abstractions;
using Kairos.Application.Common;
using Kairos.Application.Portfolio;
using Kairos.Application.Replay;
using Kairos.Application.Trades;
using Kairos.Domain.Fx;
using Kairos.Domain.Instruments;
using Kairos.Domain.Market;
using Kairos.Domain.Rates;
using Kairos.Domain.Reference;
using Kairos.Domain.Trades;
using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Tests.Replay;

public class IntradayServiceTests
{
    private const int SummerOpen = 13 * 60 + 30;

    private static readonly DateOnly Yesterday = new(2016, 6, 23);
    private static readonly DateOnly Today = new(2016, 6, 24);
    private static readonly CancellationToken None = CancellationToken.None;

    private static readonly Instrument Equity = Instrument.Equity(Guid.NewGuid(), "AAA", "Energy");
    private static readonly Instrument EurUsd = Instrument.EurUsd(Guid.NewGuid());
    private static readonly Instrument Redeemed = Instrument.TreasuryBond(
        Guid.NewGuid(), "UST 2Y 2012", TreasuryBond.Issue(Tenor.TwoYears, new DateOnly(2012, 1, 16), 0.02m));
    private static readonly Book TheBook = new(Guid.NewGuid(), "Book");
    private static readonly Trader TheTrader = new(Guid.NewGuid(), "Trader");
    private static readonly Counterparty TheCounterparty = new(Guid.NewGuid(), "Cpty");

    private static YieldCurve FlatCurve(decimal yield) =>
        new(Tenor.CurvePoints.Select(tenor => new CurvePoint(tenor, yield)));

    private static MarketSnapshot PreviousClose() => new(
        Yesterday,
        new Dictionary<Guid, decimal> { [Equity.Id] = 100m },
        new EurUsdQuote(1.2500m, 1.2500m),
        FlatCurve(0.02m));

    private static ReplayDay Day()
    {
        var path = Enumerable.Repeat(110m, 391).ToArray();
        FxTick[] ticks = [new(0, new EurUsdQuote(1.2500m, 1.2502m))];

        return new ReplayDay(Today, PreviousClose(), new Dictionary<Guid, decimal[]> { [Equity.Id] = path }, ticks, FlatCurve(0.03m));
    }

    private static IntradayService Service(int minute, InMemoryTrades? trades = null)
    {
        var day = Day();
        var catalog = new FixedReferenceData(new ReferenceCatalog([Equity, EurUsd, Redeemed], [TheBook], [TheTrader], [TheCounterparty]));
        var portfolio = new PortfolioService(
            trades ?? new InMemoryTrades(),
            catalog,
            new SettableMarketState(day.SnapshotAt(minute)),
            new ListedMarketHistory(PreviousClose()),
            new PortfolioCache(),
            new TradeRequestValidator());

        return new IntradayService(new FixedSession(new ReplaySession(day, minute)), portfolio, catalog);
    }

    private static InMemoryTrades OneLongEquity()
    {
        var trades = new InMemoryTrades();
        var terms = new TradeTerms(
            Equity.Id, Side.Buy, 100m, 100m, TheCounterparty.Id, TheBook.Id, TheTrader.Id, Yesterday, new DateOnly(2016, 6, 27), 1.25m);
        trades.Add(Trade.Book(Guid.NewGuid(), terms, "demo", new DateTimeOffset(2016, 6, 23, 15, 0, 0, TimeSpan.Zero)));

        return trades;
    }

    [Fact]
    public async Task The_overview_stops_at_the_current_minute_and_includes_it()
    {
        var points = await Service(minute: 12).GetOverviewAsync(None);

        Assert.Equal([0, 5, 10, 12], points.Select(point => point.Minute));
    }

    [Fact]
    public async Task The_overview_measures_each_market_at_its_minute()
    {
        var point = (await Service(minute: 720).GetOverviewAsync(None))[^1];

        Assert.Equal(1.2501m, point.EurUsd);
        Assert.Equal(0m, point.EquityBasketPercent);
        Assert.Equal(2.5m, point.TenYearPercent);
    }

    [Fact]
    public async Task The_basket_shows_the_equity_move_once_the_session_is_open()
    {
        var point = (await Service(minute: SummerOpen + 10).GetOverviewAsync(None))[^1];

        Assert.Equal(10m, point.EquityBasketPercent);
    }

    [Fact]
    public async Task The_day_pnl_follows_the_book_through_the_day()
    {
        var points = await Service(SummerOpen + 10, OneLongEquity()).GetOverviewAsync(None);

        // Cost: 8000 EUR. EUR/USD has already moved from 1.2500 to 1.2501 at midnight.
        Assert.Equal(10_000m / 1.2501m - 8_000m, points[0].DayPnl, 2);
        Assert.Equal(11_000m / 1.2501m - 8_000m, points[^1].DayPnl, 2);
    }

    [Fact]
    public async Task Equity_prices_start_at_the_open_and_stop_at_the_current_minute()
    {
        var prices = await Service(SummerOpen + 2).GetPricesAsync(Equity.Id, None);

        Assert.Equal([SummerOpen, SummerOpen + 1, SummerOpen + 2], prices.Select(price => price.Minute));
    }

    [Fact]
    public async Task Equity_prices_do_not_exist_before_the_open()
    {
        Assert.Empty(await Service(SummerOpen - 1).GetPricesAsync(Equity.Id, None));
    }

    [Fact]
    public async Task Eur_usd_prices_use_the_mid_all_day()
    {
        var prices = await Service(3).GetPricesAsync(EurUsd.Id, None);

        Assert.Equal(4, prices.Count);
        Assert.Equal(1.2501m, prices[^1].Price);
    }

    [Fact]
    public async Task A_redeemed_bond_has_no_prices()
    {
        Assert.Empty(await Service(600).GetPricesAsync(Redeemed.Id, None));
    }

    [Fact]
    public async Task Prices_of_an_unknown_instrument_are_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Service(600).GetPricesAsync(Guid.NewGuid(), None));
    }

    [Fact]
    public async Task Before_the_replay_has_started_the_market_is_not_ready()
    {
        var service = new IntradayService(new FixedSession(null), null!, null!);

        await Assert.ThrowsAsync<MarketNotReadyException>(() => service.GetOverviewAsync(None));
    }

    private sealed class FixedSession(ReplaySession? session) : IReplayControl
    {
        public ReplaySession? CurrentSession => session;

        public ReplayStatusDto? Status => null;

        public Task<bool> LoadDayAsync(DateOnly date, int? startMinute, CancellationToken cancellationToken) => Task.FromResult(true);

        public void Play()
        {
        }

        public void Pause()
        {
        }

        public void SetSpeed(int speed)
        {
        }

        public void Seek(int minute)
        {
        }
    }
}
