using FluentValidation;
using Kairos.Application.Abstractions;
using Kairos.Application.Common;
using Kairos.Application.Portfolio;
using Kairos.Domain.Common;
using Kairos.Domain.Trades;

namespace Kairos.Application.Trades;

/// <summary>The use cases of the blotter: booking, amending and moving trades through their life.</summary>
public sealed class TradeService(
    ITradeRepository trades,
    IReferenceDataProvider referenceData,
    IMarketState marketState,
    IMarketHistory marketHistory,
    ITradeNotifier notifier,
    PortfolioCache portfolioCache,
    ICurrentActor actor,
    TimeProvider timeProvider,
    IValidator<TradeRequest> tradeRequestValidator,
    IValidator<CancelTradeRequest> cancelRequestValidator)
{
    /// <summary>Lists the trades done on or before the current market date.</summary>
    public async Task<IReadOnlyList<TradeDto>> ListAsync(CancellationToken cancellationToken)
    {
        var catalog = await referenceData.GetCatalogAsync(cancellationToken);
        var marketDate = marketState.Current.Date;
        var allTrades = await trades.ListAsync(cancellationToken);

        return allTrades
            .Where(trade => trade.Terms.TradeDate <= marketDate)
            .Select(trade => TradeMapper.ToDto(trade, catalog))
            .ToList();
    }

    public async Task<TradeDetailDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var trade = await RequireTradeAsync(id, cancellationToken);
        var catalog = await referenceData.GetCatalogAsync(cancellationToken);

        return TradeMapper.ToDetailDto(trade, catalog);
    }

    public async Task<TradeDto> BookAsync(TradeRequest request, CancellationToken cancellationToken)
    {
        await tradeRequestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var trade = Trade.Book(Guid.NewGuid(), await BuildTermsAsync(request, cancellationToken), actor.Name, Now);
        trades.Add(trade);

        return await SaveAndNotifyAsync(trade, cancellationToken);
    }

    public async Task<TradeDto> AmendAsync(Guid id, TradeRequest request, CancellationToken cancellationToken)
    {
        await tradeRequestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var trade = await RequireTradeAsync(id, cancellationToken);
        trade.Amend(await BuildTermsAsync(request, cancellationToken), actor.Name, Now);

        return await SaveAndNotifyAsync(trade, cancellationToken);
    }

    public async Task<TradeDto> ValidateAsync(Guid id, CancellationToken cancellationToken)
    {
        var trade = await RequireTradeAsync(id, cancellationToken);
        trade.Validate(actor.Name, Now);

        return await SaveAndNotifyAsync(trade, cancellationToken);
    }

    public async Task<TradeDto> SettleAsync(Guid id, CancellationToken cancellationToken)
    {
        var trade = await RequireTradeAsync(id, cancellationToken);
        trade.Settle(actor.Name, Now);

        return await SaveAndNotifyAsync(trade, cancellationToken);
    }

    public async Task<TradeDto> CancelAsync(Guid id, CancelTradeRequest request, CancellationToken cancellationToken)
    {
        await cancelRequestValidator.ValidateAndThrowAsync(request, cancellationToken);

        var trade = await RequireTradeAsync(id, cancellationToken);
        trade.Cancel(request.Reason, actor.Name, Now);

        return await SaveAndNotifyAsync(trade, cancellationToken);
    }

    private DateTimeOffset Now => timeProvider.GetUtcNow();

    private async Task<Trade> RequireTradeAsync(Guid id, CancellationToken cancellationToken) =>
        await trades.FindAsync(id, cancellationToken) ?? throw new NotFoundException($"Trade {id} does not exist.");

    private async Task<TradeTerms> BuildTermsAsync(TradeRequest request, CancellationToken cancellationToken)
    {
        var catalog = await referenceData.GetCatalogAsync(cancellationToken);
        var instrument = catalog.Instrument(request.InstrumentId);
        var book = catalog.Book(request.BookId);
        var trader = catalog.Trader(request.TraderId);
        var counterparty = catalog.Counterparty(request.CounterpartyId);

        var market = marketState.Current;
        var tradeDate = request.TradeDate ?? market.Date;

        if (tradeDate > market.Date)
        {
            throw new DomainException("A trade cannot be dated after the current market date.");
        }

        if (!instrument.IsTradableOn(tradeDate))
        {
            throw new DomainException($"{instrument.Symbol} is not tradable on {tradeDate:yyyy-MM-dd}.");
        }

        return new TradeTerms(
            instrument.Id,
            request.Side,
            request.Quantity,
            request.Price,
            counterparty.Id,
            book.Id,
            trader.Id,
            tradeDate,
            request.ValueDate ?? ValueDateCalculator.ValueDateFor(instrument.AssetClass, tradeDate),
            await RateOnAsync(tradeDate, cancellationToken));
    }

    private async Task<decimal> RateOnAsync(DateOnly tradeDate, CancellationToken cancellationToken)
    {
        if (tradeDate == marketState.Current.Date)
        {
            return marketState.Current.UsdPerEur;
        }

        var close = await marketHistory.GetCloseAsync(tradeDate, cancellationToken)
            ?? throw new DomainException($"{tradeDate:yyyy-MM-dd} is not a market day.");

        return close.UsdPerEur;
    }

    private async Task<TradeDto> SaveAndNotifyAsync(Trade trade, CancellationToken cancellationToken)
    {
        await trades.SaveChangesAsync(cancellationToken);
        portfolioCache.Invalidate();

        var catalog = await referenceData.GetCatalogAsync(cancellationToken);
        var dto = TradeMapper.ToDto(trade, catalog);
        await notifier.TradeChangedAsync(dto, cancellationToken);

        return dto;
    }
}
