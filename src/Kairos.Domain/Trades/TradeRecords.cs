namespace Kairos.Domain.Trades;

/// <summary>One state of the terms of a trade. Versions are never modified or removed.</summary>
public sealed record TradeVersion(int Number, TradeTerms Terms, DateTimeOffset CreatedAt, string CreatedBy);

public enum AuditAction
{
    Booked,
    Validated,
    Settled,
    Cancelled,
    Amended,
}

/// <summary>One line of the audit trail of a trade.</summary>
/// <param name="ResultingStatus">The status of the trade once the action is applied.</param>
/// <param name="Version">The version of the trade once the action is applied.</param>
/// <param name="Details">The amended fields or the cancellation reason. Empty otherwise.</param>
public sealed record AuditEntry(
    DateTimeOffset At,
    string Actor,
    AuditAction Action,
    TradeStatus ResultingStatus,
    int Version,
    string Details);
