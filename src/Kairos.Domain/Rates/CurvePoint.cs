using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Rates;

/// <param name="Tenor">The maturity of this point on the curve.</param>
/// <param name="Yield">The yield as a fraction: 0.025 means 2.5 %.</param>
public readonly record struct CurvePoint(Tenor Tenor, decimal Yield);
