using Kairos.Domain.Common;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Adding_amounts_in_the_same_currency_sums_them()
    {
        var total = new Money(100.50m, Currency.EUR) + new Money(0.25m, Currency.EUR);

        Assert.Equal(new Money(100.75m, Currency.EUR), total);
    }

    [Fact]
    public void Subtracting_amounts_in_the_same_currency_gives_the_difference()
    {
        var difference = new Money(100m, Currency.USD) - new Money(130m, Currency.USD);

        Assert.Equal(new Money(-30m, Currency.USD), difference);
    }

    [Fact]
    public void Adding_amounts_in_different_currencies_is_rejected()
    {
        var euros = new Money(1m, Currency.EUR);
        var dollars = new Money(1m, Currency.USD);

        Assert.Throws<DomainException>(() => euros + dollars);
    }

    [Fact]
    public void Subtracting_amounts_in_different_currencies_is_rejected()
    {
        var euros = new Money(1m, Currency.EUR);
        var dollars = new Money(1m, Currency.USD);

        Assert.Throws<DomainException>(() => euros - dollars);
    }

    [Fact]
    public void Negating_flips_the_sign_and_keeps_the_currency()
    {
        Assert.Equal(new Money(-12m, Currency.USD), -new Money(12m, Currency.USD));
    }

    [Fact]
    public void Multiplying_scales_the_amount_on_either_side()
    {
        var money = new Money(2.5m, Currency.EUR);

        Assert.Equal(new Money(10m, Currency.EUR), money * 4m);
        Assert.Equal(new Money(10m, Currency.EUR), 4m * money);
    }

    [Theory]
    [InlineData(1.005, 1.01)]
    [InlineData(1.004, 1.00)]
    [InlineData(-1.005, -1.01)]
    public void Rounding_sends_midpoints_away_from_zero(double amount, double expected)
    {
        var rounded = new Money((decimal)amount, Currency.EUR).Round(2);

        Assert.Equal((decimal)expected, rounded.Amount);
    }

    [Fact]
    public void Zero_has_no_amount_in_the_requested_currency()
    {
        Assert.Equal(new Money(0m, Currency.USD), Money.Zero(Currency.USD));
    }

    [Fact]
    public void ToString_uses_two_decimals_and_the_currency_code()
    {
        Assert.Equal("1,234.50 EUR", new Money(1234.5m, Currency.EUR).ToString());
    }
}
