using Kairos.Domain.Fx;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Positions;

/// <summary>
/// The net holding of one instrument at weighted average cost. Immutable:
/// applying a fill returns the new state, so a position is the replay of its fills.
/// </summary>
public sealed record Position
{
    private Position(AssetClass assetClass) => AssetClass = assetClass;

    public AssetClass AssetClass { get; }

    /// <summary>Positive when long, negative when short, zero when flat.</summary>
    public decimal Quantity { get; private init; }

    /// <summary>Average entry price in dollars. Zero when flat.</summary>
    public decimal AveragePrice { get; private init; }

    /// <summary>Average entry price converted to euros at the rate of each purchase. Zero when flat.</summary>
    public decimal AverageCostInEur { get; private init; }

    public Money RealizedPriceEffect { get; private init; } = Money.Zero(Currency.EUR);

    public Money RealizedFxEffect { get; private init; } = Money.Zero(Currency.EUR);

    public Money Realized => RealizedPriceEffect + RealizedFxEffect;

    public static Position Flat(AssetClass assetClass) => new(assetClass);

    public static Position FromFills(AssetClass assetClass, IEnumerable<Fill> fills) =>
        fills.Aggregate(Flat(assetClass), (position, fill) => position.Apply(fill));

    public Position Apply(Fill fill)
    {
        var addsToPosition = Quantity == 0m || Math.Sign(Quantity) == Math.Sign(fill.SignedQuantity);
        return addsToPosition ? Extend(fill) : Reduce(fill);
    }

    /// <param name="markPrice">The current price in dollars (per 100 of nominal for a bond).</param>
    /// <param name="usdPerEur">The current EUR/USD rate.</param>
    public PositionValuation Value(decimal markPrice, decimal usdPerEur)
    {
        var marketValueInUsd = new Money(AssetClass.ContractMultiplier() * Quantity * markPrice, Currency.USD);
        var marketValue = CurrencyConverter.Convert(marketValueInUsd, Currency.EUR, usdPerEur);
        var (priceEffect, fxEffect) = EffectsOf(Quantity, markPrice, usdPerEur);

        return new PositionValuation(marketValue, priceEffect, fxEffect);
    }

    private Position Extend(Fill fill)
    {
        var held = Math.Abs(Quantity);
        var total = held + fill.Quantity;

        return this with
        {
            Quantity = Quantity + fill.SignedQuantity,
            AveragePrice = (held * AveragePrice + fill.Quantity * fill.Price) / total,
            AverageCostInEur = (held * AverageCostInEur + fill.Quantity * fill.PriceInEur) / total,
        };
    }

    private Position Reduce(Fill fill)
    {
        var direction = Math.Sign(Quantity);
        var closed = Math.Min(Math.Abs(Quantity), fill.Quantity);
        var (priceEffect, fxEffect) = EffectsOf(closed * direction, fill.Price, fill.UsdPerEur);

        var remaining = Quantity + fill.SignedQuantity;
        var isFlat = remaining == 0m;
        var hasReversed = !isFlat && Math.Sign(remaining) != direction;

        // The average cost survives a partial close. Going flat clears it, and the
        // part of a fill that reverses the position opens at the price of that fill.
        return this with
        {
            Quantity = remaining,
            AveragePrice = isFlat ? 0m : hasReversed ? fill.Price : AveragePrice,
            AverageCostInEur = isFlat ? 0m : hasReversed ? fill.PriceInEur : AverageCostInEur,
            RealizedPriceEffect = RealizedPriceEffect + priceEffect,
            RealizedFxEffect = RealizedFxEffect + fxEffect,
        };
    }

    /// <summary>
    /// Splits the euro P&amp;L of a signed quantity into two parts that add up exactly:
    /// the price move at today's rate, and the rate move on the capital invested.
    /// </summary>
    private (Money PriceEffect, Money FxEffect) EffectsOf(decimal signedQuantity, decimal price, decimal usdPerEur)
    {
        var scaledQuantity = AssetClass.ContractMultiplier() * signedQuantity;

        var priceMoveInUsd = new Money(scaledQuantity * (price - AveragePrice), Currency.USD);
        var priceEffect = CurrencyConverter.Convert(priceMoveInUsd, Currency.EUR, usdPerEur);

        if (!AssetClass.HasCurrencyEffect())
        {
            return (priceEffect, Money.Zero(Currency.EUR));
        }

        var costInUsd = new Money(scaledQuantity * AveragePrice, Currency.USD);
        var costAtTodaysRate = CurrencyConverter.Convert(costInUsd, Currency.EUR, usdPerEur);
        var costAtEntryRates = new Money(scaledQuantity * AverageCostInEur, Currency.EUR);

        return (priceEffect, costAtTodaysRate - costAtEntryRates);
    }
}
