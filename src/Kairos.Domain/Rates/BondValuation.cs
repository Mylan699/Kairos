namespace Kairos.Domain.Rates;

/// <summary>The result of pricing a bond. Prices and DV01 are per 100 of nominal.</summary>
/// <param name="CleanPrice">The quoted price, without accrued interest.</param>
/// <param name="DirtyPrice">The price actually paid: clean price plus accrued interest.</param>
/// <param name="AccruedInterest">The share of the current coupon earned since the last payment.</param>
/// <param name="Yield">The yield read on the curve at the residual maturity, as a fraction.</param>
/// <param name="ModifiedDuration">The percentage price change for a 1 % change in yield.</param>
/// <param name="Dv01">The price change for a one basis point change in yield.</param>
public sealed record BondValuation(
    decimal CleanPrice,
    decimal DirtyPrice,
    decimal AccruedInterest,
    decimal Yield,
    decimal ModifiedDuration,
    decimal Dv01);
