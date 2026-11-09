using Kairos.Application.Abstractions;
using Kairos.Application.Trades;
using Kairos.Domain.Market;
using Kairos.Domain.Trades;

namespace Kairos.Application.Tests;

internal sealed class InMemoryTrades : ITradeRepository
{
    private readonly List<Trade> trades = [];

    public int SaveCount { get; private set; }

    public int ListCount { get; private set; }

    public Task<Trade?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(trades.FirstOrDefault(trade => trade.Id == id));

    public Task<IReadOnlyList<Trade>> ListAsync(CancellationToken cancellationToken)
    {
        ListCount++;
        return Task.FromResult<IReadOnlyList<Trade>>(trades);
    }

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

internal sealed class FixedReferenceData(ReferenceCatalog catalog) : IReferenceDataProvider
{
    public Task<ReferenceCatalog> GetCatalogAsync(CancellationToken cancellationToken) => Task.FromResult(catalog);
}

internal sealed class SettableMarketState(MarketSnapshot current) : IMarketState
{
    public MarketSnapshot Current { get; set; } = current;
}

/// <summary>Market history made of the given closes, in date order.</summary>
internal sealed class ListedMarketHistory(params MarketSnapshot[] closes) : IMarketHistory
{
    public Task<MarketSnapshot?> GetCloseAsync(DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(closes.FirstOrDefault(close => close.Date == date));

    public Task<IReadOnlyList<MarketSnapshot>> GetClosesBeforeAsync(DateOnly date, int count, CancellationToken cancellationToken)
    {
        IReadOnlyList<MarketSnapshot> result = closes.Where(close => close.Date < date).TakeLast(count).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<DailyBar>> GetDailyBarsBeforeAsync(Guid instrumentId, DateOnly date, int count, CancellationToken cancellationToken)
    {
        IReadOnlyList<DailyBar> result = closes
            .Where(close => close.Date < date && close.EquityPrices.ContainsKey(instrumentId))
            .TakeLast(count)
            .Select(close =>
            {
                var price = close.EquityPrices[instrumentId];
                return new DailyBar(close.Date, price, price, price, price);
            })
            .ToList();
        return Task.FromResult(result);
    }
}

internal sealed class FixedActor : ICurrentActor
{
    public string Name => "demo.user";
}

internal sealed class FixedTime(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
