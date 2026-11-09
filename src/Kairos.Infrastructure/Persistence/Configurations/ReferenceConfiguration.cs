using Kairos.Domain.Instruments;
using Kairos.Domain.Reference;
using Kairos.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairos.Infrastructure.Persistence.Configurations;

internal sealed class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.ToTable("Instruments");
        builder.HasKey(instrument => instrument.Id);
        builder.Property(instrument => instrument.Id).ValueGeneratedNever();
        builder.Property(instrument => instrument.Symbol).HasMaxLength(32);
        builder.HasIndex(instrument => instrument.Symbol).IsUnique();
        builder.Property(instrument => instrument.AssetClass).HasConversion<string>().HasMaxLength(16);
        builder.Property(instrument => instrument.Sector).HasMaxLength(64);

        builder.OwnsOne(instrument => instrument.Bond, bond =>
        {
            bond.Property(item => item.OriginalTenor)
                .HasColumnName("BondTenorMonths")
                .HasConversion(tenor => tenor.Months, months => Tenor.FromMonths(months));
            bond.Property(item => item.IssueDate).HasColumnName("BondIssueDate");
            bond.Property(item => item.CouponRate).HasColumnName("BondCouponRate").HasPrecision(9, 6);
        });
    }
}

internal sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");
        builder.HasKey(book => book.Id);
        builder.Property(book => book.Id).ValueGeneratedNever();
        builder.Property(book => book.Name).HasMaxLength(64);
    }
}

internal sealed class TraderConfiguration : IEntityTypeConfiguration<Trader>
{
    public void Configure(EntityTypeBuilder<Trader> builder)
    {
        builder.ToTable("Traders");
        builder.HasKey(trader => trader.Id);
        builder.Property(trader => trader.Id).ValueGeneratedNever();
        builder.Property(trader => trader.Name).HasMaxLength(64);
    }
}

internal sealed class CounterpartyConfiguration : IEntityTypeConfiguration<Counterparty>
{
    public void Configure(EntityTypeBuilder<Counterparty> builder)
    {
        builder.ToTable("Counterparties");
        builder.HasKey(counterparty => counterparty.Id);
        builder.Property(counterparty => counterparty.Id).ValueGeneratedNever();
        builder.Property(counterparty => counterparty.Name).HasMaxLength(64);
    }
}
