using Kairos.Application.Abstractions;
using Kairos.Application.Common;
using Kairos.Domain.Market;

namespace Kairos.Infrastructure.Replay;

/// <summary>Holds the latest market. A snapshot is immutable, so it is swapped in a single write.</summary>
internal sealed class LiveMarketState : IMarketState
{
    private volatile MarketSnapshot? current;

    public MarketSnapshot Current => current ?? throw new MarketNotReadyException();

    public void Publish(MarketSnapshot snapshot) => current = snapshot;
}
