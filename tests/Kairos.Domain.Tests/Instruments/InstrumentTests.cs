using Kairos.Domain.Instruments;
using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.Instruments;

public class InstrumentTests
{
    private static readonly Instrument Bond = Instrument.TreasuryBond(
        Guid.NewGuid(),
        "UST 2Y",
        TreasuryBond.Issue(Tenor.TwoYears, new DateOnly(2014, 1, 15), 0.02m));

    [Theory]
    [InlineData("2014-01-14", false)]
    [InlineData("2014-01-15", true)]
    [InlineData("2016-01-14", true)]
    [InlineData("2016-01-15", false)]
    public void A_bond_trades_from_its_issue_date_until_the_day_before_maturity(string date, bool expected)
    {
        Assert.Equal(expected, Bond.IsTradableOn(DateOnly.Parse(date)));
    }

    [Fact]
    public void Equities_and_eur_usd_always_trade()
    {
        var date = new DateOnly(2016, 6, 24);

        Assert.True(Instrument.Equity(Guid.NewGuid(), "AAA", "Energy").IsTradableOn(date));
        Assert.True(Instrument.EurUsd(Guid.NewGuid()).IsTradableOn(date));
    }
}
