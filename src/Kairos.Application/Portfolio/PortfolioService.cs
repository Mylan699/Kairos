using FluentValidation;
using Kairos.Application.Abstractions;
using Kairos.Application.Trades;
using Kairos.Domain.Common;
using Kairos.Domain.Positions;
using Kairos.Domain.Risk;
using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Portfolio;

public sealed class PortfolioService(
    ITradeRepository trades,
    IReferenceDataProvider referenceData,
    IMarketState marketState,
    IMarketHistory marketHistory,
    PortfolioCache cache,
    IValidator<TradeRequest> tradeRequestValidator)
{
    private const int HistoryDays = 30;

    public async Task<IReadOnlyList<PositionDto>> GetPositionsAsync(CancellationToken cancellationToken)
    {
        var market = marketState.Current;
        var state = await GetStateAsync(cancellationToken);
        var catalog = await referenceData.GetCatalogAsync(cancellationToken);

        return PortfolioValuator.ValuePositions(state, market, catalog);
    }

    public async Task<PortfolioSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var positions = await GetPositionsAsync(cancellationToken);
        return PortfolioValuator.Summarize(marketState.Current.Date, positions);
    }

    /// <summary>Returns the cached positions of the market date, rebuilding them from the trades when needed.</summary>
    public async Task<PortfolioState> GetStateAsync(CancellationToken cancellationToken)
    {
        var marketDate = marketState.Current.Date;

        var cached = cache.Get(marketDate);
        if (cached is not null)
        {
            return cached;
        }

        var catalog = await referenceData.GetCatalogAsync(cancellationToken);
        var allTrades = await trades.ListAsync(cancellationToken);
        var previousCloses = await marketHistory.GetClosesBeforeAsync(marketDate, HistoryDays, cancellationToken);

        var totalsByClose = previousCloses
            .Select(close => (close.Date, Totals: PortfolioBuilder.TotalsAt(
                PortfolioBuilder.BuildPositions(allTrades, catalog, close.Date),
                close)))
            .ToList();

        var state = new PortfolioState(
            marketDate,
            PortfolioBuilder.BuildPositions(allTrades, catalog, marketDate),
            totalsByClose.Count == 0 ? new Dictionary<PositionKey, decimal>() : totalsByClose[^1].Totals,
            totalsByClose
                .Select(item => new PnlHistoryPoint(item.Date, Math.Round(item.Totals.Values.Sum(), 2, MidpointRounding.AwayFromZero)))
                .ToList());
        cache.Set(state);

        return state;
    }

    /// <summary>Works out what a trade would do to the book, without booking it.</summary>
    public async Task<TradeImpactDto> EstimateImpactAsync(TradeRequest request, CancellationToken cancellationToken)
    {
        await tradeRequestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var catalog = await referenceData.GetCatalogAsync(cancellationToken);
        var instrument = catalog.Instrument(request.InstrumentId);
        var book = catalog.Book(request.BookId);
        var market = marketState.Current;

        if (!instrument.IsTradableOn(market.Date))
        {
            throw new DomainException($"{instrument.Symbol} is not tradable on {market.Date:yyyy-MM-dd}.");
        }

        var state = await GetStateAsync(cancellationToken);
        var held = state.Positions.FirstOrDefault(item => item.BookId == book.Id && item.Instrument.Id == instrument.Id);

        var before = held?.Position ?? Position.Flat(instrument.AssetClass);
        var after = before.Apply(new Fill(request.Side, request.Quantity, request.Price, market.UsdPerEur));

        var dv01Before = RiskCalculator.Sensitivities([new BookPosition(book.Id, instrument, before)], market).Dv01;
        var dv01After = RiskCalculator.Sensitivities([new BookPosition(book.Id, instrument, after)], market).Dv01;
        var dv01Change = dv01After - dv01Before;
        var totalDv01 = RiskCalculator.Sensitivities(state.Positions, market).Dv01;

        var notionalInUsd = instrument.AssetClass.ContractMultiplier() * request.Quantity * request.Price;

        return new TradeImpactDto(
            NotionalEur: Math.Round(notionalInUsd / market.UsdPerEur, 2, MidpointRounding.AwayFromZero),
            PnlImpactEur: Math.Round(TotalPnl(after) - TotalPnl(before), 2, MidpointRounding.AwayFromZero),
            PositionBefore: before.Quantity,
            PositionAfter: after.Quantity,
            Dv01ChangeEur: dv01Change.Round(2).Amount,
            TotalDv01AfterEur: (totalDv01 + dv01Change).Round(2).Amount);

        decimal TotalPnl(Position position) =>
            position.Realized.Amount + position
                .Value(market.MarkPrice(instrument, position.Quantity), market.UsdPerEur)
                .Unrealized.Amount;
    }
}
