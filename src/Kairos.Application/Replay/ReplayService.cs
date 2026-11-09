using FluentValidation;
using Kairos.Application.Common;
using Kairos.Domain.Common;

namespace Kairos.Application.Replay;

/// <summary>The commands of the replay screen, validated before they reach the engine.</summary>
public sealed class ReplayService(
    IReplayControl control,
    IValidator<LoadReplayRequest> loadValidator,
    IValidator<SetReplaySpeedRequest> speedValidator,
    IValidator<SeekReplayRequest> seekValidator)
{
    public ReplayStatusDto GetStatus() => control.Status ?? throw new MarketNotReadyException();

    public static IReadOnlyList<RemarkableDayDto> GetRemarkableDays() => RemarkableDays.All;

    public async Task<ReplayStatusDto> LoadAsync(LoadReplayRequest request, CancellationToken cancellationToken)
    {
        await loadValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (!await control.LoadDayAsync(request.Date, request.StartMinute, cancellationToken))
        {
            throw new DomainException($"{request.Date:yyyy-MM-dd} cannot be replayed.");
        }

        return GetStatus();
    }

    public ReplayStatusDto Play()
    {
        control.Play();
        return GetStatus();
    }

    public ReplayStatusDto Pause()
    {
        control.Pause();
        return GetStatus();
    }

    public async Task<ReplayStatusDto> SetSpeedAsync(SetReplaySpeedRequest request, CancellationToken cancellationToken)
    {
        await speedValidator.ValidateAndThrowAsync(request, cancellationToken);

        control.SetSpeed(request.Speed);
        return GetStatus();
    }

    public async Task<ReplayStatusDto> SeekAsync(SeekReplayRequest request, CancellationToken cancellationToken)
    {
        await seekValidator.ValidateAndThrowAsync(request, cancellationToken);

        control.Seek(request.Minute);
        return GetStatus();
    }
}
