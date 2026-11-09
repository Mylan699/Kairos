using System.Globalization;
using Kairos.Domain.Common;
using Kairos.Domain.Positions;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Trades;

/// <summary>The economic content of a trade: what an amendment is allowed to change.</summary>
public sealed record TradeTerms
{
    public TradeTerms(
        Guid instrumentId,
        Side side,
        decimal quantity,
        decimal price,
        Guid counterpartyId,
        Guid bookId,
        Guid traderId,
        DateOnly tradeDate,
        DateOnly valueDate,
        decimal usdPerEurAtTrade)
    {
        if (quantity <= 0m)
        {
            throw new DomainException("A trade quantity must be strictly positive.");
        }

        if (price <= 0m)
        {
            throw new DomainException("A trade price must be strictly positive.");
        }

        if (valueDate < tradeDate)
        {
            throw new DomainException("The value date cannot be before the trade date.");
        }

        if (ValueDateCalculator.IsWeekend(valueDate))
        {
            throw new DomainException("The value date cannot fall on a weekend.");
        }

        if (usdPerEurAtTrade <= 0m)
        {
            throw new DomainException("The EUR/USD rate of a trade must be strictly positive.");
        }

        InstrumentId = instrumentId;
        Side = side;
        Quantity = quantity;
        Price = price;
        CounterpartyId = counterpartyId;
        BookId = bookId;
        TraderId = traderId;
        TradeDate = tradeDate;
        ValueDate = valueDate;
        UsdPerEurAtTrade = usdPerEurAtTrade;
    }

    public Guid InstrumentId { get; }

    public Side Side { get; }

    public decimal Quantity { get; }

    public decimal Price { get; }

    public Guid CounterpartyId { get; }

    public Guid BookId { get; }

    public Guid TraderId { get; }

    public DateOnly TradeDate { get; }

    public DateOnly ValueDate { get; }

    /// <summary>The EUR/USD rate when the trade was done: the basis of its euro cost.</summary>
    public decimal UsdPerEurAtTrade { get; }

    public Fill ToFill() => new(Side, Quantity, Price, UsdPerEurAtTrade);

    /// <summary>Lists what differs from a previous version, for the audit trail.</summary>
    public string DescribeChangesFrom(TradeTerms previous)
    {
        var changes = new List<string>();

        void Compare<T>(string field, T before, T after)
        {
            if (!EqualityComparer<T>.Default.Equals(before, after))
            {
                changes.Add($"{field}: {Format(before)} -> {Format(after)}");
            }
        }

        // A decimal read from the database keeps the scale of its column: 100.5 comes back as 100.5000.
        static string Format<T>(T value) => value is decimal number
            ? number.ToString("G29", CultureInfo.InvariantCulture)
            : FormattableString.Invariant($"{value}");

        Compare(nameof(InstrumentId), previous.InstrumentId, InstrumentId);
        Compare(nameof(Side), previous.Side, Side);
        Compare(nameof(Quantity), previous.Quantity, Quantity);
        Compare(nameof(Price), previous.Price, Price);
        Compare(nameof(CounterpartyId), previous.CounterpartyId, CounterpartyId);
        Compare(nameof(BookId), previous.BookId, BookId);
        Compare(nameof(TraderId), previous.TraderId, TraderId);
        Compare(nameof(TradeDate), previous.TradeDate, TradeDate);
        Compare(nameof(ValueDate), previous.ValueDate, ValueDate);
        Compare(nameof(UsdPerEurAtTrade), previous.UsdPerEurAtTrade, UsdPerEurAtTrade);

        return string.Join("; ", changes);
    }
}
