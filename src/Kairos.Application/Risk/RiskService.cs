using FluentValidation;
using Kairos.Application.Abstractions;
using Kairos.Application.Portfolio;
using Kairos.Domain.Common;
using Kairos.Domain.Risk;
using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Risk;

public sealed class RiskService(
    PortfolioService portfolio,
    IReferenceDataProvider referenceData,
    IMarketState marketState,
    IMarketHistory marketHistory,
    IValidator<ParametricStressRequest> parametricValidator,
    IValidator<HistoricalStressRequest> historicalValidator)
{
    private const decimal PercentToFraction = 0.01m;

    public async Task<RiskReportDto> GetReportAsync(CancellationToken cancellationToken)
    {
        var state = await portfolio.GetStateAsync(cancellationToken);
        var catalog = await referenceData.GetCatalogAsync(cancellationToken);
        var market = marketState.Current;

        var exposures = RiskCalculator.Exposures(state.Positions, market);
        var desk = RiskCalculator.Sensitivities(state.Positions, market);

        var books = state.Positions
            .GroupBy(held => held.BookId)
            .Select(group =>
            {
                var sensitivities = RiskCalculator.Sensitivities(group.ToList(), market);
                return new BookSensitivitiesDto(
                    group.Key,
                    catalog.Book(group.Key).Name,
                    Round(sensitivities.EquityDelta),
                    Round(sensitivities.FxDelta),
                    Round(sensitivities.Dv01));
            })
            .OrderBy(book => book.Book)
            .ToList();

        return new RiskReportDto(
            market.Date,
            ToLines(exposures.ByAssetClass),
            ToLines(exposures.ByCurrency),
            ToLines(exposures.BySector),
            Round(desk.EquityDelta),
            Round(desk.FxDelta),
            Round(desk.Dv01),
            books);
    }

    public async Task<StressResultDto> RunParametricAsync(ParametricStressRequest request, CancellationToken cancellationToken)
    {
        await parametricValidator.ValidateAndThrowAsync(request, cancellationToken);

        var scenario = StressScenario.Parametric(
            "Scénario paramétrable",
            request.EquityShockPercent * PercentToFraction,
            request.EurUsdShockPercent * PercentToFraction,
            request.RateShockBasisPoints);

        return await RunAsync(scenario, cancellationToken);
    }

    /// <summary>Applies to today's book the market moves actually observed between two past dates.</summary>
    public async Task<StressResultDto> RunHistoricalAsync(HistoricalStressRequest request, CancellationToken cancellationToken)
    {
        await historicalValidator.ValidateAndThrowAsync(request, cancellationToken);

        var start = await RequireCloseAsync(request.From, cancellationToken);
        var end = await RequireCloseAsync(request.To, cancellationToken);
        var name = request.Name ?? $"{request.From:yyyy-MM-dd} → {request.To:yyyy-MM-dd}";

        return await RunAsync(StressScenario.Historical(name, start, end), cancellationToken);
    }

    private async Task<Domain.Market.MarketSnapshot> RequireCloseAsync(DateOnly date, CancellationToken cancellationToken) =>
        await marketHistory.GetCloseAsync(date, cancellationToken)
        ?? throw new DomainException($"{date:yyyy-MM-dd} is not a market day.");

    private async Task<StressResultDto> RunAsync(StressScenario scenario, CancellationToken cancellationToken)
    {
        var state = await portfolio.GetStateAsync(cancellationToken);
        var catalog = await referenceData.GetCatalogAsync(cancellationToken);
        var result = StressEngine.Run(state.Positions, marketState.Current, scenario);

        return new StressResultDto(
            result.ScenarioName,
            Round(result.Total),
            result.ByBook
                .Select(line => new StressBookLineDto(line.Key, catalog.Book(line.Key).Name, Round(line.Value)))
                .OrderBy(line => line.Book)
                .ToList(),
            ToLines(result.ByAssetClass));
    }

    private static List<ExposureLineDto> ToLines<TKey>(IReadOnlyDictionary<TKey, Money> amounts)
        where TKey : notnull =>
        amounts
            .Select(amount => new ExposureLineDto(amount.Key.ToString()!, Round(amount.Value)))
            .OrderByDescending(line => Math.Abs(line.Eur))
            .ToList();

    private static decimal Round(Money amount) => amount.Round(2).Amount;
}
