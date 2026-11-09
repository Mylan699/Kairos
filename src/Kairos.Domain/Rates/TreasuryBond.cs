using Kairos.Domain.Common;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Rates;

/// <summary>A US Treasury style fixed-rate bond paying a coupon every six months.</summary>
public sealed record TreasuryBond
{
    public const int CouponsPerYear = 2;

    private const int MonthsPerCoupon = 12 / CouponsPerYear;

    private TreasuryBond(Tenor originalTenor, DateOnly issueDate, decimal couponRate)
    {
        OriginalTenor = originalTenor;
        IssueDate = issueDate;
        MaturityDate = issueDate.AddMonths(originalTenor.Months);
        CouponRate = couponRate;
    }

    public Tenor OriginalTenor { get; }

    public DateOnly IssueDate { get; }

    public DateOnly MaturityDate { get; }

    /// <summary>The annual coupon as a fraction: 0.025 means 2.5 % of the nominal per year.</summary>
    public decimal CouponRate { get; }

    public static TreasuryBond Issue(Tenor originalTenor, DateOnly issueDate, decimal couponRate)
    {
        if (couponRate < 0m)
        {
            throw new DomainException("The coupon rate cannot be negative.");
        }

        return new TreasuryBond(originalTenor, issueDate, couponRate);
    }

    /// <summary>Locates a date before maturity within the coupon schedule.</summary>
    public CouponWindow CouponWindowAt(DateOnly date)
    {
        // Counting back from maturity each time keeps month-end dates from drifting.
        var upcoming = new List<DateOnly>();
        var couponDate = MaturityDate;

        while (couponDate > date)
        {
            upcoming.Insert(0, couponDate);
            couponDate = MaturityDate.AddMonths(-MonthsPerCoupon * upcoming.Count);
        }

        return new CouponWindow(couponDate, upcoming);
    }
}

/// <param name="PreviousCoupon">The last coupon date on or before the date considered.</param>
/// <param name="UpcomingCoupons">Every coupon date after it, up to and including maturity.</param>
public sealed record CouponWindow(DateOnly PreviousCoupon, IReadOnlyList<DateOnly> UpcomingCoupons);
