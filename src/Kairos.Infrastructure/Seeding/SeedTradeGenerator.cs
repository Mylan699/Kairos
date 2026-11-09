using Kairos.Domain.Instruments;
using Kairos.Domain.Rates;
using Kairos.Domain.Reference;
using Kairos.Domain.Trades;
using Kairos.Domain.ValueObjects;
using Kairos.Infrastructure.Persistence;

namespace Kairos.Infrastructure.Seeding;

/// <summary>The closing market of one day, as needed to price seed trades.</summary>
internal sealed record SeedMarketDay(
    DateOnly Date,
    IReadOnlyDictionary<Guid, EquityDailyBarRow> Equities,
    decimal CloseBid,
    decimal CloseAsk,
    YieldCurve Curve);

/// <summary>
/// Invents a realistic trade history: chronological, sized by asset class, kept within
/// position limits, with expiring bonds closed before maturity. The fixed random seed
/// makes every nightly reset produce exactly the same trades.
/// </summary>
internal sealed class SeedTradeGenerator(IReadOnlyList<Instrument> instruments)
{
    private const int RandomSeed = 20_100_104;
    private const int OpeningDays = 8;
    private const int DaysBeforeMaturityToClose = 10;

    private const decimal EquityPositionLimitUsd = 6_000_000m;
    private const decimal NominalPositionLimit = 40_000_000m;

    private const double CancelledShare = 0.03;
    private const double NewShare = 0.02;
    private const double ValidatedShare = 0.03;
    private const double AmendedShare = 0.06;

    private const string BackOffice = "Back Office";

    private static readonly string[] CancellationReasons =
        ["Erreur de saisie", "Doublon", "Contrepartie erronée", "Annulé à la demande du client"];

    private static readonly BookPlan[] Plans =
    [
        new(ReferenceSeed.CoreEquities, TradeCount: 900, OpeningTrades: 70, EquityWeight: 1.0, FxWeight: 0.0, LongBias: 0.2, LongOnly: true, TraderIndexes: [0, 1]),
        new(ReferenceSeed.SectorEquities, TradeCount: 700, OpeningTrades: 40, EquityWeight: 1.0, FxWeight: 0.0, LongBias: 0.05, LongOnly: false, TraderIndexes: [1, 2]),
        new(ReferenceSeed.Fx, TradeCount: 500, OpeningTrades: 10, EquityWeight: 0.0, FxWeight: 1.0, LongBias: 0.0, LongOnly: false, TraderIndexes: [3]),
        new(ReferenceSeed.Rates, TradeCount: 500, OpeningTrades: 12, EquityWeight: 0.0, FxWeight: 0.0, LongBias: 0.1, LongOnly: false, TraderIndexes: [4]),
        new(ReferenceSeed.CrossAsset, TradeCount: 400, OpeningTrades: 20, EquityWeight: 0.5, FxWeight: 0.25, LongBias: 0.05, LongOnly: false, TraderIndexes: [2, 5]),
    ];

    private readonly Random random = new(RandomSeed);
    private readonly Dictionary<(Guid Book, Guid Instrument), decimal> held = [];
    private readonly List<Trade> trades = [];
    private readonly List<Instrument> equities = instruments.Where(item => item.AssetClass == AssetClass.Equity).ToList();
    private readonly List<Instrument> bonds = instruments.Where(item => item.AssetClass == AssetClass.Bond).ToList();
    private readonly Instrument eurUsd = instruments.Single(item => item.AssetClass == AssetClass.Fx);
    private readonly Dictionary<Guid, Instrument> instrumentsById = instruments.ToDictionary(item => item.Id);

    public IReadOnlyList<Trade> Generate(IReadOnlyList<SeedMarketDay> days)
    {
        var plannedByDay = PlanTradeDays(days.Count);

        for (var index = 0; index < days.Count; index++)
        {
            CloseBondsNearMaturity(days[index]);

            foreach (var plan in plannedByDay[index])
            {
                AddTrade(plan, days[index]);
            }
        }

        return trades;
    }

    private List<BookPlan>[] PlanTradeDays(int dayCount)
    {
        var plannedByDay = Enumerable.Range(0, dayCount).Select(_ => new List<BookPlan>()).ToArray();
        var openingEnd = Math.Min(OpeningDays, dayCount - 1);

        foreach (var plan in Plans)
        {
            for (var count = 0; count < plan.TradeCount; count++)
            {
                var isOpening = count < plan.OpeningTrades;
                var dayIndex = isOpening ? random.Next(0, openingEnd + 1) : random.Next(0, dayCount);
                plannedByDay[dayIndex].Add(plan);
            }
        }

        return plannedByDay;
    }

    private void AddTrade(BookPlan plan, SeedMarketDay day)
    {
        var instrument = PickInstrument(plan, day);
        var position = held.GetValueOrDefault((plan.Book.Id, instrument.Id));
        var marketPrice = MidPrice(instrument, day);
        var limit = PositionLimit(instrument, marketPrice);

        var side = PickSide(plan, position, limit);
        var quantity = PickQuantity(instrument, marketPrice);

        if (plan.LongOnly && side == Side.Sell)
        {
            quantity = Math.Min(quantity, position);
        }

        if (quantity > 0m)
        {
            Record(plan, instrument, side, quantity, day);
        }
    }

    private void CloseBondsNearMaturity(SeedMarketDay day)
    {
        foreach (var plan in Plans)
        {
            var expiring = held
                .Where(item => item.Key.Book == plan.Book.Id && item.Value != 0m)
                .Select(item => (Instrument: instrumentsById[item.Key.Instrument], Quantity: item.Value))
                .Where(item => item.Instrument.Bond is { } bond
                    && bond.MaturityDate.DayNumber - day.Date.DayNumber <= DaysBeforeMaturityToClose)
                .ToList();

            foreach (var (instrument, quantity) in expiring)
            {
                Record(plan, instrument, quantity > 0m ? Side.Sell : Side.Buy, Math.Abs(quantity), day, alwaysLive: true);
            }
        }
    }

    private void Record(BookPlan plan, Instrument instrument, Side side, decimal quantity, SeedMarketDay day, bool alwaysLive = false)
    {
        var trader = ReferenceSeed.Traders[plan.TraderIndexes[random.Next(plan.TraderIndexes.Length)]];
        var counterparty = ReferenceSeed.Counterparties[random.Next(ReferenceSeed.Counterparties.Count)];
        var finalTerms = Terms(instrument, side, quantity, ExecutionPrice(instrument, side, day), counterparty, plan.Book, trader, day);

        var bookedAt = new DateTimeOffset(day.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddMinutes(random.Next(780, 1200));
        var outcome = alwaysLive ? 1.0 : random.NextDouble();

        var trade = random.NextDouble() < AmendedShare
            ? BookWithAmendment(finalTerms, trader, bookedAt, instrument, counterparty, plan.Book, day)
            : Trade.Book(NewId(), finalTerms, trader.Name, bookedAt);

        ApplyOutcome(trade, outcome, bookedAt, finalTerms.ValueDate);

        if (trade.IsLive)
        {
            var key = (plan.Book.Id, instrument.Id);
            held[key] = held.GetValueOrDefault(key) + quantity * side.Sign();
        }

        trades.Add(trade);
    }

    private Trade BookWithAmendment(
        TradeTerms finalTerms,
        Trader trader,
        DateTimeOffset bookedAt,
        Instrument instrument,
        Counterparty counterparty,
        Book book,
        SeedMarketDay day)
    {
        // The amendment is what produces the final terms, so positions only ever see those.
        var mistakenQuantity = finalTerms.Quantity * 1.1m;
        var firstTerms = Terms(instrument, finalTerms.Side, mistakenQuantity, finalTerms.Price, counterparty, book, trader, day);

        var trade = Trade.Book(NewId(), firstTerms, trader.Name, bookedAt);
        trade.Amend(finalTerms, trader.Name, bookedAt.AddMinutes(7));

        return trade;
    }

    private void ApplyOutcome(Trade trade, double outcome, DateTimeOffset bookedAt, DateOnly valueDate)
    {
        var validatedAt = bookedAt.AddMinutes(20);

        if (outcome < CancelledShare)
        {
            trade.Cancel(CancellationReasons[random.Next(CancellationReasons.Length)], trade.CurrentVersion.CreatedBy, validatedAt);
        }
        else if (outcome < CancelledShare + NewShare)
        {
            // Stays New.
        }
        else if (outcome < CancelledShare + NewShare + ValidatedShare)
        {
            trade.Validate(BackOffice, validatedAt);
        }
        else
        {
            trade.Validate(BackOffice, validatedAt);
            trade.Settle(BackOffice, new DateTimeOffset(valueDate.ToDateTime(new TimeOnly(17, 0)), TimeSpan.Zero));
        }
    }

    private Instrument PickInstrument(BookPlan plan, SeedMarketDay day)
    {
        var draw = random.NextDouble();

        if (draw < plan.EquityWeight)
        {
            return equities[random.Next(equities.Count)];
        }

        if (draw < plan.EquityWeight + plan.FxWeight)
        {
            return eurUsd;
        }

        var tradable = bonds.Where(bond => IsSafelyTradable(bond, day.Date)).ToList();
        return tradable[random.Next(tradable.Count)];
    }

    private static bool IsSafelyTradable(Instrument bond, DateOnly date) =>
        bond.IsTradableOn(date) && bond.Bond!.MaturityDate.DayNumber - date.DayNumber > DaysBeforeMaturityToClose + 5;

    /// <summary>The fuller the position, the more likely the desk is to reduce it.</summary>
    private Side PickSide(BookPlan plan, decimal position, decimal limit)
    {
        var fill = (double)Math.Clamp(position / limit, -1m, 1m);
        var buyProbability = 0.5 + plan.LongBias - 0.45 * fill;

        return random.NextDouble() < buyProbability ? Side.Buy : Side.Sell;
    }

    private static decimal PositionLimit(Instrument instrument, decimal price) =>
        instrument.AssetClass == AssetClass.Equity ? EquityPositionLimitUsd / price : NominalPositionLimit;

    private decimal PickQuantity(Instrument instrument, decimal price) => instrument.AssetClass switch
    {
        AssetClass.Equity => Math.Max(10m, RoundTo(Between(100_000m, 1_500_000m) / price, 10m)),
        AssetClass.Fx => RoundTo(Between(1_000_000m, 15_000_000m), 250_000m),
        _ => RoundTo(Between(1_000_000m, 20_000_000m), 500_000m),
    };

    private decimal MidPrice(Instrument instrument, SeedMarketDay day) => instrument.AssetClass switch
    {
        AssetClass.Equity => day.Equities[instrument.Id].Close,
        AssetClass.Fx => (day.CloseBid + day.CloseAsk) / 2m,
        _ => BondPricer.Value(instrument.Bond!, day.Date, day.Curve).CleanPrice,
    };

    private decimal ExecutionPrice(Instrument instrument, Side side, SeedMarketDay day)
    {
        switch (instrument.AssetClass)
        {
            case AssetClass.Equity:
                var bar = day.Equities[instrument.Id];
                return Math.Round(bar.Low + (bar.High - bar.Low) * (decimal)random.NextDouble(), 2, MidpointRounding.AwayFromZero);
            case AssetClass.Fx:
                return side == Side.Buy ? day.CloseAsk : day.CloseBid;
            default:
                var slippage = Between(-0.03m, 0.03m);
                return Math.Round(BondPricer.Value(instrument.Bond!, day.Date, day.Curve).CleanPrice + slippage, 3, MidpointRounding.AwayFromZero);
        }
    }

    private static TradeTerms Terms(
        Instrument instrument,
        Side side,
        decimal quantity,
        decimal price,
        Counterparty counterparty,
        Book book,
        Trader trader,
        SeedMarketDay day) => new(
            instrument.Id,
            side,
            quantity,
            price,
            counterparty.Id,
            book.Id,
            trader.Id,
            day.Date,
            ValueDateCalculator.ValueDateFor(instrument.AssetClass, day.Date),
            (day.CloseBid + day.CloseAsk) / 2m);

    private decimal Between(decimal low, decimal high) => low + (high - low) * (decimal)random.NextDouble();

    private static decimal RoundTo(decimal value, decimal step) =>
        Math.Round(value / step, MidpointRounding.AwayFromZero) * step;

    private Guid NewId()
    {
        var bytes = new byte[16];
        random.NextBytes(bytes);
        return new Guid(bytes);
    }

    private sealed record BookPlan(
        Book Book,
        int TradeCount,
        int OpeningTrades,
        double EquityWeight,
        double FxWeight,
        double LongBias,
        bool LongOnly,
        int[] TraderIndexes);
}
