using FluentValidation;
using Kairos.Application.Market;
using Kairos.Application.Portfolio;
using Kairos.Application.Risk;
using Kairos.Domain.Market;
using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Replay;

/// <param name="Speed">Simulated seconds per real second.</param>
/// <param name="Minute">Minutes since midnight UTC on the replayed date.</param>
/// <param name="SessionOpenMinute">When the US equity session opens, in the same unit.</param>
/// <param name="SessionCloseMinute">When the US equity session closes, in the same unit.</param>
public sealed record ReplayStatusDto(
    DateOnly Date,
    int Minute,
    int Speed,
    bool IsPlaying,
    int SessionOpenMinute,
    int SessionCloseMinute);

/// <param name="Last">Last price: the mid for EUR/USD, the clean price for a bond.</param>
/// <param name="ChangePercent">Change since the previous close.</param>
/// <param name="YieldPercent">The yield of a bond. Null for other instruments.</param>
public sealed record QuoteDto(
    Guid InstrumentId,
    string Symbol,
    AssetClass AssetClass,
    decimal Bid,
    decimal Ask,
    decimal Last,
    decimal ChangePercent,
    decimal? YieldPercent);

/// <summary>What every screen receives on each tick of the replay.</summary>
public sealed record MarketTickDto(
    ReplayStatusDto Status,
    IReadOnlyList<QuoteDto> Quotes,
    CurveDto Curve,
    PortfolioSummaryDto Summary);

public sealed record RemarkableDayDto(DateOnly Date, string Name, string Description, int StartMinute);

public sealed record LoadReplayRequest(DateOnly Date, int? StartMinute = null);

public sealed record SetReplaySpeedRequest(int Speed);

public sealed record SeekReplayRequest(int Minute);

public sealed class LoadReplayRequestValidator : AbstractValidator<LoadReplayRequest>
{
    public LoadReplayRequestValidator()
    {
        RuleFor(request => request.StartMinute).InclusiveBetween(0, ReplayLimits.LastMinute);
    }
}

public sealed class SetReplaySpeedRequestValidator : AbstractValidator<SetReplaySpeedRequest>
{
    public SetReplaySpeedRequestValidator()
    {
        RuleFor(request => request.Speed)
            .Must(speed => ReplayLimits.Speeds.Contains(speed))
            .WithMessage("This replay speed is not available.");
    }
}

public sealed class SeekReplayRequestValidator : AbstractValidator<SeekReplayRequest>
{
    public SeekReplayRequestValidator()
    {
        RuleFor(request => request.Minute).InclusiveBetween(0, ReplayLimits.LastMinute);
    }
}

public static class ReplayLimits
{
    public const int LastMinute = 1_439;

    /// <summary>Simulated seconds per real second, from half a minute to half an hour.</summary>
    public static IReadOnlyList<int> Speeds { get; } = [30, 60, 120, 240, 600, 1_800];
}

public sealed record ReplaySession(ReplayDay Day, int Minute);

/// <summary>Controls the replay. It is shared by every visitor of the demo (ADR-011).</summary>
public interface IReplayControl
{
    ReplayStatusDto? Status { get; }

    /// <summary>The day being replayed and how far it has gone. Null until the first day is loaded.</summary>
    ReplaySession? CurrentSession { get; }

    /// <returns>False when the date cannot be replayed.</returns>
    Task<bool> LoadDayAsync(DateOnly date, int? startMinute, CancellationToken cancellationToken);

    void Play();

    void Pause();

    void SetSpeed(int speed);

    void Seek(int minute);
}

/// <summary>Pushes live data to connected screens.</summary>
public interface IMarketBroadcaster
{
    Task PublishTickAsync(MarketTickDto tick, CancellationToken cancellationToken);

    Task PublishPositionsAsync(IReadOnlyList<PositionDto> positions, CancellationToken cancellationToken);

    Task PublishRiskAsync(RiskReportDto report, CancellationToken cancellationToken);

    Task PublishDataResetAsync(CancellationToken cancellationToken);
}
