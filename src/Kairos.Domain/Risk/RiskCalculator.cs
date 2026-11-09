using Kairos.Domain.Fx;
using Kairos.Domain.Market;
using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Risk;

/// <summary>Market values in euros, split three ways.</summary>
public sealed record ExposureReport(
    IReadOnlyDictionary<AssetClass, Money> ByAssetClass,
    IReadOnlyDictionary<Currency, Money> ByCurrency,
    IReadOnlyDictionary<string, Money> BySector);

/// <summary>What the book gains or loses, in euros, for a small market move.</summary>
/// <param name="EquityDelta">For every equity rising by 1 %.</param>
/// <param name="FxDelta">For EUR/USD rising by 1 %.</param>
/// <param name="Dv01">Lost when yields rise by one basis point: positive for a long bond position.</param>
public sealed record Sensitivities(Money EquityDelta, Money FxDelta, Money Dv01);

public static class RiskCalculator
{
    private const decimal OnePercent = 0.01m;
    private const decimal BondQuotedPer = 100m;

    public static ExposureReport Exposures(IReadOnlyCollection<BookPosition> positions, MarketSnapshot market)
    {
        var byAssetClass = new Dictionary<AssetClass, Money>();
        var byCurrency = new Dictionary<Currency, Money>();
        var bySector = new Dictionary<string, Money>();

        foreach (var held in positions.Where(held => held.Instrument.IsTradableOn(market.Date)))
        {
            var markPrice = market.MarkPrice(held.Instrument, held.Position.Quantity);
            var marketValue = held.Position.Value(markPrice, market.UsdPerEur).MarketValue;

            Add(byAssetClass, held.Instrument.AssetClass, marketValue);

            if (held.Instrument.AssetClass == AssetClass.Fx)
            {
                // Long EUR/USD means holding euros financed by a dollar debt of the same value.
                Add(byCurrency, Currency.EUR, marketValue);
                Add(byCurrency, Currency.USD, -marketValue);
            }
            else
            {
                Add(byCurrency, Currency.USD, marketValue);
            }

            if (held.Instrument.Sector is { } sector)
            {
                Add(bySector, sector, marketValue);
            }
        }

        return new ExposureReport(byAssetClass, byCurrency, bySector);
    }

    public static Sensitivities Sensitivities(IReadOnlyCollection<BookPosition> positions, MarketSnapshot market)
    {
        var equityUp = StressScenario.Parametric("Equity +1%", OnePercent, 0m, 0m);
        var eurUsdUp = StressScenario.Parametric("EUR/USD +1%", 0m, OnePercent, 0m);

        return new Sensitivities(
            StressEngine.Run(positions, market, equityUp).Total,
            StressEngine.Run(positions, market, eurUsdUp).Total,
            TotalDv01(positions, market));
    }

    private static Money TotalDv01(IEnumerable<BookPosition> positions, MarketSnapshot market)
    {
        var totalInUsd = positions
            .Where(held => held.Instrument.AssetClass == AssetClass.Bond)
            .Sum(held => held.Position.Quantity / BondQuotedPer
                * BondPricer.Value(held.Instrument.Bond!, market.Date, market.Curve).Dv01);

        return CurrencyConverter.Convert(new Money(totalInUsd, Currency.USD), Currency.EUR, market.UsdPerEur);
    }

    private static void Add<TKey>(Dictionary<TKey, Money> totals, TKey key, Money amount)
        where TKey : notnull
    {
        totals[key] = totals.GetValueOrDefault(key, Money.Zero(Currency.EUR)) + amount;
    }
}
