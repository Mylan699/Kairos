using Kairos.Domain.Common;

namespace Kairos.Domain.Rates;

/// <summary>
/// Discounts the remaining cash flows of a bond at the curve yield for its residual
/// maturity, with US Treasury conventions: semi-annual compounding and actual/actual accrual.
/// </summary>
/// <remarks>
/// The maths runs on double because fractional powers do not exist for decimal.
/// A model price has no exact value, so the difference has no business meaning.
/// </remarks>
public static class BondPricer
{
    private const double Nominal = 100d;
    private const double BasisPoint = 0.0001d;

    public static BondValuation Value(TreasuryBond bond, DateOnly valuationDate, YieldCurve curve)
    {
        if (valuationDate < bond.IssueDate)
        {
            throw new DomainException("A bond cannot be valued before its issue date.");
        }

        if (valuationDate >= bond.MaturityDate)
        {
            return new BondValuation((decimal)Nominal, (decimal)Nominal, 0m, 0m, 0m, 0m);
        }

        var window = bond.CouponWindowAt(valuationDate);
        var couponCount = window.UpcomingCoupons.Count;

        // Fraction of the current coupon period still to run: 1 right after a coupon, close to 0 just before one.
        var periodFraction = FractionOfPeriodRemaining(valuationDate, window);

        var residualYears = (periodFraction + couponCount - 1) / TreasuryBond.CouponsPerYear;
        var yield = (double)curve.YieldAt((decimal)residualYears);
        var yieldPerPeriod = yield / TreasuryBond.CouponsPerYear;
        var coupon = Nominal * (double)bond.CouponRate / TreasuryBond.CouponsPerYear;

        var dirtyPrice = 0d;
        var timeWeightedValue = 0d;

        for (var index = 0; index < couponCount; index++)
        {
            var periods = periodFraction + index;
            var cashFlow = index == couponCount - 1 ? coupon + Nominal : coupon;
            var presentValue = cashFlow / Math.Pow(1d + yieldPerPeriod, periods);

            dirtyPrice += presentValue;
            timeWeightedValue += presentValue * periods / TreasuryBond.CouponsPerYear;
        }

        var accruedInterest = coupon * (1d - periodFraction);
        var macaulayDuration = timeWeightedValue / dirtyPrice;
        var modifiedDuration = macaulayDuration / (1d + yieldPerPeriod);

        return new BondValuation(
            CleanPrice: (decimal)(dirtyPrice - accruedInterest),
            DirtyPrice: (decimal)dirtyPrice,
            AccruedInterest: (decimal)accruedInterest,
            Yield: (decimal)yield,
            ModifiedDuration: (decimal)modifiedDuration,
            Dv01: (decimal)(modifiedDuration * dirtyPrice * BasisPoint));
    }

    private static double FractionOfPeriodRemaining(DateOnly valuationDate, CouponWindow window)
    {
        var nextCoupon = window.UpcomingCoupons[0];
        double daysToNextCoupon = nextCoupon.DayNumber - valuationDate.DayNumber;
        double daysInPeriod = nextCoupon.DayNumber - window.PreviousCoupon.DayNumber;

        return daysToNextCoupon / daysInPeriod;
    }
}
