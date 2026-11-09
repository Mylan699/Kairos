using Kairos.Domain.ValueObjects;
using Kairos.Infrastructure.Seeding;

namespace Kairos.Infrastructure.Tests.Seeding;

public class ReferenceSeedTests
{
    private static readonly IReadOnlyList<DateOnly> MarketDays = Weekdays(new DateOnly(2010, 1, 4), new DateOnly(2020, 4, 29));

    private static List<DateOnly> Weekdays(DateOnly from, DateOnly to)
    {
        var days = new List<DateOnly>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                days.Add(day);
            }
        }

        return days;
    }

    [Fact]
    public void The_universe_has_forty_equities_across_eleven_sectors()
    {
        Assert.Equal(40, ReferenceSeed.EquitySectors.Count);
        Assert.Equal(11, ReferenceSeed.EquitySectors.Values.Distinct().Count());
    }

    [Fact]
    public void Ids_are_stable_from_one_run_to_the_next()
    {
        Assert.Equal(new Guid("6fd926c0-6ffe-734d-74c7-413a48c5c386"), ReferenceSeed.IdOf("stable-reference"));
        Assert.NotEqual(ReferenceSeed.EquityId("MSFT"), ReferenceSeed.EquityId("ORCL"));
    }

    [Fact]
    public void Each_maturity_is_issued_again_when_the_previous_bond_expires()
    {
        var bonds = BondSchedule.Issue(MarketDays, (_, _) => 0.02m);

        var issues = bonds.GroupBy(bond => bond.Bond!.OriginalTenor).ToDictionary(group => group.Key, group => group.Count());
        Assert.Equal(6, issues[Tenor.TwoYears]);
        Assert.Equal(3, issues[Tenor.FiveYears]);
        Assert.Equal(2, issues[Tenor.TenYears]);
        Assert.Equal(1, issues[Tenor.ThirtyYears]);
    }

    [Fact]
    public void A_bond_of_every_maturity_can_trade_on_every_market_day()
    {
        var bonds = BondSchedule.Issue(MarketDays, (_, _) => 0.02m);

        foreach (var day in MarketDays.Where((_, index) => index % 20 == 0))
        {
            Assert.Equal(4, bonds.Count(bond => bond.IsTradableOn(day)));
        }
    }

    [Fact]
    public void The_coupon_is_the_curve_yield_on_the_issue_date()
    {
        var bonds = BondSchedule.Issue(MarketDays, (date, tenor) => date.Year / 100_000m + tenor.Months / 1_000_000m);

        var bond = bonds.Single(item => item.Symbol == "UST 5Y 2015");

        Assert.Equal(0.02015m + 0.00006m, bond.Bond!.CouponRate);
    }

    [Fact]
    public void Bond_symbols_are_unique()
    {
        var bonds = BondSchedule.Issue(MarketDays, (_, _) => 0.02m);

        Assert.Equal(bonds.Count, bonds.Select(bond => bond.Symbol).Distinct().Count());
    }
}
