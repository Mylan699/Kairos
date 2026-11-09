namespace Kairos.Domain.ValueObjects;

public enum AssetClass
{
    Equity,
    Fx,
    Bond,
}

public static class AssetClassExtensions
{
    private const decimal BondQuotedPer = 100m;

    /// <summary>Converts quantity times price into an amount: bonds are quoted per 100 of nominal.</summary>
    public static decimal ContractMultiplier(this AssetClass assetClass) =>
        assetClass == AssetClass.Bond ? 1m / BondQuotedPer : 1m;

    /// <summary>
    /// True when buying the instrument means converting euros into dollars, so the
    /// EUR/USD rate moves the euro value of the capital invested. An EUR/USD position
    /// is financed in dollars and invests no euro capital.
    /// </summary>
    public static bool HasCurrencyEffect(this AssetClass assetClass) => assetClass != AssetClass.Fx;
}
