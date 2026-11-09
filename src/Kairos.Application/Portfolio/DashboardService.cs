using Kairos.Application.Abstractions;
using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Portfolio;

public sealed record ContributorDto(string Symbol, AssetClass AssetClass, decimal DayPnl);

public sealed record AssetClassExposureDto(AssetClass AssetClass, decimal MarketValue, decimal DayPnl);

public sealed record DashboardDto(
    PortfolioSummaryDto Summary,
    IReadOnlyList<AssetClassExposureDto> AssetClasses,
    IReadOnlyList<ContributorDto> TopContributors,
    IReadOnlyList<ContributorDto> BottomContributors,
    IReadOnlyList<PnlHistoryPoint> History);

public sealed class DashboardService(PortfolioService portfolio, IMarketState marketState)
{
    private const int ContributorCount = 5;

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var positions = await portfolio.GetPositionsAsync(cancellationToken);
        var state = await portfolio.GetStateAsync(cancellationToken);

        var byInstrument = positions
            .GroupBy(position => (position.Symbol, position.AssetClass))
            .Select(group => new ContributorDto(group.Key.Symbol, group.Key.AssetClass, group.Sum(position => position.Pnl.Day)))
            .ToList();

        return new DashboardDto(
            PortfolioValuator.Summarize(marketState.Current.Date, positions),
            positions
                .GroupBy(position => position.AssetClass)
                .Select(group => new AssetClassExposureDto(
                    group.Key,
                    group.Sum(position => position.MarketValue),
                    group.Sum(position => position.Pnl.Day)))
                .OrderBy(exposure => exposure.AssetClass)
                .ToList(),
            byInstrument.Where(item => item.DayPnl > 0m).OrderByDescending(item => item.DayPnl).Take(ContributorCount).ToList(),
            byInstrument.Where(item => item.DayPnl < 0m).OrderBy(item => item.DayPnl).Take(ContributorCount).ToList(),
            state.History);
    }
}
