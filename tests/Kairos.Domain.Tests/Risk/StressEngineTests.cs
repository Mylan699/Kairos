using Kairos.Domain.Common;
using Kairos.Domain.Rates;
using Kairos.Domain.Risk;
using Kairos.Domain.ValueObjects;
using static Kairos.Domain.Tests.Risk.RiskFixture;

namespace Kairos.Domain.Tests.Risk;

public class StressEngineTests
{
    private static readonly StressScenario EquitiesDown = StressScenario.Parametric("Equities -10%", -0.10m, 0m, 0m);
    private static readonly StressScenario EurUsdDown = StressScenario.Parametric("EUR/USD -5%", 0m, -0.05m, 0m);

    [Fact]
    public void An_equity_shock_moves_the_market_value_at_the_current_rate()
    {
        BookPosition[] positions = [Held(BookA, Tech, Side.Buy, 100m, 100m)];

        var result = StressEngine.Run(positions, SampleMarket(), EquitiesDown);

        Assert.Equal(-800m, result.Total.Amount, 8);
    }

    [Fact]
    public void A_short_equity_position_gains_from_an_equity_fall()
    {
        BookPosition[] positions = [Held(BookA, Tech, Side.Sell, 100m, 100m)];

        var result = StressEngine.Run(positions, SampleMarket(), EquitiesDown);

        Assert.Equal(800m, result.Total.Amount, 8);
    }

    [Fact]
    public void A_weaker_euro_lifts_the_euro_value_of_dollar_assets()
    {
        BookPosition[] positions = [Held(BookA, Tech, Side.Buy, 100m, 100m)];

        var result = StressEngine.Run(positions, SampleMarket(), EurUsdDown);

        Assert.Equal(421.05m, result.Total.Amount, 2);
    }

    [Fact]
    public void A_long_eur_usd_position_loses_when_the_euro_falls()
    {
        BookPosition[] positions = [Held(BookA, EurUsd, Side.Buy, 1_000_000m, 1.25m)];

        var result = StressEngine.Run(positions, SampleMarket(), EurUsdDown);

        Assert.Equal(-52_631.58m, result.Total.Amount, 2);
    }

    [Fact]
    public void A_rate_shock_revalues_the_bond_on_the_shifted_curve()
    {
        BookPosition[] positions = [Held(BookA, TenYear, Side.Buy, 1_000_000m, 100m)];
        var scenario = StressScenario.Parametric("Rates +100bp", 0m, 0m, 100m);

        var result = StressEngine.Run(positions, SampleMarket(), scenario);

        var before = BondPricer.Value(TenYear.Bond!, MarketDate, FlatCurve(0.03m)).CleanPrice;
        var after = BondPricer.Value(TenYear.Bond!, MarketDate, FlatCurve(0.04m)).CleanPrice;
        Assert.Equal((after - before) * 10_000m / Rate, result.Total.Amount, 6);
        Assert.True(result.Total.Amount < 0m);
    }

    [Fact]
    public void Combined_shocks_compound_rather_than_simply_add()
    {
        BookPosition[] positions = [Held(BookA, Tech, Side.Buy, 100m, 100m)];
        var both = StressScenario.Parametric("Combined", -0.10m, -0.05m, 0m);

        var combined = StressEngine.Run(positions, SampleMarket(), both).Total.Amount;
        var separate = StressEngine.Run(positions, SampleMarket(), EquitiesDown).Total.Amount
            + StressEngine.Run(positions, SampleMarket(), EurUsdDown).Total.Amount;

        Assert.Equal(-421.05m, combined, 2);
        Assert.NotEqual(separate, combined);
    }

    [Fact]
    public void Results_are_split_by_book_and_by_asset_class_and_add_up()
    {
        BookPosition[] positions =
        [
            Held(BookA, Tech, Side.Buy, 100m, 100m),
            Held(BookA, EurUsd, Side.Buy, 1_000m, 1.25m),
            Held(BookB, Energy, Side.Buy, 200m, 50m),
        ];
        var scenario = StressScenario.Parametric("Combined", -0.10m, -0.05m, 0m);

        var result = StressEngine.Run(positions, SampleMarket(), scenario);

        Assert.Equal(result.Total.Amount, result.ByBook.Values.Sum(money => money.Amount), 8);
        Assert.Equal(result.Total.Amount, result.ByAssetClass.Values.Sum(money => money.Amount), 8);
        Assert.Equal(2, result.ByBook.Count);
        Assert.True(result.ByAssetClass.ContainsKey(AssetClass.Fx));
    }

    [Fact]
    public void A_historical_scenario_turns_the_start_market_into_the_end_market()
    {
        var start = SampleMarket(techPrice: 100m, energyPrice: 50m, rate: 1.25m, yield: 0.03m);
        var end = SampleMarket(techPrice: 80m, energyPrice: 55m, rate: 1.20m, yield: 0.02m);

        var replayed = start.Apply(StressScenario.Historical("Replay", start, end));

        Assert.Equal(80m, replayed.EquityPrices[Tech.Id], 8);
        Assert.Equal(55m, replayed.EquityPrices[Energy.Id], 8);
        Assert.Equal(1.20m, replayed.EurUsd.Mid, 8);
        Assert.Equal(0.02m, replayed.Curve.YieldAt(10m), 8);
    }

    [Fact]
    public void A_historical_scenario_keeps_each_instrument_on_its_own_move()
    {
        var start = SampleMarket(techPrice: 100m, energyPrice: 50m);
        var end = SampleMarket(techPrice: 80m, energyPrice: 55m);
        BookPosition[] positions = [Held(BookA, Tech, Side.Buy, 100m, 100m), Held(BookA, Energy, Side.Buy, 100m, 50m)];

        var result = StressEngine.Run(positions, start, StressScenario.Historical("Replay", start, end));

        Assert.Equal((-2_000m + 500m) / Rate, result.Total.Amount, 8);
    }

    [Fact]
    public void No_positions_means_no_impact()
    {
        var result = StressEngine.Run([], SampleMarket(), EquitiesDown);

        Assert.Equal(0m, result.Total.Amount);
        Assert.Empty(result.ByBook);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1.5)]
    public void A_shock_wiping_out_a_market_is_rejected(double equity, double eurUsd)
    {
        Assert.Throws<DomainException>(() => StressScenario.Parametric("Bad", (decimal)equity, (decimal)eurUsd, 0m));
    }
}
