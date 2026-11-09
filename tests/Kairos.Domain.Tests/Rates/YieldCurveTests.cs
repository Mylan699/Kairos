using Kairos.Domain.Common;
using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.Rates;

public class YieldCurveTests
{
    private static readonly YieldCurve Curve = new(
    [
        new CurvePoint(Tenor.TenYears, 0.03m),
        new CurvePoint(Tenor.OneYear, 0.01m),
        new CurvePoint(Tenor.FiveYears, 0.02m),
    ]);

    [Fact]
    public void Points_are_sorted_by_maturity()
    {
        Assert.Equal([12, 60, 120], Curve.Points.Select(point => point.Tenor.Months));
    }

    [Fact]
    public void A_yield_on_a_curve_point_is_returned_as_is()
    {
        Assert.Equal(0.02m, Curve.YieldAt(5m));
    }

    [Theory]
    [InlineData(7.5, 0.025)]
    [InlineData(6, 0.022)]
    [InlineData(3, 0.015)]
    public void A_yield_between_two_points_is_interpolated_linearly(double years, double expected)
    {
        Assert.Equal((decimal)expected, Curve.YieldAt((decimal)years));
    }

    [Fact]
    public void A_maturity_shorter_than_the_curve_uses_the_first_yield()
    {
        Assert.Equal(0.01m, Curve.YieldAt(0.25m));
    }

    [Fact]
    public void A_maturity_longer_than_the_curve_uses_the_last_yield()
    {
        Assert.Equal(0.03m, Curve.YieldAt(30m));
    }

    [Fact]
    public void Shifting_moves_every_point_by_the_same_basis_points()
    {
        var shifted = Curve.ShiftedBy(100m);

        Assert.Equal([0.02m, 0.03m, 0.04m], shifted.Points.Select(point => point.Yield));
    }

    [Fact]
    public void Shifting_leaves_the_original_curve_untouched()
    {
        Curve.ShiftedBy(100m);

        Assert.Equal(0.01m, Curve.YieldAt(1m));
    }

    [Fact]
    public void An_empty_curve_is_rejected()
    {
        Assert.Throws<DomainException>(() => new YieldCurve([]));
    }

    [Fact]
    public void A_duplicated_tenor_is_rejected()
    {
        CurvePoint[] points = [new(Tenor.OneYear, 0.01m), new(Tenor.OneYear, 0.02m)];

        Assert.Throws<DomainException>(() => new YieldCurve(points));
    }
}
