using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Portfolio;

/// <summary>Result figures in euros, shared by a position, a book and the whole desk.</summary>
public sealed record PnlFigures(
    decimal Realized,
    decimal Unrealized,
    decimal PriceEffect,
    decimal FxEffect,
    decimal Total,
    decimal Day);

public sealed record PositionDto(
    Guid BookId,
    string Book,
    Guid InstrumentId,
    string Symbol,
    AssetClass AssetClass,
    string? Sector,
    decimal Quantity,
    decimal AveragePrice,
    decimal MarkPrice,
    decimal MarketValue,
    PnlFigures Pnl);

public sealed record BookSummaryDto(Guid BookId, string Book, decimal MarketValue, PnlFigures Pnl);

public sealed record PnlHistoryPoint(DateOnly Date, decimal Total);

public sealed record PortfolioSummaryDto(DateOnly MarketDate, IReadOnlyList<BookSummaryDto> Books, PnlFigures Total);

/// <summary>What a trade would change, shown before it is booked.</summary>
/// <param name="NotionalEur">The size of the trade in euros.</param>
/// <param name="PnlImpactEur">The immediate result: the gap between the trade price and the mark.</param>
/// <param name="PositionBefore">The signed quantity held today.</param>
/// <param name="PositionAfter">The signed quantity once the trade is done.</param>
/// <param name="Dv01ChangeEur">How much the desk DV01 would move.</param>
/// <param name="TotalDv01AfterEur">The desk DV01 once the trade is done.</param>
public sealed record TradeImpactDto(
    decimal NotionalEur,
    decimal PnlImpactEur,
    decimal PositionBefore,
    decimal PositionAfter,
    decimal Dv01ChangeEur,
    decimal TotalDv01AfterEur);
