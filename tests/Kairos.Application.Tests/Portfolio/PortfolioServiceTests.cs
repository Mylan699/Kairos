using FluentValidation;
using Kairos.Application.Abstractions;
using Kairos.Application.Portfolio;
using Kairos.Application.Trades;
using Kairos.Domain.Common;
using Kairos.Domain.Fx;
using Kairos.Domain.Instruments;
using Kairos.Domain.Market;
using Kairos.Domain.Rates;
using Kairos.Domain.Reference;
using Kairos.Domain.Trades;
using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Tests.Portfolio;

public class PortfolioServiceTests
{
    private const decimal YesterdayRate = 1.12m;
    private const decimal TodayRate = 1.10m;
    private const decimal HalfSpread = 0.0001m;

    private static readonly DateOnly Yesterday = new(2016, 6, 23);
    private static readonly DateOnly Today = new(2016, 6, 24);
    private static readonly DateTimeOffset BookedAt = new(2016, 6, 23, 15, 0, 0, TimeSpan.Zero);

    private static readonly Instrument Equity = Instrument.Equity(Guid.NewGuid(), "AAA", "Energy");
    private static readonly Instrument EurUsd = Instrument.EurUsd(Guid.NewGuid());
    private static readonly Instrument Bond = Instrument.TreasuryBond(
        Guid.NewGuid(),
        "UST 10Y",
        TreasuryBond.Issue(Tenor.TenYears, new DateOnly(2014, 1, 15), 0.02m));
    private static readonly Book BookA = new(Guid.NewGuid(), "A Book");
    private static readonly Book BookB = new(Guid.NewGuid(), "B Book");
    private static readonly Trader Trader = new(Guid.NewGuid(), "C. Martin");
    private static readonly Counterparty Counterparty = new(Guid.NewGuid(), "Cpty Orion");

    private readonly CancellationToken none = CancellationToken.None;
    private readonly InMemoryTrades trades = new();
    private readonly PortfolioCache cache = new();
    private readonly PortfolioService service;

    public PortfolioServiceTests()
    {
        var catalog = new ReferenceCatalog([Equity, EurUsd, Bond], [BookA, BookB], [Trader], [Counterparty]);

        service = new PortfolioService(
            trades,
            new FixedReferenceData(catalog),
            new SettableMarketState(Snapshot(Today, equityPrice: 50m, TodayRate - HalfSpread, TodayRate + HalfSpread)),
            new ListedMarketHistory(Snapshot(Yesterday, equityPrice: 48m, YesterdayRate, YesterdayRate)),
            cache,
            new TradeRequestValidator());
    }

    private static MarketSnapshot Snapshot(DateOnly date, decimal equityPrice, decimal bid, decimal ask) => new(
        date,
        new Dictionary<Guid, decimal> { [Equity.Id] = equityPrice },
        new EurUsdQuote(bid, ask),
        new YieldCurve(Tenor.CurvePoints.Select(tenor => new CurvePoint(tenor, 0.02m))));

    private Trade Add(
        Instrument instrument,
        Side side,
        decimal quantity,
        decimal price,
        DateOnly date,
        decimal rate,
        Book? book = null,
        DateTimeOffset? bookedAt = null)
    {
        var terms = new TradeTerms(
            instrument.Id,
            side,
            quantity,
            price,
            Counterparty.Id,
            (book ?? BookA).Id,
            Trader.Id,
            date,
            ValueDateCalculator.ValueDateFor(instrument.AssetClass, date),
            rate);

        var trade = Trade.Book(Guid.NewGuid(), terms, "demo.user", bookedAt ?? BookedAt);
        trades.Add(trade);
        return trade;
    }

    private TradeRequest Request(Instrument instrument, Side side, decimal quantity, decimal price) =>
        new(instrument.Id, side, quantity, price, Counterparty.Id, BookA.Id, Trader.Id);

    [Fact]
    public async Task A_position_shows_its_value_and_pnl_split_in_euros()
    {
        // Bought for 4000 EUR (4500 USD at 1.125). Now 5000 USD at 1.10, worth 4545.45 EUR.
        Add(Equity, Side.Buy, 100m, 45m, Yesterday, 1.125m);

        var position = Assert.Single(await service.GetPositionsAsync(none));

        Assert.Equal(100m, position.Quantity);
        Assert.Equal(45m, position.AveragePrice);
        Assert.Equal(50m, position.MarkPrice);
        Assert.Equal(4_545.45m, position.MarketValue);
        Assert.Equal(545.45m, position.Pnl.Unrealized);
        Assert.Equal(454.55m, position.Pnl.PriceEffect);
        Assert.Equal(90.91m, position.Pnl.FxEffect);
    }

    [Fact]
    public async Task The_day_pnl_is_measured_from_the_previous_close()
    {
        // Yesterday's close: 4800 USD at 1.12 = 4285.71 EUR, against a 4000 EUR cost.
        Add(Equity, Side.Buy, 100m, 45m, Yesterday, 1.125m);

        var position = Assert.Single(await service.GetPositionsAsync(none));

        Assert.Equal(259.74m, position.Pnl.Day);
    }

    [Fact]
    public async Task A_position_opened_today_earns_its_whole_result_today()
    {
        Add(Equity, Side.Buy, 100m, 49m, Today, TodayRate);

        var position = Assert.Single(await service.GetPositionsAsync(none));

        Assert.Equal(position.Pnl.Total, position.Pnl.Day);
    }

    [Fact]
    public async Task A_sale_realizes_the_gain_in_the_position()
    {
        Add(Equity, Side.Buy, 100m, 44m, Yesterday, 1.10m);
        Add(Equity, Side.Sell, 100m, 55m, Today, 1.10m);

        var position = Assert.Single(await service.GetPositionsAsync(none));

        Assert.Equal(0m, position.Quantity);
        Assert.Equal(1_000m, position.Pnl.Realized);
        Assert.Equal(0m, position.Pnl.Unrealized);
    }

    [Fact]
    public async Task Trades_after_the_market_date_are_ignored()
    {
        Add(Equity, Side.Buy, 100m, 45m, Yesterday, 1.125m);
        Add(Equity, Side.Buy, 500m, 45m, Today.AddDays(3), 1.125m);

        var position = Assert.Single(await service.GetPositionsAsync(none));

        Assert.Equal(100m, position.Quantity);
    }

    [Fact]
    public async Task Cancelled_trades_are_ignored()
    {
        Add(Equity, Side.Buy, 100m, 45m, Yesterday, 1.125m);
        Add(Equity, Side.Buy, 500m, 45m, Yesterday, 1.125m).Cancel("Duplicate", "demo.user", BookedAt);

        var position = Assert.Single(await service.GetPositionsAsync(none));

        Assert.Equal(100m, position.Quantity);
    }

    [Fact]
    public async Task An_amended_trade_counts_at_its_latest_terms()
    {
        var trade = Add(Equity, Side.Buy, 100m, 45m, Yesterday, 1.125m);
        trade.Amend(
            new TradeTerms(Equity.Id, Side.Buy, 300m, 45m, Counterparty.Id, BookA.Id, Trader.Id, Yesterday, trade.Terms.ValueDate, 1.125m),
            "demo.user",
            BookedAt);

        var position = Assert.Single(await service.GetPositionsAsync(none));

        Assert.Equal(300m, position.Quantity);
    }

    [Fact]
    public async Task Trades_of_the_same_date_apply_in_booking_order()
    {
        // Booked in reverse: the short must be seen first, so the buy closes it and realizes the gain.
        Add(Equity, Side.Buy, 100m, 40m, Today, 1.10m, bookedAt: BookedAt.AddMinutes(5));
        Add(Equity, Side.Sell, 100m, 50m, Today, 1.10m, bookedAt: BookedAt);

        var position = Assert.Single(await service.GetPositionsAsync(none));

        Assert.Equal(909.09m, position.Pnl.Realized);
    }

    [Fact]
    public async Task An_eur_usd_long_is_marked_at_the_bid_and_a_short_at_the_ask()
    {
        Add(EurUsd, Side.Buy, 1_000m, 1.0m, Today, TodayRate, BookA);
        Add(EurUsd, Side.Sell, 1_000m, 1.0m, Today, TodayRate, BookB);

        var positions = await service.GetPositionsAsync(none);

        Assert.Equal(TodayRate - HalfSpread, positions.Single(position => position.BookId == BookA.Id).MarkPrice);
        Assert.Equal(TodayRate + HalfSpread, positions.Single(position => position.BookId == BookB.Id).MarkPrice);
    }

    [Fact]
    public async Task The_summary_adds_positions_into_books_and_books_into_a_total()
    {
        Add(Equity, Side.Buy, 100m, 45m, Yesterday, 1.125m, BookA);
        Add(Equity, Side.Buy, 50m, 45m, Yesterday, 1.125m, BookB);

        var summary = await service.GetSummaryAsync(none);

        Assert.Equal(Today, summary.MarketDate);
        Assert.Equal(["A Book", "B Book"], summary.Books.Select(book => book.Book));
        Assert.Equal(summary.Books.Sum(book => book.Pnl.Total), summary.Total.Total);
    }

    [Fact]
    public async Task Positions_are_rebuilt_only_when_something_changed()
    {
        Add(Equity, Side.Buy, 100m, 45m, Yesterday, 1.125m);

        await service.GetPositionsAsync(none);
        await service.GetPositionsAsync(none);
        Assert.Equal(1, trades.ListCount);

        cache.Invalidate();
        await service.GetPositionsAsync(none);
        Assert.Equal(2, trades.ListCount);
    }

    [Fact]
    public async Task The_impact_of_a_trade_above_the_mark_is_an_immediate_loss()
    {
        var impact = await service.EstimateImpactAsync(Request(Equity, Side.Buy, 100m, 51m), none);

        Assert.Equal(-90.91m, impact.PnlImpactEur);
        Assert.Equal(4_636.36m, impact.NotionalEur);
        Assert.Equal(0m, impact.PositionBefore);
        Assert.Equal(100m, impact.PositionAfter);
        Assert.Equal(0m, impact.Dv01ChangeEur);
    }

    [Fact]
    public async Task The_impact_includes_the_position_already_held()
    {
        Add(Equity, Side.Buy, 100m, 45m, Yesterday, 1.125m);

        var impact = await service.EstimateImpactAsync(Request(Equity, Side.Sell, 150m, 50m), none);

        Assert.Equal(100m, impact.PositionBefore);
        Assert.Equal(-50m, impact.PositionAfter);
    }

    [Fact]
    public async Task Buying_a_bond_adds_to_the_desk_dv01_and_a_sale_reduces_it()
    {
        var buy = await service.EstimateImpactAsync(Request(Bond, Side.Buy, 1_000_000m, 100m), none);
        var sell = await service.EstimateImpactAsync(Request(Bond, Side.Sell, 1_000_000m, 100m), none);

        Assert.True(buy.Dv01ChangeEur > 0m);
        Assert.Equal(-buy.Dv01ChangeEur, sell.Dv01ChangeEur);
        Assert.Equal(buy.Dv01ChangeEur, buy.TotalDv01AfterEur);
    }

    [Fact]
    public async Task Estimating_books_nothing()
    {
        await service.EstimateImpactAsync(Request(Equity, Side.Buy, 100m, 51m), none);

        Assert.Empty(await trades.ListAsync(none));
        Assert.Equal(0, trades.SaveCount);
    }

    [Fact]
    public async Task An_invalid_estimate_is_rejected()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.EstimateImpactAsync(Request(Equity, Side.Buy, 0m, 51m), none));
    }

    [Fact]
    public async Task A_redeemed_bond_cannot_be_estimated()
    {
        var redeemed = Instrument.TreasuryBond(
            Guid.NewGuid(),
            "UST 2Y",
            TreasuryBond.Issue(Tenor.TwoYears, new DateOnly(2012, 1, 16), 0.02m));
        var catalog = new ReferenceCatalog([redeemed], [BookA], [Trader], [Counterparty]);
        var other = new PortfolioService(
            trades,
            new FixedReferenceData(catalog),
            new SettableMarketState(Snapshot(Today, 50m, TodayRate, TodayRate)),
            new ListedMarketHistory(),
            new PortfolioCache(),
            new TradeRequestValidator());

        await Assert.ThrowsAsync<DomainException>(() =>
            other.EstimateImpactAsync(Request(redeemed, Side.Buy, 100m, 100m), none));
    }
}
