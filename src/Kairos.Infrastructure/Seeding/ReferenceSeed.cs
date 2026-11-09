using System.Security.Cryptography;
using System.Text;
using Kairos.Domain.Instruments;
using Kairos.Domain.Reference;

namespace Kairos.Infrastructure.Seeding;

/// <summary>The static data of the desk. Traders and counterparties are fictional.</summary>
internal static class ReferenceSeed
{
    /// <summary>The equity universe and its sectors, as chosen in ADR-006.</summary>
    public static readonly IReadOnlyDictionary<string, string> EquitySectors = new Dictionary<string, string>
    {
        ["MSFT"] = "Technology", ["ORCL"] = "Technology", ["CRM"] = "Technology", ["CSCO"] = "Technology", ["MSI"] = "Technology",
        ["DIS"] = "Communication Services", ["T"] = "Communication Services", ["EA"] = "Communication Services", ["CMCSA"] = "Communication Services",
        ["MCD"] = "Consumer Cyclical", ["SBUX"] = "Consumer Cyclical", ["TJX"] = "Consumer Cyclical", ["DHI"] = "Consumer Cyclical", ["CCL"] = "Consumer Cyclical",
        ["PEP"] = "Consumer Defensive", ["PM"] = "Consumer Defensive", ["STZ"] = "Consumer Defensive", ["GIS"] = "Consumer Defensive",
        ["CVX"] = "Energy", ["VLO"] = "Energy", ["WMB"] = "Energy",
        ["V"] = "Financial Services", ["PGR"] = "Financial Services", ["ICE"] = "Financial Services", ["CME"] = "Financial Services", ["AJG"] = "Financial Services",
        ["MRK"] = "Healthcare", ["ABT"] = "Healthcare", ["BSX"] = "Healthcare", ["VRTX"] = "Healthcare",
        ["CAT"] = "Industrials", ["DE"] = "Industrials", ["UPS"] = "Industrials", ["FDX"] = "Industrials",
        ["EQIX"] = "Real Estate", ["O"] = "Real Estate",
        ["SO"] = "Utilities", ["ED"] = "Utilities",
        ["FCX"] = "Basic Materials", ["PPG"] = "Basic Materials",
    };

    public static readonly Book CoreEquities = NewBook("Actions US Cœur");
    public static readonly Book SectorEquities = NewBook("Actions Sectorielles");
    public static readonly Book Fx = NewBook("Change EUR/USD");
    public static readonly Book Rates = NewBook("Taux Souverains US");
    public static readonly Book CrossAsset = NewBook("Cross-Asset");

    public static IReadOnlyList<Book> Books { get; } = [CoreEquities, SectorEquities, Fx, Rates, CrossAsset];

    public static IReadOnlyList<Trader> Traders { get; } =
        new[] { "Inès Moreau", "Hugo Lefèvre", "Clara Nguyen", "Mathis Girard", "Sofia Bernard", "Noé Fontaine" }
            .Select(name => new Trader(IdOf($"trader:{name}"), name))
            .ToList();

    public static IReadOnlyList<Counterparty> Counterparties { get; } =
        new[] { "Cpty Orion", "Cpty Vega", "Cpty Lyra", "Cpty Altaïr", "Cpty Sirius", "Cpty Deneb", "Cpty Rigel", "Cpty Antarès" }
            .Select(name => new Counterparty(IdOf($"counterparty:{name}"), name))
            .ToList();

    public static Guid EurUsdId { get; } = IdOf("instrument:EUR/USD");

    public static Guid EquityId(string ticker) => IdOf($"instrument:{ticker}");

    public static Guid BondId(string symbol) => IdOf($"instrument:{symbol}");

    public static IEnumerable<Instrument> Equities() =>
        EquitySectors.Select(item => Instrument.Equity(EquityId(item.Key), item.Key, item.Value));

    public static Instrument EurUsd() => Instrument.EurUsd(EurUsdId);

    /// <summary>The same name always gives the same id, so that every rebuild of the database lines up.</summary>
    public static Guid IdOf(string name) => new(MD5.HashData(Encoding.UTF8.GetBytes(name)));

    private static Book NewBook(string name) => new(IdOf($"book:{name}"), name);
}
