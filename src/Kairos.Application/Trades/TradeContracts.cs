using Kairos.Domain.Trades;
using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Trades;

/// <summary>What a user enters to book or amend a trade. Dates default to the market date and its settlement.</summary>
public sealed record TradeRequest(
    Guid InstrumentId,
    Side Side,
    decimal Quantity,
    decimal Price,
    Guid CounterpartyId,
    Guid BookId,
    Guid TraderId,
    DateOnly? TradeDate = null,
    DateOnly? ValueDate = null);

public sealed record CancelTradeRequest(string Reason);

/// <summary>One row of the blotter.</summary>
public sealed record TradeDto(
    Guid Id,
    int Version,
    TradeStatus Status,
    Guid InstrumentId,
    string Symbol,
    AssetClass AssetClass,
    Side Side,
    decimal Quantity,
    decimal Price,
    Guid CounterpartyId,
    string Counterparty,
    Guid BookId,
    string Book,
    Guid TraderId,
    string Trader,
    DateOnly TradeDate,
    DateOnly ValueDate,
    DateTimeOffset UpdatedAt);

public sealed record TradeVersionDto(
    int Number,
    Side Side,
    decimal Quantity,
    decimal Price,
    string Counterparty,
    string Book,
    string Trader,
    DateOnly TradeDate,
    DateOnly ValueDate,
    DateTimeOffset CreatedAt,
    string CreatedBy);

public sealed record AuditEntryDto(
    DateTimeOffset At,
    string Actor,
    AuditAction Action,
    TradeStatus ResultingStatus,
    int Version,
    string Details);

/// <summary>A trade with its whole history.</summary>
public sealed record TradeDetailDto(
    TradeDto Trade,
    IReadOnlyList<TradeVersionDto> Versions,
    IReadOnlyList<AuditEntryDto> AuditTrail);
