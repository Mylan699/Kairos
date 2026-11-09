using Kairos.Application.Abstractions;
using Kairos.Application.Trades;
using Kairos.Domain.Fx;
using Kairos.Domain.Instruments;
using Kairos.Domain.Market;
using Kairos.Domain.Rates;
using Kairos.Domain.Reference;
using Kairos.Domain.Trades;
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

    public TradeService Service { get; }

    public TradeServiceFixture()
    {
        var catalog = new ReferenceCatalog([Equity, MaturedBond], [TheBook], [TheTrader], [TheCounterparty]);

        Service = new TradeService(
            Trades,
            new FixedReferenceData(catalog),
            new FixedMarketState(),
            new FixedMarketHistory(),
            Notifier,
            new FixedActor(),
            new FixedTime(),
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

    internal sealed class InMemoryTrades : ITradeRepository
    {
        private readonly List<Trade> trades = [];

        public int SaveCount { get; private set; }

        public Task<Trade?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(trades.FirstOrDefault(trade => trade.Id == id));

        public Task<IReadOnlyList<Trade>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Trade>>(trades);

        public void Add(Trade trade) => trades.Add(trade);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    internal sealed class NotifierSpy : ITradeNotifier
    {
        public List<TradeDto> Notifications { get; } = [];

        public Task TradeChangedAsync(TradeDto trade, CancellationToken cancellationToken)
        {
            Notifications.Add(trade);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedReferenceData(ReferenceCatalog catalog) : IReferenceDataProvider
    {
        public Task<ReferenceCatalog> GetCatalogAsync(CancellationToken cancellationToken) => Task.FromResult(catalog);
    }

    private sealed class FixedMarketState : IMarketState
    {
        public MarketSnapshot Current { get; } = Snapshot(MarketDate, CurrentRate);
    }

    private sealed class FixedMarketHistory : IMarketHistory
    {
        public Task<MarketSnapshot?> GetCloseAsync(DateOnly date, CancellationToken cancellationToken) =>
            Task.FromResult(date == PastMarketDay ? Snapshot(date, PastRate) : null);
    }

    private sealed class FixedActor : ICurrentActor
    {
        public string Name => "demo.user";
    }

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
