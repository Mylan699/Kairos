using Kairos.Domain.Common;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Fx;

/// <summary>Converts amounts between euros and dollars at an explicit EUR/USD rate.</summary>
public static class CurrencyConverter
{
    /// <param name="usdPerEur">The EUR/USD rate: how many dollars one euro is worth.</param>
    public static Money Convert(Money money, Currency target, decimal usdPerEur)
    {
        if (usdPerEur <= 0m)
        {
            throw new DomainException("The EUR/USD rate must be strictly positive.");
        }

        if (money.Currency == target)
        {
            return money;
        }

        // Only two currencies exist: a different target means the conversion crosses EUR/USD.
        return target == Currency.EUR
            ? new Money(money.Amount / usdPerEur, Currency.EUR)
            : new Money(money.Amount * usdPerEur, Currency.USD);
    }
}
