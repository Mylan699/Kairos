using Kairos.Domain.Instruments;
using Kairos.Domain.Reference;
using Kairos.Domain.Trades;
using Microsoft.EntityFrameworkCore;

namespace Kairos.Infrastructure.Persistence;

public sealed class KairosDbContext(DbContextOptions<KairosDbContext> options) : DbContext(options)
{
    public DbSet<Trade> Trades => Set<Trade>();

    public DbSet<Instrument> Instruments => Set<Instrument>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<Trader> Traders => Set<Trader>();

    public DbSet<Counterparty> Counterparties => Set<Counterparty>();

    public DbSet<EquityDailyBarRow> EquityDailyBars => Set<EquityDailyBarRow>();

    public DbSet<EurUsdDailyBarRow> EurUsdDailyBars => Set<EurUsdDailyBarRow>();

    public DbSet<EurUsdMinuteBarRow> EurUsdMinuteBars => Set<EurUsdMinuteBarRow>();

    public DbSet<CurvePointRow> CurvePoints => Set<CurvePointRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KairosDbContext).Assembly);
}
