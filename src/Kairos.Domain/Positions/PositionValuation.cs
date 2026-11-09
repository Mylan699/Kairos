using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Positions;

/// <summary>What an open position is worth today, in euros.</summary>
/// <param name="MarketValue">The signed value of the position at the current price and rate.</param>
/// <param name="PriceEffect">The share of the unrealized P&amp;L caused by the instrument price.</param>
/// <param name="FxEffect">The share of the unrealized P&amp;L caused by EUR/USD on the capital invested.</param>
public sealed record PositionValuation(Money MarketValue, Money PriceEffect, Money FxEffect)
{
    public Money Unrealized => PriceEffect + FxEffect;
}
