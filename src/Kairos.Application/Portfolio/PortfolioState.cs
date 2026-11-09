using Kairos.Domain.Risk;

namespace Kairos.Application.Portfolio;

public readonly record struct PositionKey(Guid BookId, Guid InstrumentId);

/// <summary>The positions of one market date, and the results they closed at the day before.</summary>
/// <param name="PreviousCloseTotals">Total P&amp;L in euros at the previous close: the baseline of the day P&amp;L.</param>
public sealed record PortfolioState(
    DateOnly Date,
    IReadOnlyList<BookPosition> Positions,
    IReadOnlyDictionary<PositionKey, decimal> PreviousCloseTotals);

/// <summary>
/// Keeps the current positions in memory. They only change when a trade changes or
/// the market date moves, while their valuation changes on every tick of the replay.
/// </summary>
public sealed class PortfolioCache
{
    private volatile PortfolioState? state;

    public PortfolioState? Get(DateOnly date)
    {
        var current = state;
        return current?.Date == date ? current : null;
    }

    public void Set(PortfolioState newState) => state = newState;

    public void Invalidate() => state = null;
}
