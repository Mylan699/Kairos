using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.ValueObjects;

public class SideTests
{
    [Theory]
    [InlineData(Side.Buy, 1)]
    [InlineData(Side.Sell, -1)]
    public void Sign_is_positive_for_a_buy_and_negative_for_a_sell(Side side, int expected)
    {
        Assert.Equal(expected, side.Sign());
    }

    [Theory]
    [InlineData(Side.Buy, Side.Sell)]
    [InlineData(Side.Sell, Side.Buy)]
    public void Opposite_returns_the_other_side(Side side, Side expected)
    {
        Assert.Equal(expected, side.Opposite());
    }
}
