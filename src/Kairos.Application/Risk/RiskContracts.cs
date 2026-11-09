using FluentValidation;

namespace Kairos.Application.Risk;

public sealed record ExposureLineDto(string Key, decimal Eur);

public sealed record BookSensitivitiesDto(Guid BookId, string Book, decimal EquityDelta, decimal FxDelta, decimal Dv01);

public sealed record RiskReportDto(
    DateOnly MarketDate,
    IReadOnlyList<ExposureLineDto> ByAssetClass,
    IReadOnlyList<ExposureLineDto> ByCurrency,
    IReadOnlyList<ExposureLineDto> BySector,
    decimal EquityDelta,
    decimal FxDelta,
    decimal Dv01,
    IReadOnlyList<BookSensitivitiesDto> Books);

public sealed record StressBookLineDto(Guid BookId, string Book, decimal Eur);

public sealed record StressResultDto(
    string Scenario,
    decimal TotalEur,
    IReadOnlyList<StressBookLineDto> ByBook,
    IReadOnlyList<ExposureLineDto> ByAssetClass);

/// <summary>Shocks entered by the user, in percent and basis points. Each one can stay at zero.</summary>
public sealed record ParametricStressRequest(decimal EquityShockPercent, decimal EurUsdShockPercent, decimal RateShockBasisPoints);

public sealed record HistoricalStressRequest(DateOnly From, DateOnly To, string? Name = null);

public sealed record HistoricalScenarioDto(string Id, string Name, string Description, DateOnly From, DateOnly To);

public sealed class ParametricStressRequestValidator : AbstractValidator<ParametricStressRequest>
{
    public ParametricStressRequestValidator()
    {
        RuleFor(request => request.EquityShockPercent).InclusiveBetween(-90m, 100m);
        RuleFor(request => request.EurUsdShockPercent).InclusiveBetween(-50m, 50m);
        RuleFor(request => request.RateShockBasisPoints).InclusiveBetween(-500m, 500m);
    }
}

public sealed class HistoricalStressRequestValidator : AbstractValidator<HistoricalStressRequest>
{
    public HistoricalStressRequestValidator()
    {
        RuleFor(request => request.To).GreaterThan(request => request.From);
        RuleFor(request => request.Name).MaximumLength(100);
    }
}
