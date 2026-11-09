using Kairos.Application.Portfolio;
using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;
using Kairos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kairos.Infrastructure.Seeding;

/// <summary>Fills the trades table, and restores it to its initial content for the nightly reset.</summary>
public sealed class TradeSeeder(KairosDbContext db, PortfolioCache portfolioCache, ILogger<TradeSeeder> logger)
{
    public async Task SeedIfEmptyAsync(CancellationToken cancellationToken)
    {
        if (!await db.Trades.AnyAsync(cancellationToken))
        {
            await SeedAsync(cancellationToken);
        }
    }

    /// <summary>Drops every trade, including those entered by visitors, and writes the seed again.</summary>
    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await db.Trades.ExecuteDeleteAsync(cancellationToken);
        await SeedAsync(cancellationToken);
        portfolioCache.Invalidate();
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var instruments = await db.Instruments.AsNoTracking().ToListAsync(cancellationToken);
        var days = await LoadMarketDaysAsync(cancellationToken);

        var trades = new SeedTradeGenerator(instruments).Generate(days);

        db.Trades.AddRange(trades);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        logger.LogInformation("Seeded {TradeCount} trades", trades.Count);
    }

    private async Task<IReadOnlyList<SeedMarketDay>> LoadMarketDaysAsync(CancellationToken cancellationToken)
    {
        var dailyBars = await db.EurUsdDailyBars.AsNoTracking().OrderBy(row => row.Date).ToListAsync(cancellationToken);
        var equities = (await db.EquityDailyBars.AsNoTracking().ToListAsync(cancellationToken)).ToLookup(row => row.Date);
        var curves = (await db.CurvePoints.AsNoTracking().ToListAsync(cancellationToken)).ToLookup(row => row.Date);

        return dailyBars
            .Select(day => new SeedMarketDay(
                day.Date,
                equities[day.Date].ToDictionary(row => row.InstrumentId),
                day.CloseBid,
                day.CloseAsk,
                new YieldCurve(curves[day.Date].Select(point => new CurvePoint(Tenor.FromMonths(point.Months), point.Yield)))))
            .ToList();
    }
}
