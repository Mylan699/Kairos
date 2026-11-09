using Kairos.Application.Abstractions;
using Kairos.Application.Market;
using Kairos.Application.Portfolio;
using Kairos.Application.Risk;
using Kairos.Application.Trades;
using Kairos.Domain.Fx;
using Kairos.Domain.Instruments;
using Kairos.Domain.Market;
using Kairos.Domain.Rates;
using Kairos.Domain.Reference;
using Kairos.Domain.Trades;
using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Tests;

/// <summary>A small desk with three closes, shared by the tests of the read-side services.</summary>
internal sealed class ReadSideFixture
{
    public const decimal Rate = 1.25m;

    public static readonly DateOnly TwoDaysAgo = new(2016, 6, 22);
    public static readonly DateOnly Yesterday = new(2016, 6, 23);
    public static readonly DateOnly Today = new(2016, 6, 24);

    public static readonly Instrument Winner = Instrument.Equity(Guid.NewGuid(), "WIN", "Energy");
    public static readonly Instrument Loser = Instrument.Equity(Guid.NewGuid(), "LOS", "Technology");
    public static readonly Instrument Bond = Instrument.TreasuryBond(
        Guid.NewGuid(),
        "UST 10Y",
        TreasuryBond.Issue(Tenor.TenYears, new DateOnly(2014, 1, 15), 0.02m));
    public static readonly Book BookA = new(Guid.NewGuid(), "A Book");
    public static readonly Book BookB = new(Guid.NewGuid(), "B Book");
    public static readonly Trader TheTrader = new(Guid.NewGuid(), "C. Martin");
    public static readonly Counterparty TheCounterparty = new(Guid.NewGuid(), "Cpty Orion");

    private readonly InMemoryTrades trades = new();

    public ReadSideFixture()
    {
        var catalog = new FixedReferenceData(new ReferenceCatalog([Winner, Loser, Bond], [BookA, BookB], [TheTrader], [TheCounterparty]));
        var marketState = new SettableMarketState(Snapshot(Today, winner: 110m, loser: 90m, rate: Rate, yield: 0.02m));
        var history = new ListedMarketHistory(
            Snapshot(TwoDaysAgo, winner: 90m, loser: 110m, rate: Rate, yield: 0.02m),
            Snapshot(Yesterday, winner: 100m, loser: 100m, rate: Rate, yield: 0.03m));

        Portfolio = new PortfolioService(trades, catalog, marketState, history, new PortfolioCache(), new TradeRequestValidator());
        Risk = new RiskService(Portfolio, catalog, marketState, history, new ParametricStressRequestValidator(), new HistoricalStressRequestValidator());
        Dashboard = new DashboardService(Portfolio, marketState);
        Market = new MarketService(catalog, marketState, history);
    }

    public PortfolioService Portfolio { get; }

    public RiskService Risk { get; }

    public DashboardService Dashboard { get; }

    public MarketService Market { get; }

    public void Buy(Instrument instrument, decimal quantity, decimal price, Book? book = null, DateOnly? date = null)
    {
        var tradeDate = date ?? TwoDaysAgo;
        var terms = new TradeTerms(
            instrument.Id,
            Side.Buy,
            quantity,
            price,
            TheCounterparty.Id,
            (book ?? BookA).Id,
            TheTrader.Id,
            tradeDate,
            ValueDateCalculator.ValueDateFor(instrument.AssetClass, tradeDate),
            Rate);

        trades.Add(Trade.Book(Guid.NewGuid(), terms, "demo.user", new DateTimeOffset(2016, 6, 22, 15, 0, 0, TimeSpan.Zero)));
    }

    private static MarketSnapshot Snapshot(DateOnly date, decimal winner, decimal loser, decimal rate, decimal yield) => new(
        date,
        new Dictionary<Guid, decimal> { [Winner.Id] = winner, [Loser.Id] = loser },
        new EurUsdQuote(rate, rate),
        new YieldCurve(Tenor.CurvePoints.Select(tenor => new CurvePoint(tenor, yield))));
}
