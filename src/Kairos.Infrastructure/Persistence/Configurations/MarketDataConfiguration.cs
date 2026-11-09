using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairos.Infrastructure.Persistence.Configurations;

internal sealed class EquityDailyBarConfiguration : IEntityTypeConfiguration<EquityDailyBarRow>
{
    public void Configure(EntityTypeBuilder<EquityDailyBarRow> builder)
    {
        builder.ToTable("EquityDailyBars");
        builder.HasKey(row => new { row.Date, row.InstrumentId });
        builder.HasIndex(row => new { row.InstrumentId, row.Date });
        builder.Property(row => row.Open).HasPrecision(18, 6);
        builder.Property(row => row.High).HasPrecision(18, 6);
        builder.Property(row => row.Low).HasPrecision(18, 6);
        builder.Property(row => row.Close).HasPrecision(18, 6);
    }
}

internal sealed class EurUsdDailyBarConfiguration : IEntityTypeConfiguration<EurUsdDailyBarRow>
{
    public void Configure(EntityTypeBuilder<EurUsdDailyBarRow> builder)
    {
        builder.ToTable("EurUsdDailyBars");
        builder.HasKey(row => row.Date);
        builder.Property(row => row.Open).HasPrecision(9, 5);
        builder.Property(row => row.High).HasPrecision(9, 5);
        builder.Property(row => row.Low).HasPrecision(9, 5);
        builder.Property(row => row.Close).HasPrecision(9, 5);
        builder.Property(row => row.CloseBid).HasPrecision(9, 5);
        builder.Property(row => row.CloseAsk).HasPrecision(9, 5);
    }
}

internal sealed class EurUsdMinuteBarConfiguration : IEntityTypeConfiguration<EurUsdMinuteBarRow>
{
    public void Configure(EntityTypeBuilder<EurUsdMinuteBarRow> builder)
    {
        builder.ToTable("EurUsdMinuteBars");
        builder.HasKey(row => new { row.Date, row.Minute });
        builder.Property(row => row.Bid).HasPrecision(9, 5);
        builder.Property(row => row.Ask).HasPrecision(9, 5);
    }
}

internal sealed class CurvePointConfiguration : IEntityTypeConfiguration<CurvePointRow>
{
    public void Configure(EntityTypeBuilder<CurvePointRow> builder)
    {
        builder.ToTable("CurvePoints");
        builder.HasKey(row => new { row.Date, row.Months });
        builder.Property(row => row.Yield).HasPrecision(9, 6);
    }
}
