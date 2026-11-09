using Kairos.Domain.Common;
using Kairos.Domain.Fx;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.Fx;

public class CurrencyConverterTests
{
    private const decimal UsdPerEur = 1.25m;

    [Fact]
    public void Dollars_are_divided_by_the_rate_to_get_euros()
    {
        var euros = CurrencyConverter.Convert(new Money(125m, Currency.USD), Currency.EUR, UsdPerEur);

        Assert.Equal(new Money(100m, Currency.EUR), euros);
    }

    [Fact]
    public void Euros_are_multiplied_by_the_rate_to_get_dollars()
    {
        var dollars = CurrencyConverter.Convert(new Money(100m, Currency.EUR), Currency.USD, UsdPerEur);

        Assert.Equal(new Money(125m, Currency.USD), dollars);
    }

    [Theory]
    [InlineData(Currency.EUR)]
    [InlineData(Currency.USD)]
    public void Converting_into_the_same_currency_leaves_the_amount_unchanged(Currency currency)
    {
        var money = new Money(42m, currency);

        Assert.Equal(money, CurrencyConverter.Convert(money, currency, UsdPerEur));
    }

    [Fact]
    public void Negative_amounts_keep_their_sign()
    {
        var euros = CurrencyConverter.Convert(new Money(-125m, Currency.USD), Currency.EUR, UsdPerEur);

        Assert.Equal(new Money(-100m, Currency.EUR), euros);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.1)]
    public void A_non_positive_rate_is_rejected(double rate)
    {
        var money = new Money(1m, Currency.USD);

        Assert.Throws<DomainException>(() => CurrencyConverter.Convert(money, Currency.EUR, (decimal)rate));
    }
}
