using Kairos.Domain.Common;
using Kairos.Domain.Instruments;
using Kairos.Domain.Risk;
using Kairos.Domain.ValueObjects;
using static Kairos.Domain.Tests.Risk.RiskFixture;

namespace Kairos.Domain.Tests.Risk;

public class RiskCalculatorTests
{
    [Fact]
    public void Exposures_are_grouped_by_asset_class_and_sector()
    {
        BookPosition[] positions =
        [
            Held(BookA, Tech, Side.Buy, 100m, 100m),
            Held(BookB, Tech, Side.Sell, 40m, 100m),
            Held(BookA, Energy, Side.Buy, 100m, 50m),
        ];

        var exposures = RiskCalculator.Exposures(positions, SampleMarket());

        Assert.Equal(new Money(8_800m, Currency.EUR), exposures.ByAssetClass[AssetClass.Equity]);
        Assert.Equal(new Money(4_800m, Currency.EUR), exposures.BySector["Technology"]);
        Assert.Equal(new Money(4_000m, Currency.EUR), exposures.BySector["Energy"]);
    }

    [Fact]
    public void A_long_eur_usd_position_is_long_euro_and_short_dollar()
    {
        BookPosition[] positions = [Held(BookA, EurUsd, Side.Buy, 1_000m, 1.25m)];

        var exposures = RiskCalculator.Exposures(positions, SampleMarket());

        Assert.Equal(new Money(1_000m, Currency.EUR), exposures.ByCurrency[Currency.EUR]);
        Assert.Equal(new Money(-1_000m, Currency.EUR), exposures.ByCurrency[Currency.USD]);
    }

    [Fact]
    public void Dollar_assets_count_as_dollar_exposure()
    {
        BookPosition[] positions = [Held(BookA, Tech, Side.Buy, 100m, 100m)];

        var exposures = RiskCalculator.Exposures(positions, SampleMarket());

        Assert.Equal(new Money(8_000m, Currency.EUR), exposures.ByCurrency[Currency.USD]);
        Assert.False(exposures.ByCurrency.ContainsKey(Currency.EUR));
    }

    [Fact]
    public void Only_equities_appear_in_the_sector_breakdown()
    {
        BookPosition[] positions = [Held(BookA, TenYear, Side.Buy, 1_000m, 100m), Held(BookA, EurUsd, Side.Buy, 1_000m, 1.25m)];

        Assert.Empty(RiskCalculator.Exposures(positions, SampleMarket()).BySector);
    }

    [Fact]
    public void Equity_delta_is_one_percent_of_the_equity_market_value()
    {
        BookPosition[] positions = [Held(BookA, Tech, Side.Buy, 100m, 100m)];

        var sensitivities = RiskCalculator.Sensitivities(positions, SampleMarket());

        Assert.Equal(80m, sensitivities.EquityDelta.Amount, 8);
    }

    [Fact]
    public void Fx_delta_is_negative_for_dollar_assets_and_positive_for_long_eur_usd()
    {
        var dollarAssets = RiskCalculator.Sensitivities([Held(BookA, Tech, Side.Buy, 100m, 100m)], SampleMarket());
        var longEurUsd = RiskCalculator.Sensitivities([Held(BookA, EurUsd, Side.Buy, 1_000m, 1.25m)], SampleMarket());

        Assert.True(dollarAssets.FxDelta.Amount < 0m);
        Assert.True(longEurUsd.FxDelta.Amount > 0m);
    }

    [Fact]
    public void Total_dv01_matches_the_loss_from_a_one_basis_point_rise()
    {
        BookPosition[] positions = [Held(BookA, TenYear, Side.Buy, 1_000_000m, 100m)];
        var onePointUp = StressScenario.Parametric("+1bp", 0m, 0m, 1m);

        var dv01 = RiskCalculator.Sensitivities(positions, SampleMarket()).Dv01.Amount;
        var loss = -StressEngine.Run(positions, SampleMarket(), onePointUp).Total.Amount;

        Assert.True(dv01 > 0m);
        Assert.Equal(dv01, loss, 0);
    }

    [Fact]
    public void A_short_bond_position_has_a_negative_dv01()
    {
        var sensitivities = RiskCalculator.Sensitivities([Held(BookA, TenYear, Side.Sell, 1_000_000m, 100m)], SampleMarket());

        Assert.True(sensitivities.Dv01.Amount < 0m);
    }

    [Fact]
    public void An_equity_with_no_market_price_is_reported_clearly()
    {
        var unknown = Instrument.Equity(Guid.NewGuid(), "ZZZ", "Energy");

        Assert.Throws<DomainException>(() =>
            RiskCalculator.Exposures([Held(BookA, unknown, Side.Buy, 1m, 1m)], SampleMarket()));
    }
}
