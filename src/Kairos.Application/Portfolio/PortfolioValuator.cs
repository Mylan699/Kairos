using Kairos.Application.Abstractions;
using Kairos.Domain.Market;
using Kairos.Domain.Risk;

namespace Kairos.Application.Portfolio;

/// <summary>Turns positions and a market into the figures shown on screen. Pure, and cheap enough for every tick.</summary>
public static class PortfolioValuator
{
    private const int EuroDecimals = 2;

    public static IReadOnlyList<PositionDto> ValuePositions(PortfolioState state, MarketSnapshot market, ReferenceCatalog catalog) =>
        state.Positions.Select(held => ValuePosition(held, state, market, catalog)).ToList();

    /// <summary>Totals are summed from rounded lines, so that the rows on screen add up to the total shown.</summary>
    public static PortfolioSummaryDto Summarize(DateOnly marketDate, IReadOnlyList<PositionDto> positions)
    {
        var books = positions
            .GroupBy(position => (position.BookId, position.Book))
            .Select(group => new BookSummaryDto(
                group.Key.BookId,
                group.Key.Book,
                group.Sum(position => position.MarketValue),
                Add(group.Select(position => position.Pnl))))
            .OrderBy(book => book.Book)
            .ToList();

        return new PortfolioSummaryDto(marketDate, books, Add(books.Select(book => book.Pnl)));
    }

    private static PositionDto ValuePosition(BookPosition held, PortfolioState state, MarketSnapshot market, ReferenceCatalog catalog)
    {
        var position = held.Position;
        var markPrice = market.MarkPrice(held.Instrument, position.Quantity);
        var valuation = position.Value(markPrice, market.UsdPerEur);

        var total = position.Realized + valuation.Unrealized;
        var baseline = state.PreviousCloseTotals.GetValueOrDefault(new PositionKey(held.BookId, held.Instrument.Id));

        var pnl = new PnlFigures(
            Realized: Round(position.Realized.Amount),
            Unrealized: Round(valuation.Unrealized.Amount),
            PriceEffect: Round(position.RealizedPriceEffect.Amount + valuation.PriceEffect.Amount),
            FxEffect: Round(position.RealizedFxEffect.Amount + valuation.FxEffect.Amount),
            Total: Round(total.Amount),
            Day: Round(total.Amount - baseline));

        return new PositionDto(
            held.BookId,
            catalog.Book(held.BookId).Name,
            held.Instrument.Id,
            held.Instrument.Symbol,
            held.Instrument.AssetClass,
            held.Instrument.Sector,
            position.Quantity,
            position.AveragePrice,
            markPrice,
            Round(valuation.MarketValue.Amount),
            pnl);
    }

    private static PnlFigures Add(IEnumerable<PnlFigures> figures)
    {
        var all = figures.ToList();

        return new PnlFigures(
            all.Sum(figure => figure.Realized),
            all.Sum(figure => figure.Unrealized),
            all.Sum(figure => figure.PriceEffect),
            all.Sum(figure => figure.FxEffect),
            all.Sum(figure => figure.Total),
            all.Sum(figure => figure.Day));
    }

    private static decimal Round(decimal amount) => Math.Round(amount, EuroDecimals, MidpointRounding.AwayFromZero);
}
