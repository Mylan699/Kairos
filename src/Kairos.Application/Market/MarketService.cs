using Kairos.Application.Abstractions;
using Kairos.Domain.Common;
using Kairos.Domain.Market;
using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Market;

public sealed record InstrumentDto(
    Guid Id,
    string Symbol,
    AssetClass AssetClass,
    string? Sector,
    bool IsTradable,
    DateOnly? MaturityDate,
    decimal? CouponRate);

public sealed record ReferenceItemDto(Guid Id, string Name);

public sealed record ReferenceDataDto(
    IReadOnlyList<InstrumentDto> Instruments,
    IReadOnlyList<ReferenceItemDto> Books,
    IReadOnlyList<ReferenceItemDto> Traders,
    IReadOnlyList<ReferenceItemDto> Counterparties);

public sealed record CurvePointDto(string Tenor, int Months, decimal YieldPercent);

public sealed record CurveDto(DateOnly Date, IReadOnlyList<CurvePointDto> Points);

/// <summary>Read side of the market. Nothing after the current market date is ever returned.</summary>
public sealed class MarketService(
    IReferenceDataProvider referenceData,
    IMarketState marketState,
    IMarketHistory marketHistory)
{
    private const int MaximumBars = 2_600;
    private const decimal FractionToPercent = 100m;

    public async Task<ReferenceDataDto> GetReferenceDataAsync(CancellationToken cancellationToken)
    {
        var catalog = await referenceData.GetCatalogAsync(cancellationToken);
        var marketDate = marketState.Current.Date;

        return new ReferenceDataDto(
            catalog.Instruments
                .Select(instrument => new InstrumentDto(
                    instrument.Id,
                    instrument.Symbol,
                    instrument.AssetClass,
                    instrument.Sector,
                    instrument.IsTradableOn(marketDate),
                    instrument.Bond?.MaturityDate,
                    instrument.Bond?.CouponRate))
                .OrderBy(instrument => instrument.AssetClass)
                .ThenBy(instrument => instrument.Symbol)
                .ToList(),
            catalog.Books.Select(book => new ReferenceItemDto(book.Id, book.Name)).OrderBy(item => item.Name).ToList(),
            catalog.Traders.Select(trader => new ReferenceItemDto(trader.Id, trader.Name)).OrderBy(item => item.Name).ToList(),
            catalog.Counterparties.Select(counterparty => new ReferenceItemDto(counterparty.Id, counterparty.Name)).OrderBy(item => item.Name).ToList());
    }

    /// <summary>Returns the daily bars before the market date: today's bar is still being written by the replay.</summary>
    public async Task<IReadOnlyList<DailyBar>> GetDailyBarsAsync(Guid instrumentId, int count, CancellationToken cancellationToken)
    {
        var catalog = await referenceData.GetCatalogAsync(cancellationToken);
        var instrument = catalog.Instrument(instrumentId);
        var boundedCount = Math.Clamp(count, 1, MaximumBars);

        return await marketHistory.GetDailyBarsBeforeAsync(instrument.Id, marketState.Current.Date, boundedCount, cancellationToken);
    }

    /// <summary>Returns the live curve when no date is given, or the close of a past date.</summary>
    public async Task<CurveDto> GetCurveAsync(DateOnly? date, CancellationToken cancellationToken)
    {
        var market = marketState.Current;
        var requested = date ?? market.Date;

        if (requested > market.Date)
        {
            throw new DomainException("A curve cannot be requested after the current market date.");
        }

        var snapshot = requested == market.Date
            ? market
            : await marketHistory.GetCloseAsync(requested, cancellationToken)
                ?? throw new DomainException($"{requested:yyyy-MM-dd} is not a market day.");

        return ToDto(snapshot);
    }

    private static CurveDto ToDto(MarketSnapshot snapshot) => new(
        snapshot.Date,
        snapshot.Curve.Points.Select(ToDto).ToList());

    private static CurvePointDto ToDto(CurvePoint point) =>
        new(point.Tenor.Label, point.Tenor.Months, point.Yield * FractionToPercent);
}
