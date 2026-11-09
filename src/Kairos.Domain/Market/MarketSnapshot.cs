using Kairos.Domain.Common;
using Kairos.Domain.Fx;
using Kairos.Domain.Instruments;
using Kairos.Domain.Rates;
using Kairos.Domain.Risk;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Market;

/// <summary>Every market data needed to value the book at one moment.</summary>
/// <param name="Date">The market date, which drives bond residual maturities.</param>
/// <param name="EquityPrices">Equity prices in dollars, by instrument id.</param>
/// <param name="EurUsd">The EUR/USD quote.</param>
/// <param name="Curve">The Treasury yield curve.</param>
public sealed record MarketSnapshot(
    DateOnly Date,
    IReadOnlyDictionary<Guid, decimal> EquityPrices,
    EurUsdQuote EurUsd,
    YieldCurve Curve)
{
    public decimal UsdPerEur => EurUsd.Mid;

    /// <summary>The price a position would be valued at, in dollars (per 100 of nominal for a bond).</summary>
    public decimal MarkPrice(Instrument instrument, decimal positionQuantity) => instrument.AssetClass switch
    {
        AssetClass.Equity => EquityPrices.TryGetValue(instrument.Id, out var price)
            ? price
            : throw new DomainException($"No market price for {instrument.Symbol}."),
        AssetClass.Fx => EurUsd.ClosingPrice(positionQuantity),
        AssetClass.Bond => BondPricer.Value(instrument.Bond!, Date, Curve).CleanPrice,
        _ => throw new DomainException($"Unknown asset class {instrument.AssetClass}."),
    };

    /// <summary>The same market after the moves of a scenario.</summary>
    public MarketSnapshot Apply(StressScenario scenario)
    {
        var eurUsdFactor = 1m + scenario.EurUsdReturn;

        return this with
        {
            EquityPrices = EquityPrices.ToDictionary(
                price => price.Key,
                price => price.Value * (1m + scenario.EquityReturnFor(price.Key))),
            EurUsd = new EurUsdQuote(EurUsd.Bid * eurUsdFactor, EurUsd.Ask * eurUsdFactor),
            Curve = Curve.ShiftedBy(scenario.CurveShiftFor),
        };
    }
}
