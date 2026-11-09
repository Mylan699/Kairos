namespace Kairos.Domain.Trades;

/// <summary>One state of the terms of a trade. Versions are never modified or removed.</summary>
public sealed record TradeVersion(int Number, TradeTerms Terms, DateTimeOffset CreatedAt, string CreatedBy)
{
    // Entity Framework cannot pass an owned object through a constructor.
    private TradeVersion()
        : this(0, null!, default, string.Empty)
    {
    }
}

public enum AuditAction
{
    Booked,
    Validated,
    Settled,
    Cancelled,
    Amended,
}

/// <summary>One line of the audit trail of a trade.</summary>
/// <param name="Sequence">The position of the line in the trail, from 1.</param>
/// <param name="ResultingStatus">The status of the trade once the action is applied.</param>
/// <param name="Version">The version of the trade once the action is applied.</param>
/// <param name="Details">The amended fields or the cancellation reason. Empty otherwise.</param>
public sealed record AuditEntry(
    int Sequence,
    DateTimeOffset At,
    string Actor,
    AuditAction Action,
    TradeStatus ResultingStatus,
    int Version,
    string Details);
