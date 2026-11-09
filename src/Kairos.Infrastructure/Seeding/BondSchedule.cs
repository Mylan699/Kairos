using Kairos.Domain.Instruments;
using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;

namespace Kairos.Infrastructure.Seeding;

/// <summary>
/// Issues the synthetic Treasury bonds. A two-year bond cannot live through ten years of
/// history, so each maturity is issued again when the previous bond expires (ADR-010).
/// </summary>
internal static class BondSchedule
{
    private static readonly Tenor[] IssuedTenors = [Tenor.TwoYears, Tenor.FiveYears, Tenor.TenYears, Tenor.ThirtyYears];

    /// <param name="marketDays">Every market day, in ascending order.</param>
    /// <param name="yieldOn">The yield of a curve point on a market day, used as the coupon of the bond.</param>
    public static IReadOnlyList<Instrument> Issue(IReadOnlyList<DateOnly> marketDays, Func<DateOnly, Tenor, decimal> yieldOn)
    {
        var bonds = new List<Instrument>();

        foreach (var tenor in IssuedTenors)
        {
            DateOnly? issueDate = marketDays[0];

            while (issueDate is { } date)
            {
                var symbol = $"UST {tenor.Label} {date.Year}";
                bonds.Add(Instrument.TreasuryBond(ReferenceSeed.BondId(symbol), symbol, TreasuryBond.Issue(tenor, date, yieldOn(date, tenor))));

                var maturity = date.AddMonths(tenor.Months);
                issueDate = marketDays.Where(day => day >= maturity).Select(day => (DateOnly?)day).FirstOrDefault();
            }
        }

        return bonds;
    }
}
