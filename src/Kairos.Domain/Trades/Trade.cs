using Kairos.Domain.Common;

namespace Kairos.Domain.Trades;

/// <summary>A trade, its version history and its audit trail.</summary>
public sealed class Trade
{
    private readonly List<TradeVersion> versions = [];
    private readonly List<AuditEntry> auditTrail = [];

    private Trade(Guid id) => Id = id;

    public Guid Id { get; }

    public TradeStatus Status { get; private set; }

    public IReadOnlyList<TradeVersion> Versions => versions;

    public IReadOnlyList<AuditEntry> AuditTrail => auditTrail;

    public TradeVersion CurrentVersion => versions[^1];

    public TradeTerms Terms => CurrentVersion.Terms;

    /// <summary>A cancelled trade no longer counts in positions.</summary>
    public bool IsLive => Status != TradeStatus.Cancelled;

    public static Trade Book(Guid id, TradeTerms terms, string actor, DateTimeOffset at)
    {
        EnsureActorIsNamed(actor);

        var trade = new Trade(id) { Status = TradeStatus.New };
        trade.versions.Add(new TradeVersion(1, terms, at, actor));
        trade.RecordAudit(at, actor, AuditAction.Booked);

        return trade;
    }

    public void Validate(string actor, DateTimeOffset at)
    {
        EnsureActorIsNamed(actor);
        MoveTo(TradeStatus.Validated);
        RecordAudit(at, actor, AuditAction.Validated);
    }

    public void Settle(string actor, DateTimeOffset at)
    {
        EnsureActorIsNamed(actor);
        MoveTo(TradeStatus.Settled);
        RecordAudit(at, actor, AuditAction.Settled);
    }

    public void Cancel(string reason, string actor, DateTimeOffset at)
    {
        EnsureActorIsNamed(actor);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Cancelling a trade requires a reason.");
        }

        MoveTo(TradeStatus.Cancelled);
        RecordAudit(at, actor, AuditAction.Cancelled, reason);
    }

    /// <summary>Adds a new version of the terms. A validated trade has to be validated again.</summary>
    public void Amend(TradeTerms newTerms, string actor, DateTimeOffset at)
    {
        EnsureActorIsNamed(actor);

        if (!TradeStateMachine.CanAmend(Status))
        {
            throw new DomainException($"A {Status} trade cannot be amended.");
        }

        if (newTerms == Terms)
        {
            throw new DomainException("An amendment must change at least one term.");
        }

        var changes = newTerms.DescribeChangesFrom(Terms);
        versions.Add(new TradeVersion(CurrentVersion.Number + 1, newTerms, at, actor));

        if (Status == TradeStatus.Validated)
        {
            MoveTo(TradeStatus.New);
        }

        RecordAudit(at, actor, AuditAction.Amended, changes);
    }

    private void MoveTo(TradeStatus target)
    {
        TradeStateMachine.EnsureCanMove(Status, target);
        Status = target;
    }

    private void RecordAudit(DateTimeOffset at, string actor, AuditAction action, string details = "")
    {
        auditTrail.Add(new AuditEntry(at, actor, action, Status, CurrentVersion.Number, details));
    }

    // Checked before anything changes, so a trade can never move without an audit line.
    private static void EnsureActorIsNamed(string actor)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new DomainException("Every action on a trade must name who performed it.");
        }
    }
}
