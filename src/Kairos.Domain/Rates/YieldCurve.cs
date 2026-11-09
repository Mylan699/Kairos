using Kairos.Domain.Common;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Rates;

/// <summary>The Treasury yields of one date, across maturities.</summary>
public sealed class YieldCurve
{
    private const decimal BasisPointsPerUnit = 10_000m;

    public YieldCurve(IEnumerable<CurvePoint> points)
    {
        Points = points.OrderBy(point => point.Tenor.Months).ToList();

        if (Points.Count == 0)
        {
            throw new DomainException("A yield curve needs at least one point.");
        }

        if (Points.DistinctBy(point => point.Tenor).Count() != Points.Count)
        {
            throw new DomainException("A yield curve cannot contain the same tenor twice.");
        }
    }

    /// <summary>The points of the curve, from the shortest maturity to the longest.</summary>
    public IReadOnlyList<CurvePoint> Points { get; }

    /// <summary>
    /// Interpolates linearly between the two surrounding points. Beyond either
    /// end of the curve, the yield of the closest point is used.
    /// </summary>
    public decimal YieldAt(decimal years)
    {
        var first = Points[0];
        if (years <= first.Tenor.Years)
        {
            return first.Yield;
        }

        for (var index = 1; index < Points.Count; index++)
        {
            var lower = Points[index - 1];
            var upper = Points[index];

            if (years <= upper.Tenor.Years)
            {
                var weight = (years - lower.Tenor.Years) / (upper.Tenor.Years - lower.Tenor.Years);
                return lower.Yield + (upper.Yield - lower.Yield) * weight;
            }
        }

        return Points[^1].Yield;
    }

    /// <summary>Moves each point towards another curve: a weight of 0 gives this curve, 1 gives the other.</summary>
    public YieldCurve BlendedWith(YieldCurve other, decimal weight) =>
        new(Points.Select(point => point with
        {
            Yield = point.Yield + (other.YieldAt(point.Tenor.Years) - point.Yield) * weight,
        }));

    /// <summary>Moves every point of the curve by the same number of basis points.</summary>
    public YieldCurve ShiftedBy(decimal basisPoints) => ShiftedBy(_ => basisPoints);

    /// <summary>Moves each point of the curve by the basis points given for its maturity.</summary>
    public YieldCurve ShiftedBy(Func<Tenor, decimal> basisPointsForTenor) =>
        new(Points.Select(point => point with
        {
            Yield = point.Yield + basisPointsForTenor(point.Tenor) / BasisPointsPerUnit,
        }));
}
