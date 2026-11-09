using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Reference;

/// <summary>A portfolio of trades. Every book reports its results in euros.</summary>
public sealed record Book(Guid Id, string Name)
{
    public Currency ReportingCurrency => Currency.EUR;
}

public sealed record Trader(Guid Id, string Name);

public sealed record Counterparty(Guid Id, string Name);
