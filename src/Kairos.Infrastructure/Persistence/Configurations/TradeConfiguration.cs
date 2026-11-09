using Kairos.Domain.Trades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairos.Infrastructure.Persistence.Configurations;

internal sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    private const int EnumLength = 16;
    private const int NameLength = 64;

    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("Trades");
        builder.HasKey(trade => trade.Id);
        builder.Property(trade => trade.Id).ValueGeneratedNever();
        builder.Property(trade => trade.Status).HasConversion<string>().HasMaxLength(EnumLength);

        builder.OwnsMany(trade => trade.Versions, version =>
        {
            version.ToTable("TradeVersions");
            version.WithOwner().HasForeignKey("TradeId");
            version.HasKey("TradeId", nameof(TradeVersion.Number));
            version.Property(item => item.Number).ValueGeneratedNever();
            version.Property(item => item.CreatedBy).HasMaxLength(NameLength);
            version.OwnsOne(item => item.Terms, ConfigureTerms);
        });

        builder.OwnsMany(trade => trade.AuditTrail, entry =>
        {
            entry.ToTable("TradeAuditEntries");
            entry.WithOwner().HasForeignKey("TradeId");
            entry.HasKey("TradeId", nameof(AuditEntry.Sequence));
            entry.Property(item => item.Sequence).ValueGeneratedNever();
            entry.Property(item => item.Actor).HasMaxLength(NameLength);
            entry.Property(item => item.Action).HasConversion<string>().HasMaxLength(EnumLength);
            entry.Property(item => item.ResultingStatus).HasConversion<string>().HasMaxLength(EnumLength);
            entry.Property(item => item.Details).HasMaxLength(1024);
        });

        builder.Navigation(trade => trade.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(trade => trade.AuditTrail).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureTerms(OwnedNavigationBuilder<TradeVersion, TradeTerms> terms)
    {
        terms.Property(item => item.InstrumentId).HasColumnName("InstrumentId");
        terms.Property(item => item.Side).HasColumnName("Side").HasConversion<string>().HasMaxLength(EnumLength);
        terms.Property(item => item.Quantity).HasColumnName("Quantity").HasPrecision(20, 4);
        terms.Property(item => item.Price).HasColumnName("Price").HasPrecision(18, 6);
        terms.Property(item => item.CounterpartyId).HasColumnName("CounterpartyId");
        terms.Property(item => item.BookId).HasColumnName("BookId");
        terms.Property(item => item.TraderId).HasColumnName("TraderId");
        terms.Property(item => item.TradeDate).HasColumnName("TradeDate");
        terms.Property(item => item.ValueDate).HasColumnName("ValueDate");
        terms.Property(item => item.UsdPerEurAtTrade).HasColumnName("UsdPerEurAtTrade").HasPrecision(18, 8);
    }
}
