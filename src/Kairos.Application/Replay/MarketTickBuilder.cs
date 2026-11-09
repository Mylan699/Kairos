using Kairos.Application.Abstractions;
using Kairos.Application.Market;
using Kairos.Application.Portfolio;
using Kairos.Domain.Instruments;
using Kairos.Domain.Market;
using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Replay;

/// <summary>Assembles the message sent to screens on each tick.</summary>
public static class MarketTickBuilder
{
    // Only EUR/USD has a real bid and ask. Equities and bonds get a typical one-tick spread.
    private const decimal EquityHalfSpread = 0.01m;
    private const decimal BondHalfSpread = 0.015m;

    private const decimal FractionToPercent = 100m;

    public static MarketTickDto Build(
        ReplayStatusDto status,
        MarketSnapshot market,
        MarketSnapshot previousClose,
        ReferenceCatalog catalog,
        PortfolioSummaryDto summary) => new(
            status,
            catalog.Instruments
                .Where(instrument => instrument.IsTradableOn(market.Date))
                .Select(instrument => QuoteOf(instrument, market, previousClose))
                .ToList(),
            new CurveDto(
                market.Date,
                market.Curve.Points
                    .Select(point => new CurvePointDto(point.Tenor.Label, point.Tenor.Months, Math.Round(point.Yield * FractionToPercent, 4)))
                    .ToList()),
            summary);

    private static QuoteDto QuoteOf(Instrument instrument, MarketSnapshot market, MarketSnapshot previousClose) =>
        instrument.AssetClass switch
        {
            AssetClass.Equity => EquityQuote(instrument, market, previousClose),
            AssetClass.Fx => EurUsdQuote(instrument, market, previousClose),
            _ => BondQuote(instrument, market, previousClose),
        };

    private static QuoteDto EquityQuote(Instrument instrument, MarketSnapshot market, MarketSnapshot previousClose)
    {
        var last = market.EquityPrices[instrument.Id];

        return new QuoteDto(
            instrument.Id,
            instrument.Symbol,
            instrument.AssetClass,
            last - EquityHalfSpread,
            last + EquityHalfSpread,
            last,
            PercentChange(last, previousClose.EquityPrices[instrument.Id]),
            YieldPercent: null);
    }

    private static QuoteDto EurUsdQuote(Instrument instrument, MarketSnapshot market, MarketSnapshot previousClose)
    {
        var last = Math.Round(market.EurUsd.Mid, 5, MidpointRounding.AwayFromZero);

        return new QuoteDto(
            instrument.Id,
            instrument.Symbol,
            instrument.AssetClass,
            market.EurUsd.Bid,
            market.EurUsd.Ask,
            last,
            PercentChange(market.EurUsd.Mid, previousClose.EurUsd.Mid),
            YieldPercent: null);
    }

    private static QuoteDto BondQuote(Instrument instrument, MarketSnapshot market, MarketSnapshot previousClose)
    {
        var bond = instrument.Bond!;
        var valuation = BondPricer.Value(bond, market.Date, market.Curve);
        var last = Math.Round(valuation.CleanPrice, 3, MidpointRounding.AwayFromZero);

        // A bond issued today has no previous close to compare with.
        var reference = previousClose.Date >= bond.IssueDate
            ? BondPricer.Value(bond, previousClose.Date, previousClose.Curve).CleanPrice
            : valuation.CleanPrice;

        return new QuoteDto(
            instrument.Id,
            instrument.Symbol,
            instrument.AssetClass,
            last - BondHalfSpread,
            last + BondHalfSpread,
            last,
            PercentChange(valuation.CleanPrice, reference),
            Math.Round(valuation.Yield * FractionToPercent, 3));
    }

    private static decimal PercentChange(decimal last, decimal reference) =>
        Math.Round((last / reference - 1m) * FractionToPercent, 2, MidpointRounding.AwayFromZero);
}
