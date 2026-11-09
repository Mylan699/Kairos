using Kairos.Domain.Common;
using Kairos.Domain.Market;
using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Risk;

/// <summary>
/// A set of market moves to apply. Returns are fractions (-0.10 means -10 %), and
/// curve shifts are in basis points.
/// </summary>
public sealed class StressScenario
{
    private const decimal BasisPointsPerUnit = 10_000m;

    private readonly decimal uniformEquityReturn;
    private readonly IReadOnlyDictionary<Guid, decimal> equityReturnsByInstrument;
    private readonly decimal uniformCurveShift;
    private readonly IReadOnlyDictionary<Tenor, decimal> curveShiftsByTenor;

    private StressScenario(
        string name,
        decimal uniformEquityReturn,
        IReadOnlyDictionary<Guid, decimal> equityReturnsByInstrument,
        decimal eurUsdReturn,
        decimal uniformCurveShift,
        IReadOnlyDictionary<Tenor, decimal> curveShiftsByTenor)
    {
        Name = name;
        this.uniformEquityReturn = uniformEquityReturn;
        this.equityReturnsByInstrument = equityReturnsByInstrument;
        EurUsdReturn = eurUsdReturn;
        this.uniformCurveShift = uniformCurveShift;
        this.curveShiftsByTenor = curveShiftsByTenor;
    }

    public string Name { get; }

    public decimal EurUsdReturn { get; }

    /// <summary>The same move for every equity and every maturity. Any shock can be left at zero.</summary>
    public static StressScenario Parametric(string name, decimal equityReturn, decimal eurUsdReturn, decimal rateShiftBasisPoints)
    {
        if (equityReturn <= -1m || eurUsdReturn <= -1m)
        {
            throw new DomainException("A market cannot lose 100 % or more of its value.");
        }

        return new StressScenario(
            name,
            equityReturn,
            new Dictionary<Guid, decimal>(),
            eurUsdReturn,
            rateShiftBasisPoints,
            new Dictionary<Tenor, decimal>());
    }

    /// <summary>The moves actually observed between two dates, instrument by instrument.</summary>
    public static StressScenario Historical(string name, MarketSnapshot start, MarketSnapshot end)
    {
        var equityReturns = start.EquityPrices
            .Where(price => end.EquityPrices.ContainsKey(price.Key))
            .ToDictionary(price => price.Key, price => end.EquityPrices[price.Key] / price.Value - 1m);

        var curveShifts = start.Curve.Points.ToDictionary(
            point => point.Tenor,
            point => (end.Curve.YieldAt(point.Tenor.Years) - point.Yield) * BasisPointsPerUnit);

        return new StressScenario(name, 0m, equityReturns, end.EurUsd.Mid / start.EurUsd.Mid - 1m, 0m, curveShifts);
    }

    public decimal EquityReturnFor(Guid instrumentId) =>
        equityReturnsByInstrument.GetValueOrDefault(instrumentId, uniformEquityReturn);

    public decimal CurveShiftFor(Tenor tenor) =>
        curveShiftsByTenor.GetValueOrDefault(tenor, uniformCurveShift);
}
