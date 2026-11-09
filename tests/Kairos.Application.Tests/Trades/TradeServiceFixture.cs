using Kairos.Application.Abstractions;
using Kairos.Application.Portfolio;
using Kairos.Application.Trades;
using Kairos.Domain.Fx;
using Kairos.Domain.Instruments;
using Kairos.Domain.Market;
using Kairos.Domain.Rates;
using Kairos.Domain.Reference;
using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Tests.Trades;

/// <summary>Builds a TradeService on in-memory stand-ins for everything outside the application layer.</summary>
internal sealed class TradeServiceFixture
{
    public const decimal CurrentRate = 1.10m;
    public const decimal PastRate = 1.25m;

    public static readonly DateOnly MarketDate = new(2016, 6, 24);
    public static readonly DateOnly PastMarketDay = new(2016, 6, 20);
    public static readonly DateTimeOffset Now = new(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

    public static readonly Instrument Equity = Instrument.Equity(Guid.NewGuid(), "AAA", "Energy");
    public static readonly Instrument MaturedBond = Instrument.TreasuryBond(
        Guid.NewGuid(),
        "UST 2Y",
        TreasuryBond.Issue(Tenor.TwoYears, new DateOnly(2012, 1, 16), 0.02m));
    public static readonly Book TheBook = new(Guid.NewGuid(), "Equities");
    public static readonly Trader TheTrader = new(Guid.NewGuid(), "C. Martin");
    public static readonly Counterparty TheCounterparty = new(Guid.NewGuid(), "Cpty Orion");

    public InMemoryTrades Trades { get; } = new();

    public NotifierSpy Notifier { get; } = new();

    public PortfolioCache Cache { get; } = new();

    public TradeService Service { get; }

    public TradeServiceFixture()
    {
        var catalog = new ReferenceCatalog([Equity, MaturedBond], [TheBook], [TheTrader], [TheCounterparty]);

        Service = new TradeService(
            Trades,
            new FixedReferenceData(catalog),
            new SettableMarketState(Snapshot(MarketDate, CurrentRate)),
            new ListedMarketHistory(Snapshot(PastMarketDay, PastRate)),
            Notifier,
            Cache,
            new FixedActor(),
            new FixedTime(Now),
            new TradeRequestValidator(),
            new CancelTradeRequestValidator());
    }

    public static TradeRequest Request(
        decimal quantity = 100m,
        decimal price = 50m,
        DateOnly? tradeDate = null,
        DateOnly? valueDate = null,
        Guid? instrumentId = null) => new(
            instrumentId ?? Equity.Id,
            Side.Buy,
            quantity,
            price,
            TheCounterparty.Id,
            TheBook.Id,
            TheTrader.Id,
            tradeDate,
            valueDate);

    private static MarketSnapshot Snapshot(DateOnly date, decimal rate) => new(
        date,
        new Dictionary<Guid, decimal> { [Equity.Id] = 50m },
        new EurUsdQuote(rate, rate),
        new YieldCurve([new CurvePoint(Tenor.TenYears, 0.02m)]));
}
