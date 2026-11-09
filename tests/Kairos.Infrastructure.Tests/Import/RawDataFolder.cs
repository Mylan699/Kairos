namespace Kairos.Infrastructure.Tests.Import;

/// <summary>A temporary folder laid out like data/raw, deleted at the end of the test.</summary>
internal sealed class RawDataFolder : IDisposable
{
    public RawDataFolder()
    {
        Root = Path.Combine(Path.GetTempPath(), "kairos-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(Root, "sp500"));
        Directory.CreateDirectory(Path.Combine(Root, "eurusd"));
        Directory.CreateDirectory(Path.Combine(Root, "treasury"));
    }

    public string Root { get; }

    public string StocksPath => Path.Combine(Root, "sp500", "sp500_stocks.csv");

    public string EurUsdPath => Path.Combine(Root, "eurusd", "eurusd_minute.csv");

    public string TreasuryPath => Path.Combine(Root, "treasury", "us_treasury_yields_daily.csv");

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
