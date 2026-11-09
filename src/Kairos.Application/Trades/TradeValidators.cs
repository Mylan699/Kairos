using FluentValidation;

namespace Kairos.Application.Trades;

public sealed class TradeRequestValidator : AbstractValidator<TradeRequest>
{
    // Well above any realistic ticket: stops absurd values from reaching a public demo.
    private const decimal MaximumQuantity = 10_000_000_000m;
    private const decimal MaximumPrice = 1_000_000m;

    public TradeRequestValidator()
    {
        RuleFor(request => request.InstrumentId).NotEmpty();
        RuleFor(request => request.Side).IsInEnum();
        RuleFor(request => request.Quantity).GreaterThan(0m).LessThanOrEqualTo(MaximumQuantity);
        RuleFor(request => request.Price).GreaterThan(0m).LessThanOrEqualTo(MaximumPrice);
        RuleFor(request => request.CounterpartyId).NotEmpty();
        RuleFor(request => request.BookId).NotEmpty();
        RuleFor(request => request.TraderId).NotEmpty();
        RuleFor(request => request.ValueDate)
            .GreaterThanOrEqualTo(request => request.TradeDate)
            .When(request => request.TradeDate.HasValue && request.ValueDate.HasValue);
    }
}

public sealed class CancelTradeRequestValidator : AbstractValidator<CancelTradeRequest>
{
    private const int MaximumReasonLength = 200;

    public CancelTradeRequestValidator()
    {
        RuleFor(request => request.Reason).NotEmpty().MaximumLength(MaximumReasonLength);
    }
}
