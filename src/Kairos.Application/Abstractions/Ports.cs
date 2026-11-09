using Kairos.Application.Trades;
using Kairos.Domain.Market;
using Kairos.Domain.Trades;

namespace Kairos.Application.Abstractions;

public interface IReferenceDataProvider
{
    Task<ReferenceCatalog> GetCatalogAsync(CancellationToken cancellationToken);
}

public interface ITradeRepository
{
    Task<Trade?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Trade>> ListAsync(CancellationToken cancellationToken);

    void Add(Trade trade);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>The market as it stands at the current point of the replay.</summary>
public interface IMarketState
{
    MarketSnapshot Current { get; }
}

/// <summary>The open, high, low and close of one day.</summary>
public sealed record DailyBar(DateOnly Date, decimal Open, decimal High, decimal Low, decimal Close);

/// <summary>Historical end-of-day market data.</summary>
public interface IMarketHistory
{
    /// <summary>Returns the closing market of a date, or null when that date is not a market day.</summary>
    Task<MarketSnapshot?> GetCloseAsync(DateOnly date, CancellationToken cancellationToken);

    /// <summary>Returns the closes of the market days just before a date, oldest first.</summary>
    Task<IReadOnlyList<MarketSnapshot>> GetClosesBeforeAsync(DateOnly date, int count, CancellationToken cancellationToken);

    /// <summary>Returns the daily bars of an instrument just before a date, oldest first. Empty for a bond.</summary>
    Task<IReadOnlyList<DailyBar>> GetDailyBarsBeforeAsync(Guid instrumentId, DateOnly date, int count, CancellationToken cancellationToken);
}

/// <summary>Tells the rest of the application, and connected screens, that a trade changed.</summary>
public interface ITradeNotifier
{
    Task TradeChangedAsync(TradeDto trade, CancellationToken cancellationToken);
}

/// <summary>Who is acting, for the audit trail.</summary>
public interface ICurrentActor
{
    string Name { get; }
}
