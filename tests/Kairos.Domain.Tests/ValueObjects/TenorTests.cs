using Kairos.Domain.Common;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.ValueObjects;

public class TenorTests
{
    [Fact]
    public void Curve_points_match_the_published_maturities_in_ascending_order()
    {
        var labels = Tenor.CurvePoints.Select(tenor => tenor.Label);

        Assert.Equal(["1M", "3M", "6M", "1Y", "2Y", "3Y", "5Y", "7Y", "10Y", "20Y", "30Y"], labels);
    }

    [Fact]
    public void Years_expresses_the_maturity_as_a_fraction_of_a_year()
    {
        Assert.Equal(0.5m, Tenor.SixMonths.Years);
        Assert.Equal(30m, Tenor.ThirtyYears.Years);
    }

    [Theory]
    [InlineData("10Y")]
    [InlineData("10y")]
    public void Parse_finds_a_tenor_whatever_the_case(string label)
    {
        Assert.Equal(Tenor.TenYears, Tenor.Parse(label));
    }

    [Fact]
    public void FromMonths_finds_a_standard_maturity()
    {
        Assert.Equal(Tenor.TwoYears, Tenor.FromMonths(24));
    }

    [Fact]
    public void FromMonths_rejects_a_non_standard_maturity()
    {
        Assert.Throws<DomainException>(() => Tenor.FromMonths(48));
    }

    [Fact]
    public void Parse_rejects_an_unknown_maturity()
    {
        Assert.Throws<DomainException>(() => Tenor.Parse("4Y"));
    }
}
