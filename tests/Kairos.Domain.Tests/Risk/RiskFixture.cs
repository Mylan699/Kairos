using Kairos.Domain.Fx;
using Kairos.Domain.Instruments;
using Kairos.Domain.Market;
using Kairos.Domain.Positions;
using Kairos.Domain.Rates;
using Kairos.Domain.Risk;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.Risk;

internal static class RiskFixture
{
    public const decimal Rate = 1.25m;

    public static readonly DateOnly MarketDate = new(2016, 1, 15);
    public static readonly Guid BookA = Guid.NewGuid();
    public static readonly Guid BookB = Guid.NewGuid();

    public static readonly Instrument Tech = Instrument.Equity(Guid.NewGuid(), "AAA", "Technology");
    public static readonly Instrument Energy = Instrument.Equity(Guid.NewGuid(), "BBB", "Energy");
    public static readonly Instrument EurUsd = Instrument.EurUsd(Guid.NewGuid());
    public static readonly Instrument TenYear = Instrument.TreasuryBond(
        Guid.NewGuid(),
        "UST 10Y",
        TreasuryBond.Issue(Tenor.TenYears, new DateOnly(2014, 1, 15), 0.03m));

    public static YieldCurve FlatCurve(decimal yield) =>
        new(Tenor.CurvePoints.Select(tenor => new CurvePoint(tenor, yield)));

    public static MarketSnapshot SampleMarket(decimal techPrice = 100m, decimal energyPrice = 50m, decimal rate = Rate, decimal yield = 0.03m) =>
        new(
            MarketDate,
            new Dictionary<Guid, decimal> { [Tech.Id] = techPrice, [Energy.Id] = energyPrice },
            new EurUsdQuote(rate, rate),
            FlatCurve(yield));

    public static BookPosition Held(Guid book, Instrument instrument, Side side, decimal quantity, decimal price, decimal rate = Rate) =>
        new(book, instrument, Position.FromFills(instrument.AssetClass, [new Fill(side, quantity, price, rate)]));
}
