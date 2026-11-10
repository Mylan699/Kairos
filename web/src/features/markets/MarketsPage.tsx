import { useMemo, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useCurve, useDailyBars, useIntradayPrices, useReferenceData, useRemarkableDays } from '@/api/queries';
import type { AssetClass, Curve, MarketTick, Quote } from '@/api/types';
import { CONTROL_CLASSES, cx, EmptyState, FlashValue, Panel, SearchIcon, SegmentedControl, Select, Skeleton, useTheme } from '@/design-system';
import { formatBasisPoints, formatPercent, formatPrice, formatYield, trendClass } from '@/lib/format';
import { ASSET_CLASS_LABELS } from '@/lib/labels';
import { useLive } from '@/realtime/live';
import { comparisonOptions, slope2s10s } from './curveModel';
import { DailyChart, IntradayChart } from './PriceChart';
import { YieldCurveChart, type CurveSeries } from './YieldCurveChart';

const DAILY_BAR_COUNT = 250;
const HISTORY_DAY_COUNT = 260;
const NO_COMPARISON = 'none';

// Leaves the two charts and their credit on a single screen at 1440 x 900.
const CHART_HEIGHT = 'h-[364px]';

const ASSET_ORDER: AssetClass[] = ['Fx', 'Bond', 'Equity'];

const MODE_SEGMENTS = [
  { value: 'intraday', label: 'Journée' },
  { value: 'daily', label: '1 an' },
] as const;

function Watchlist({ quotes, selectedId, onSelect }: { quotes: Quote[]; selectedId: string | null; onSelect: (id: string) => void }) {
  const [search, setSearch] = useState('');
  const term = search.trim().toLowerCase();
  const visible = quotes.filter((quote) => quote.symbol.toLowerCase().includes(term));

  return (
    <div className="flex h-full flex-col">
      <div className="relative border-b border-line-subtle p-2">
        <SearchIcon className="pointer-events-none absolute left-4.5 top-4 text-ink-3" />
        <input
          type="search"
          aria-label="Rechercher un instrument"
          placeholder="Rechercher"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          className={cx(CONTROL_CLASSES, 'pl-8')}
        />
      </div>
      <div className="max-h-72 min-h-0 flex-1 overflow-y-auto xl:max-h-none">
        {ASSET_ORDER.map((assetClass) => {
          const group = visible.filter((quote) => quote.assetClass === assetClass);

          return (
            group.length > 0 && (
              <section key={assetClass}>
                <h3 className="label sticky top-0 bg-surface-2 px-3 py-1.5">{ASSET_CLASS_LABELS[assetClass]}</h3>
                <ul>
                  {group.map((quote) => (
                    <li key={quote.instrumentId}>
                      <button
                        type="button"
                        aria-pressed={quote.instrumentId === selectedId}
                        onClick={() => onSelect(quote.instrumentId)}
                        className={cx(
                          'grid h-7 w-full grid-cols-[1fr_auto_4.5rem] items-center gap-2 px-3 text-xs transition-colors duration-[var(--motion-fast)]',
                          quote.instrumentId === selectedId ? 'bg-accent-soft' : 'hover:bg-surface-3',
                        )}
                      >
                        <span className="num truncate text-left text-ink">{quote.symbol}</span>
                        <FlashValue value={quote.last} className="text-ink-2">{formatPrice(quote.last, quote.assetClass)}</FlashValue>
                        <span className={cx('num text-right', trendClass(quote.changePercent))}>{formatPercent(quote.changePercent)}</span>
                      </button>
                    </li>
                  ))}
                </ul>
              </section>
            )
          );
        })}
      </div>
    </div>
  );
}

function ChartPanel({ tick, quote, assetClass }: { tick: MarketTick; quote: Quote; assetClass: AssetClass }) {
  const [mode, setMode] = useState<'intraday' | 'daily'>('intraday');
  const date = tick.status.date;
  const hasDailyBars = assetClass !== 'Bond';
  const showDaily = mode === 'daily' && hasDailyBars;

  const intraday = useIntradayPrices(quote.instrumentId, date);
  const daily = useDailyBars(showDaily ? quote.instrumentId : null, DAILY_BAR_COUNT);

  const isEquityBeforeOpen = assetClass === 'Equity' && tick.status.minute < tick.status.sessionOpenMinute;
  const last = isEquityBeforeOpen ? null : { minute: tick.status.minute, price: quote.last };

  return (
    <Panel
      className={CHART_HEIGHT}
      flush
      title={quote.symbol}
      aside={
        <div className="flex items-center gap-3">
          <span className="num text-sm text-ink">{formatPrice(quote.last, quote.assetClass)}</span>
          <span className={cx('num text-xs', trendClass(quote.changePercent))}>{formatPercent(quote.changePercent)}</span>
          {hasDailyBars && <SegmentedControl label="Période" value={mode} segments={MODE_SEGMENTS} onChange={setMode} />}
        </div>
      }
    >
      <div className="h-full p-2">
        {showDaily ? (
          daily.data ? (
            <DailyChart key={quote.instrumentId} assetClass={assetClass} bars={daily.data} />
          ) : (
            <Skeleton className="h-full w-full" />
          )
        ) : isEquityBeforeOpen ? (
          <EmptyState title="Séance pas encore ouverte" description="Les actions américaines cotent à partir de l'ouverture de New York." />
        ) : intraday.data ? (
          <IntradayChart key={quote.instrumentId} date={date} assetClass={assetClass} prices={intraday.data} last={last} />
        ) : (
          <Skeleton className="h-full w-full" />
        )}
      </div>
    </Panel>
  );
}

function CurvePanel({ tick, eurUsdId }: { tick: MarketTick; eurUsdId: string | null }) {
  useTheme();
  const [firstChoice, setFirstChoice] = useState<string | null>(null);
  const [secondChoice, setSecondChoice] = useState<string | null>(null);

  const days = useDailyBars(eurUsdId, HISTORY_DAY_COUNT);
  const remarkable = useRemarkableDays();

  const options = useMemo(
    () => comparisonOptions(days.data?.map((bar) => bar.date) ?? [], remarkable.data ?? [], tick.status.date),
    [days.data, remarkable.data, tick.status.date],
  );

  const selectable = [{ value: NO_COMPARISON, label: 'Aucune' }, ...options];

  // Until a choice is made, the curve is compared with the previous day and with last year.
  const firstDate = firstChoice ?? options[0]?.value ?? NO_COMPARISON;
  const secondDate = secondChoice ?? options.find((option) => option.label === 'Il y a 1 an')?.value ?? NO_COMPARISON;

  const firstCurve = useCurve(firstDate === NO_COMPARISON ? null : firstDate);
  const secondCurve = useCurve(secondDate === NO_COMPARISON ? null : secondDate);

  const labelOf = (date: string) => options.find((option) => option.value === date)?.label ?? date;

  const series: CurveSeries[] = [{ key: 'live', label: 'Courbe en direct', curve: tick.curve, stroke: 'var(--kairos-accent)' }];
  const addComparison = (key: string, date: string, curve: Curve | undefined, dashed: boolean) => {
    if (date !== NO_COMPARISON && curve) {
      series.push({ key, label: labelOf(date), curve, stroke: dashed ? 'var(--kairos-ink-3)' : 'var(--kairos-ink-2)', dashed });
    }
  };

  addComparison('first', firstDate, firstCurve.data, false);
  addComparison('second', secondDate, secondCurve.data, true);

  const slope = slope2s10s(tick.curve);
  const tenYear = tick.curve.points.find((point) => point.months === 120);

  return (
    <Panel
      title="Courbe des taux US"
      aside={
        <dl className="flex items-baseline gap-4 whitespace-nowrap">
          <div className="flex items-baseline gap-1.5">
            <dt className="label">10 ans</dt>
            <dd className="num text-sm text-ink">{formatYield(tenYear?.yieldPercent)}</dd>
          </div>
          <div className="hidden items-baseline gap-1.5 sm:flex">
            <dt className="label">Pente 2-10 ans</dt>
            <dd className="num text-sm text-ink">{formatBasisPoints(slope)}</dd>
          </div>
        </dl>
      }
    >
      <div className="mb-3 grid max-w-md grid-cols-2 gap-2">
        <Select label="Comparer avec" value={firstDate} options={selectable} onChange={setFirstChoice} />
        <Select label="Et avec" value={secondDate} options={selectable} onChange={setSecondChoice} />
      </div>
      <YieldCurveChart series={series} />
    </Panel>
  );
}

function MarketsSkeleton() {
  return (
    <div className="grid gap-2 md:gap-3 xl:grid-cols-12" aria-busy="true" aria-label="Chargement des marchés">
      <Skeleton className="h-96 xl:col-span-3 xl:row-span-2 xl:h-auto" />
      <Skeleton className={cx(CHART_HEIGHT, 'xl:col-span-9')} />
      <Skeleton className="h-96 xl:col-span-9" />
    </div>
  );
}

export default function MarketsPage() {
  const { tick } = useLive();
  const reference = useReferenceData();
  const [searchParams, setSearchParams] = useSearchParams();

  if (!tick || !reference.data) {
    return <MarketsSkeleton />;
  }

  const eurUsd = reference.data.instruments.find((item) => item.assetClass === 'Fx');
  const requested = searchParams.get('instrument');
  const selectedId = tick.quotes.some((quote) => quote.instrumentId === requested) ? requested : eurUsd?.id ?? null;
  const quote = tick.quotes.find((item) => item.instrumentId === selectedId);

  return (
    <div className="grid items-start gap-2 md:gap-3 xl:grid-cols-12">
      <Panel flush title="Instruments" className="order-3 xl:sticky xl:top-15 xl:order-none xl:col-span-3 xl:row-span-2 xl:h-[calc(100dvh-4.5rem)]">
        <Watchlist quotes={tick.quotes} selectedId={selectedId} onSelect={(id) => setSearchParams({ instrument: id })} />
      </Panel>

      <div className="order-1 flex min-w-0 flex-col gap-2 md:gap-3 xl:order-none xl:col-span-9">
        {quote ? <ChartPanel tick={tick} quote={quote} assetClass={quote.assetClass} /> : <EmptyState title="Instrument indisponible à cette date" />}
        <CurvePanel tick={tick} eurUsdId={eurUsd?.id ?? null} />
        <p className="text-2xs text-ink-3">
          Graphiques de prix réalisés avec{' '}
          <a href="https://www.tradingview.com/lightweight-charts/" className="underline hover:text-ink" target="_blank" rel="noreferrer">
            TradingView Lightweight Charts
          </a>
          .
        </p>
      </div>
    </div>
  );
}
