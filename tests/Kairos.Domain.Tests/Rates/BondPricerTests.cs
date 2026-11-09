using Kairos.Domain.Common;
using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.Rates;

public class BondPricerTests
{
    private static readonly DateOnly IssueDate = new(2014, 1, 15);
    private static readonly TreasuryBond FivePercentTwoYear = TreasuryBond.Issue(Tenor.TwoYears, IssueDate, 0.05m);

    private static YieldCurve FlatCurve(decimal yield) =>
        new(Tenor.CurvePoints.Select(tenor => new CurvePoint(tenor, yield)));

    [Fact]
    public void A_bond_discounted_at_its_coupon_on_a_coupon_date_is_worth_par()
    {
        var bond = TreasuryBond.Issue(Tenor.TenYears, IssueDate, 0.03m);

        var valuation = BondPricer.Value(bond, IssueDate, FlatCurve(0.03m));

        Assert.Equal(100m, valuation.CleanPrice, 8);
        Assert.Equal(0m, valuation.AccruedInterest, 8);
    }

    [Fact]
    public void Price_duration_and_dv01_match_a_hand_calculation()
    {
        // Two years left, 5 % coupon, 6 % yield: four flows discounted at 3 % per half-year.
        var valuation = BondPricer.Value(FivePercentTwoYear, IssueDate, FlatCurve(0.06m));

        Assert.Equal(98.1415m, valuation.CleanPrice, 4);
        Assert.Equal(1.8711m, valuation.ModifiedDuration, 4);
        Assert.Equal(0.0184m, valuation.Dv01, 4);
    }

    [Fact]
    public void Between_two_coupons_accrued_interest_and_discounting_are_prorated()
    {
        // 92 days into a 184-day period: half the coupon is accrued, the last flow is half a period away.
        var valuation = BondPricer.Value(FivePercentTwoYear, new DateOnly(2015, 10, 15), FlatCurve(0.06m));

        Assert.Equal(1.25m, valuation.AccruedInterest, 8);
        Assert.Equal(100.99625m, valuation.DirtyPrice, 5);
        Assert.Equal(99.74625m, valuation.CleanPrice, 5);
    }

    [Fact]
    public void The_yield_is_interpolated_at_the_residual_maturity()
    {
        var bond = TreasuryBond.Issue(Tenor.TenYears, new DateOnly(2010, 1, 15), 0.03m);
        var curve = new YieldCurve([new CurvePoint(Tenor.FiveYears, 0.02m), new CurvePoint(Tenor.TenYears, 0.03m)]);

        var valuation = BondPricer.Value(bond, new DateOnly(2012, 7, 15), curve);

        Assert.Equal(0.025m, valuation.Yield, 10);
    }

    [Fact]
    public void A_higher_yield_lowers_the_price()
    {
        var low = BondPricer.Value(FivePercentTwoYear, IssueDate, FlatCurve(0.02m));
        var high = BondPricer.Value(FivePercentTwoYear, IssueDate, FlatCurve(0.08m));

        Assert.True(high.CleanPrice < low.CleanPrice);
    }

    [Fact]
    public void Dv01_matches_the_price_change_of_a_one_basis_point_move()
    {
        var bond = TreasuryBond.Issue(Tenor.TenYears, IssueDate, 0.025m);
        var curve = FlatCurve(0.03m);

        var valuation = BondPricer.Value(bond, IssueDate, curve);
        var up = BondPricer.Value(bond, IssueDate, curve.ShiftedBy(1m));
        var down = BondPricer.Value(bond, IssueDate, curve.ShiftedBy(-1m));

        Assert.Equal((down.DirtyPrice - up.DirtyPrice) / 2m, valuation.Dv01, 6);
    }

    [Fact]
    public void A_longer_bond_has_a_higher_duration()
    {
        var shortBond = BondPricer.Value(TreasuryBond.Issue(Tenor.TwoYears, IssueDate, 0.03m), IssueDate, FlatCurve(0.03m));
        var longBond = BondPricer.Value(TreasuryBond.Issue(Tenor.ThirtyYears, IssueDate, 0.03m), IssueDate, FlatCurve(0.03m));

        Assert.True(longBond.ModifiedDuration > shortBond.ModifiedDuration);
    }

    [Fact]
    public void On_or_after_maturity_a_bond_is_redeemed_at_par_with_no_risk()
    {
        var valuation = BondPricer.Value(FivePercentTwoYear, FivePercentTwoYear.MaturityDate, FlatCurve(0.06m));

        Assert.Equal(100m, valuation.CleanPrice);
        Assert.Equal(0m, valuation.Dv01);
        Assert.Equal(0m, valuation.ModifiedDuration);
    }

    [Fact]
    public void Valuing_before_the_issue_date_is_rejected()
    {
        Assert.Throws<DomainException>(() =>
            BondPricer.Value(FivePercentTwoYear, IssueDate.AddDays(-1), FlatCurve(0.06m)));
    }
}
