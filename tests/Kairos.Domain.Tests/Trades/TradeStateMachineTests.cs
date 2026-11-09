using Kairos.Domain.Common;
using Kairos.Domain.Trades;

namespace Kairos.Domain.Tests.Trades;

public class TradeStateMachineTests
{
    [Theory]
    [InlineData(TradeStatus.New, TradeStatus.New, false)]
    [InlineData(TradeStatus.New, TradeStatus.Validated, true)]
    [InlineData(TradeStatus.New, TradeStatus.Settled, false)]
    [InlineData(TradeStatus.New, TradeStatus.Cancelled, true)]
    [InlineData(TradeStatus.Validated, TradeStatus.New, true)]
    [InlineData(TradeStatus.Validated, TradeStatus.Validated, false)]
    [InlineData(TradeStatus.Validated, TradeStatus.Settled, true)]
    [InlineData(TradeStatus.Validated, TradeStatus.Cancelled, true)]
    [InlineData(TradeStatus.Settled, TradeStatus.New, false)]
    [InlineData(TradeStatus.Settled, TradeStatus.Validated, false)]
    [InlineData(TradeStatus.Settled, TradeStatus.Settled, false)]
    [InlineData(TradeStatus.Settled, TradeStatus.Cancelled, false)]
    [InlineData(TradeStatus.Cancelled, TradeStatus.New, false)]
    [InlineData(TradeStatus.Cancelled, TradeStatus.Validated, false)]
    [InlineData(TradeStatus.Cancelled, TradeStatus.Settled, false)]
    [InlineData(TradeStatus.Cancelled, TradeStatus.Cancelled, false)]
    public void Every_transition_is_either_allowed_or_forbidden(TradeStatus from, TradeStatus to, bool allowed)
    {
        Assert.Equal(allowed, TradeStateMachine.CanMove(from, to));
    }

    [Fact]
    public void A_forbidden_transition_is_rejected()
    {
        Assert.Throws<DomainException>(() =>
            TradeStateMachine.EnsureCanMove(TradeStatus.Settled, TradeStatus.Cancelled));
    }

    [Theory]
    [InlineData(TradeStatus.New, true)]
    [InlineData(TradeStatus.Validated, true)]
    [InlineData(TradeStatus.Settled, false)]
    [InlineData(TradeStatus.Cancelled, false)]
    public void Only_trades_not_yet_final_can_be_amended(TradeStatus status, bool expected)
    {
        Assert.Equal(expected, TradeStateMachine.CanAmend(status));
    }
}
