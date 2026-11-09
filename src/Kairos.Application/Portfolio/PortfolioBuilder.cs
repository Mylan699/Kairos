using Kairos.Application.Abstractions;
using Kairos.Domain.Market;
using Kairos.Domain.Positions;
using Kairos.Domain.Risk;
using Kairos.Domain.Trades;

namespace Kairos.Application.Portfolio;

public static class PortfolioBuilder
{
    /// <summary>Replays the live trades done up to a date, in order, into one position per book and instrument.</summary>
    public static IReadOnlyList<BookPosition> BuildPositions(IEnumerable<Trade> trades, ReferenceCatalog catalog, DateOnly asOf) =>
        trades
            .Where(trade => trade.IsLive && trade.Terms.TradeDate <= asOf)
            .GroupBy(trade => new PositionKey(trade.Terms.BookId, trade.Terms.InstrumentId))
            .Select(group =>
            {
                var instrument = catalog.Instrument(group.Key.InstrumentId);
                var fills = group
                    .OrderBy(trade => trade.Terms.TradeDate)
                    .ThenBy(trade => trade.AuditTrail[0].At)
                    .Select(trade => trade.Terms.ToFill());

                return new BookPosition(group.Key.BookId, instrument, Position.FromFills(instrument.AssetClass, fills));
            })
            .ToList();

    /// <summary>Records what each position had earned at a close, to measure the next day against it.</summary>
    public static IReadOnlyDictionary<PositionKey, decimal> TotalsAt(IEnumerable<BookPosition> positions, MarketSnapshot close) =>
        positions.ToDictionary(
            held => new PositionKey(held.BookId, held.Instrument.Id),
            held => held.Position.Realized.Amount + held.Position
                .Value(close.MarkPrice(held.Instrument, held.Position.Quantity), close.UsdPerEur)
                .Unrealized.Amount);
}
