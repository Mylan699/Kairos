using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Instruments;

/// <summary>Something that can be traded on the desk. All instruments are priced in dollars.</summary>
public sealed class Instrument
{
    private Instrument(Guid id, string symbol, AssetClass assetClass, string? sector, TreasuryBond? bond)
    {
        Id = id;
        Symbol = symbol;
        AssetClass = assetClass;
        Sector = sector;
        Bond = bond;
    }

    public Guid Id { get; }

    public string Symbol { get; }

    public AssetClass AssetClass { get; }

    /// <summary>The sector of an equity. Null for the other asset classes.</summary>
    public string? Sector { get; }

    /// <summary>The terms of a bond. Null for the other asset classes.</summary>
    public TreasuryBond? Bond { get; }

    public static Instrument Equity(Guid id, string ticker, string sector) =>
        new(id, ticker, AssetClass.Equity, sector, bond: null);

    public static Instrument EurUsd(Guid id) =>
        new(id, "EUR/USD", AssetClass.Fx, sector: null, bond: null);

    public static Instrument TreasuryBond(Guid id, string symbol, TreasuryBond bond) =>
        new(id, symbol, AssetClass.Bond, sector: null, bond);
}
