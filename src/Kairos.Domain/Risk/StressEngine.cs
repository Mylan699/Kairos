using Kairos.Domain.Market;
using Kairos.Domain.Positions;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Risk;

/// <summary>The euro impact of a scenario on the book.</summary>
public sealed record StressResult(
    string ScenarioName,
    Money Total,
    IReadOnlyDictionary<Guid, Money> ByBook,
    IReadOnlyDictionary<AssetClass, Money> ByAssetClass);

/// <summary>
/// Revalues every position in the shocked market and compares it with today. Full
/// revaluation captures what an approximation by sensitivities would miss: bond
/// convexity, and the way equity and currency shocks compound.
/// </summary>
public static class StressEngine
{
    public static StressResult Run(IReadOnlyCollection<BookPosition> positions, MarketSnapshot market, StressScenario scenario)
    {
        var shockedMarket = market.Apply(scenario);
        var impacts = positions.Select(position => (Position: position, Impact: ImpactOf(position, market, shockedMarket))).ToList();

        var byBook = impacts
            .GroupBy(item => item.Position.BookId)
            .ToDictionary(group => group.Key, group => Sum(group.Select(item => item.Impact)));

        var byAssetClass = impacts
            .GroupBy(item => item.Position.Instrument.AssetClass)
            .ToDictionary(group => group.Key, group => Sum(group.Select(item => item.Impact)));

        return new StressResult(scenario.Name, Sum(impacts.Select(item => item.Impact)), byBook, byAssetClass);
    }

    private static Money ImpactOf(BookPosition held, MarketSnapshot market, MarketSnapshot shockedMarket) =>
        UnrealizedIn(held, shockedMarket) - UnrealizedIn(held, market);

    private static Money UnrealizedIn(BookPosition held, MarketSnapshot market)
    {
        var markPrice = market.MarkPrice(held.Instrument, held.Position.Quantity);
        return held.Position.Value(markPrice, market.UsdPerEur).Unrealized;
    }

    private static Money Sum(IEnumerable<Money> amounts) =>
        amounts.Aggregate(Money.Zero(Currency.EUR), (total, amount) => total + amount);
}
