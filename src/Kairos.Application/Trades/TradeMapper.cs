using Kairos.Application.Abstractions;
using Kairos.Domain.Trades;

namespace Kairos.Application.Trades;

internal static class TradeMapper
{
    public static TradeDto ToDto(Trade trade, ReferenceCatalog catalog)
    {
        var terms = trade.Terms;
        var instrument = catalog.Instrument(terms.InstrumentId);

        return new TradeDto(
            trade.Id,
            trade.CurrentVersion.Number,
            trade.Status,
            instrument.Id,
            instrument.Symbol,
            instrument.AssetClass,
            terms.Side,
            terms.Quantity,
            terms.Price,
            terms.CounterpartyId,
            catalog.Counterparty(terms.CounterpartyId).Name,
            terms.BookId,
            catalog.Book(terms.BookId).Name,
            terms.TraderId,
            catalog.Trader(terms.TraderId).Name,
            terms.TradeDate,
            terms.ValueDate,
            trade.AuditTrail[^1].At);
    }

    public static TradeDetailDto ToDetailDto(Trade trade, ReferenceCatalog catalog) => new(
        ToDto(trade, catalog),
        trade.Versions.Select(version => ToDto(version, catalog)).ToList(),
        trade.AuditTrail
            .Select(entry => new AuditEntryDto(entry.At, entry.Actor, entry.Action, entry.ResultingStatus, entry.Version, entry.Details))
            .ToList());

    private static TradeVersionDto ToDto(TradeVersion version, ReferenceCatalog catalog) => new(
        version.Number,
        version.Terms.Side,
        version.Terms.Quantity,
        version.Terms.Price,
        catalog.Counterparty(version.Terms.CounterpartyId).Name,
        catalog.Book(version.Terms.BookId).Name,
        catalog.Trader(version.Terms.TraderId).Name,
        version.Terms.TradeDate,
        version.Terms.ValueDate,
        version.CreatedAt,
        version.CreatedBy);
}
