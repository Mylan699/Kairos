using Kairos.Domain.Common;
using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.Rates;

public class TreasuryBondTests
{
    private static readonly TreasuryBond TwoYearBond =
        TreasuryBond.Issue(Tenor.TwoYears, new DateOnly(2014, 1, 15), 0.05m);

    [Fact]
    public void Maturity_is_the_issue_date_plus_the_original_tenor()
    {
        Assert.Equal(new DateOnly(2016, 1, 15), TwoYearBond.MaturityDate);
    }

    [Fact]
    public void A_negative_coupon_is_rejected()
    {
        Assert.Throws<DomainException>(() =>
            TreasuryBond.Issue(Tenor.TwoYears, new DateOnly(2014, 1, 15), -0.01m));
    }

    [Fact]
    public void The_window_lists_every_coupon_still_to_come()
    {
        var window = TwoYearBond.CouponWindowAt(new DateOnly(2014, 10, 1));

        Assert.Equal(new DateOnly(2014, 7, 15), window.PreviousCoupon);
        Assert.Equal(
            [new DateOnly(2015, 1, 15), new DateOnly(2015, 7, 15), new DateOnly(2016, 1, 15)],
            window.UpcomingCoupons);
    }

    [Fact]
    public void On_a_coupon_date_that_coupon_is_already_paid()
    {
        var window = TwoYearBond.CouponWindowAt(new DateOnly(2015, 7, 15));

        Assert.Equal(new DateOnly(2015, 7, 15), window.PreviousCoupon);
        Assert.Equal([new DateOnly(2016, 1, 15)], window.UpcomingCoupons);
    }

    [Fact]
    public void Month_end_coupon_dates_do_not_drift()
    {
        var bond = TreasuryBond.Issue(Tenor.OneYear, new DateOnly(2015, 8, 31), 0.02m);

        var window = bond.CouponWindowAt(new DateOnly(2015, 9, 1));

        Assert.Equal(new DateOnly(2015, 8, 31), window.PreviousCoupon);
        Assert.Equal([new DateOnly(2016, 2, 29), new DateOnly(2016, 8, 31)], window.UpcomingCoupons);
    }
}
