using Kairos.Domain.Common;

namespace Kairos.Domain.Trades;

/// <summary>The only place that says which status a trade may move to.</summary>
public static class TradeStateMachine
{
    private static readonly Dictionary<TradeStatus, TradeStatus[]> AllowedTransitions = new()
    {
        [TradeStatus.New] = [TradeStatus.Validated, TradeStatus.Cancelled],

        // Moving back to New happens when a validated trade is amended: it must be validated again.
        [TradeStatus.Validated] = [TradeStatus.New, TradeStatus.Settled, TradeStatus.Cancelled],

        [TradeStatus.Settled] = [],
        [TradeStatus.Cancelled] = [],
    };

    public static bool CanMove(TradeStatus from, TradeStatus to) => AllowedTransitions[from].Contains(to);

    public static bool CanAmend(TradeStatus status) => status is TradeStatus.New or TradeStatus.Validated;

    public static void EnsureCanMove(TradeStatus from, TradeStatus to)
    {
        if (!CanMove(from, to))
        {
            throw new DomainException($"A trade cannot move from {from} to {to}.");
        }
    }
}
