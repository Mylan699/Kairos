using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Trades;

/// <summary>Computes the date on which a trade is settled, skipping weekends.</summary>
public static class ValueDateCalculator
{
    private const int EquityAndFxDays = 2;
    private const int BondDays = 1;

    public static DateOnly ValueDateFor(AssetClass assetClass, DateOnly tradeDate) =>
        AddBusinessDays(tradeDate, assetClass == AssetClass.Bond ? BondDays : EquityAndFxDays);

    public static bool IsWeekend(DateOnly date) =>
        date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static DateOnly AddBusinessDays(DateOnly date, int businessDays)
    {
        var result = date;
        var remaining = businessDays;

        while (remaining > 0)
        {
            result = result.AddDays(1);
            if (!IsWeekend(result))
            {
                remaining--;
            }
        }

        return result;
    }
}
