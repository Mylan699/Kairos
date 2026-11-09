using Kairos.Domain.ValueObjects;
using Kairos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kairos.Infrastructure.Seeding;

/// <summary>Writes the static data. Bonds need the imported curves, for their coupons.</summary>
internal sealed class ReferenceSeeder(KairosDbContext db)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await db.Instruments.AnyAsync(cancellationToken))
        {
            return;
        }

        var marketDays = await db.EurUsdDailyBars.OrderBy(row => row.Date).Select(row => row.Date).ToListAsync(cancellationToken);
        var issuedMonths = new[] { Tenor.TwoYears.Months, Tenor.FiveYears.Months, Tenor.TenYears.Months, Tenor.ThirtyYears.Months };

        var yields = await db.CurvePoints.AsNoTracking()
            .Where(point => issuedMonths.Contains(point.Months))
            .ToDictionaryAsync(point => (point.Date, point.Months), point => point.Yield, cancellationToken);

        db.Instruments.AddRange(ReferenceSeed.Equities());
        db.Instruments.Add(ReferenceSeed.EurUsd());
        db.Instruments.AddRange(BondSchedule.Issue(marketDays, (date, tenor) => yields[(date, tenor.Months)]));
        db.Books.AddRange(ReferenceSeed.Books);
        db.Traders.AddRange(ReferenceSeed.Traders);
        db.Counterparties.AddRange(ReferenceSeed.Counterparties);

        await db.SaveChangesAsync(cancellationToken);
    }
}
