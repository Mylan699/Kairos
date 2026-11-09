using Kairos.Domain.Common;
using Kairos.Domain.Fx;
using Kairos.Domain.Positions;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.Positions;

public class PositionTests
{
    private const decimal Rate = 1.25m;

    private static Position Equity(params Fill[] fills) => Position.FromFills(AssetClass.Equity, fills);

    private static Money Eur(decimal amount) => new(amount, Currency.EUR);

    [Fact]
    public void Buying_twice_averages_the_cost()
    {
        var position = Equity(new Fill(Side.Buy, 100m, 10m, Rate), new Fill(Side.Buy, 300m, 20m, Rate));

        Assert.Equal(400m, position.Quantity);
        Assert.Equal(17.5m, position.AveragePrice);
    }

    [Fact]
    public void A_partial_sale_realizes_the_gain_and_keeps_the_average_cost()
    {
        var position = Equity(new Fill(Side.Buy, 100m, 10m, Rate), new Fill(Side.Sell, 40m, 15m, Rate));

        Assert.Equal(60m, position.Quantity);
        Assert.Equal(10m, position.AveragePrice);
        Assert.Equal(Eur(160m), position.Realized);
    }

    [Fact]
    public void A_short_position_gains_when_it_buys_back_lower()
    {
        var position = Equity(new Fill(Side.Sell, 100m, 20m, Rate), new Fill(Side.Buy, 100m, 15m, Rate));

        Assert.Equal(0m, position.Quantity);
        Assert.Equal(Eur(400m), position.Realized);
    }

    [Fact]
    public void Adding_to_a_short_position_averages_its_entry_price()
    {
        var position = Equity(new Fill(Side.Sell, 100m, 20m, Rate), new Fill(Side.Sell, 100m, 30m, Rate));

        Assert.Equal(-200m, position.Quantity);
        Assert.Equal(25m, position.AveragePrice);
        Assert.Equal(Eur(0m), position.Realized);
    }

    [Fact]
    public void Closing_completely_clears_the_average_cost()
    {
        var position = Equity(new Fill(Side.Buy, 100m, 10m, Rate), new Fill(Side.Sell, 100m, 12m, Rate));

        Assert.Equal(0m, position.AveragePrice);
        Assert.Equal(0m, position.AverageCostInEur);
    }

    [Fact]
    public void A_sale_larger_than_the_position_reverses_it_at_the_sale_price()
    {
        var position = Equity(new Fill(Side.Buy, 100m, 10m, Rate), new Fill(Side.Sell, 150m, 12m, Rate));

        Assert.Equal(-50m, position.Quantity);
        Assert.Equal(12m, position.AveragePrice);
        Assert.Equal(9.6m, position.AverageCostInEur);
        Assert.Equal(Eur(160m), position.Realized);
    }

    [Fact]
    public void A_rate_move_alone_shows_up_as_realized_currency_effect()
    {
        // Bought for 800 euros (1000 USD at 1.25), sold for 1000 euros (1000 USD at 1.00).
        var position = Equity(new Fill(Side.Buy, 100m, 10m, 1.25m), new Fill(Side.Sell, 100m, 10m, 1m));

        Assert.Equal(Eur(0m), position.RealizedPriceEffect);
        Assert.Equal(Eur(200m), position.RealizedFxEffect);
    }

    [Fact]
    public void Unrealized_pnl_splits_into_price_and_currency_effects_that_add_up()
    {
        // Cost 800 EUR. Now worth 1200 USD at 1.60, so 750 EUR: a 50 EUR loss overall.
        var position = Equity(new Fill(Side.Buy, 100m, 10m, 1.25m));

        var valuation = position.Value(markPrice: 12m, usdPerEur: 1.6m);

        Assert.Equal(Eur(750m), valuation.MarketValue);
        Assert.Equal(Eur(125m), valuation.PriceEffect);
        Assert.Equal(Eur(-175m), valuation.FxEffect);
        Assert.Equal(Eur(-50m), valuation.Unrealized);
    }

    [Fact]
    public void A_short_position_has_a_negative_market_value()
    {
        var valuation = Equity(new Fill(Side.Sell, 100m, 10m, Rate)).Value(markPrice: 10m, usdPerEur: Rate);

        Assert.Equal(Eur(-800m), valuation.MarketValue);
        Assert.Equal(Eur(0m), valuation.Unrealized);
    }

    [Fact]
    public void A_flat_position_has_no_value_and_no_unrealized_pnl()
    {
        var valuation = Position.Flat(AssetClass.Equity).Value(markPrice: 10m, usdPerEur: Rate);

        Assert.Equal(Eur(0m), valuation.MarketValue);
        Assert.Equal(Eur(0m), valuation.Unrealized);
    }

    [Fact]
    public void Bond_prices_apply_per_100_of_nominal()
    {
        var position = Position.FromFills(AssetClass.Bond, [new Fill(Side.Buy, 1_000_000m, 98m, Rate)]);

        var valuation = position.Value(markPrice: 99m, usdPerEur: Rate);

        Assert.Equal(Eur(792_000m), valuation.MarketValue);
        Assert.Equal(Eur(8_000m), valuation.PriceEffect);
        Assert.Equal(Eur(0m), valuation.FxEffect);
    }

    [Fact]
    public void An_eur_usd_position_earns_the_rate_difference_with_no_currency_effect()
    {
        // Long 1,000,000 EUR at 1.10 with the bid now at 1.20: +100,000 USD, worth 83,333.33 EUR.
        var position = Position.FromFills(AssetClass.Fx, [new Fill(Side.Buy, 1_000_000m, 1.10m, 1.10m)]);

        var valuation = position.Value(markPrice: 1.20m, usdPerEur: 1.20m);

        Assert.Equal(83_333.33m, valuation.PriceEffect.Amount, 2);
        Assert.Equal(Eur(0m), valuation.FxEffect);
    }

    [Fact]
    public void An_eur_usd_position_marked_at_the_closing_side_shows_the_spread_as_a_loss()
    {
        var quote = new EurUsdQuote(1.1000m, 1.1002m);
        var position = Position.FromFills(
            AssetClass.Fx,
            [new Fill(Side.Buy, 1_000_000m, quote.ExecutionPrice(Side.Buy), quote.Mid)]);

        var valuation = position.Value(quote.ClosingPrice(position.Quantity), quote.Mid);

        Assert.True(valuation.Unrealized.Amount < 0m);
        Assert.Equal(-181.8m, valuation.Unrealized.Amount, 1);
    }

    [Fact]
    public void A_round_trip_realizes_the_same_gain_in_either_order()
    {
        var buyFirst = Equity(new Fill(Side.Buy, 100m, 10m, Rate), new Fill(Side.Sell, 100m, 15m, Rate));
        var sellFirst = Equity(new Fill(Side.Sell, 100m, 15m, Rate), new Fill(Side.Buy, 100m, 10m, Rate));

        Assert.Equal(buyFirst.Realized, sellFirst.Realized);
        Assert.Equal(buyFirst.Quantity, sellFirst.Quantity);
    }

    [Theory]
    [InlineData(0, 10, 1.25)]
    [InlineData(-5, 10, 1.25)]
    [InlineData(5, 0, 1.25)]
    [InlineData(5, 10, 0)]
    public void An_invalid_fill_is_rejected(double quantity, double price, double rate)
    {
        Assert.Throws<DomainException>(() => new Fill(Side.Buy, (decimal)quantity, (decimal)price, (decimal)rate));
    }
}
