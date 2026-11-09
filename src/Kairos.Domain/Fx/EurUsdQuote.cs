using Kairos.Domain.Common;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Fx;

/// <summary>An EUR/USD market quote, expressed in US dollars for one euro.</summary>
public sealed record EurUsdQuote
{
    public EurUsdQuote(decimal bid, decimal ask)
    {
        if (bid <= 0m)
        {
            throw new DomainException("The bid must be strictly positive.");
        }

        if (ask < bid)
        {
            throw new DomainException("The ask cannot be below the bid.");
        }

        Bid = bid;
        Ask = ask;
    }

    public decimal Bid { get; }

    public decimal Ask { get; }

    public decimal Mid => (Bid + Ask) / 2m;

    /// <summary>A buyer of euros pays the ask, a seller of euros receives the bid.</summary>
    public decimal ExecutionPrice(Side side) => side == Side.Buy ? Ask : Bid;

    /// <summary>The price a position could be closed at now: a long sells at the bid, a short buys at the ask.</summary>
    public decimal ClosingPrice(decimal positionQuantity) =>
        ExecutionPrice(positionQuantity > 0m ? Side.Sell : Side.Buy);
}
