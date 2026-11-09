using Kairos.Domain.Common;
using Kairos.Domain.Fx;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.Fx;

public class EurUsdQuoteTests
{
    [Fact]
    public void Mid_is_halfway_between_bid_and_ask()
    {
        Assert.Equal(1.1001m, new EurUsdQuote(1.1000m, 1.1002m).Mid);
    }

    [Fact]
    public void A_buyer_pays_the_ask_and_a_seller_receives_the_bid()
    {
        var quote = new EurUsdQuote(1.1000m, 1.1002m);

        Assert.Equal(1.1002m, quote.ExecutionPrice(Side.Buy));
        Assert.Equal(1.1000m, quote.ExecutionPrice(Side.Sell));
    }

    [Fact]
    public void A_long_position_closes_at_the_bid_and_a_short_at_the_ask()
    {
        var quote = new EurUsdQuote(1.1000m, 1.1002m);

        Assert.Equal(1.1000m, quote.ClosingPrice(1_000_000m));
        Assert.Equal(1.1002m, quote.ClosingPrice(-1_000_000m));
    }

    [Fact]
    public void A_locked_market_with_equal_bid_and_ask_is_accepted()
    {
        Assert.Equal(1.1m, new EurUsdQuote(1.1m, 1.1m).Mid);
    }

    [Fact]
    public void An_ask_below_the_bid_is_rejected()
    {
        Assert.Throws<DomainException>(() => new EurUsdQuote(1.1002m, 1.1000m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.1)]
    public void A_non_positive_bid_is_rejected(double bid)
    {
        Assert.Throws<DomainException>(() => new EurUsdQuote((decimal)bid, 1.1m));
    }
}
