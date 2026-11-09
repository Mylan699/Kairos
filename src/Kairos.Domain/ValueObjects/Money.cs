using System.Globalization;
using Kairos.Domain.Common;

namespace Kairos.Domain.ValueObjects;

/// <summary>
/// An amount tied to its currency. Amounts in different currencies can never be
/// added by accident: a conversion has to go through an explicit exchange rate.
/// </summary>
public readonly record struct Money(decimal Amount, Currency Currency)
{
    public static Money Zero(Currency currency) => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator -(Money money) => new(-money.Amount, money.Currency);

    public static Money operator *(Money money, decimal factor) => new(money.Amount * factor, money.Currency);

    public static Money operator *(decimal factor, Money money) => money * factor;

    public Money Round(int decimals) =>
        new(Math.Round(Amount, decimals, MidpointRounding.AwayFromZero), Currency);

    public override string ToString() =>
        $"{Amount.ToString("N2", CultureInfo.InvariantCulture)} {Currency}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new DomainException($"Cannot combine {left.Currency} and {right.Currency} amounts.");
        }
    }
}
