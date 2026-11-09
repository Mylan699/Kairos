import type { ReactNode } from 'react';
import { useBookFilter } from '@/app/BookFilter';
import { useDashboard, useIntradayOverview, usePositions } from '@/api/queries';
import type { IntradayPoint, PortfolioSummary } from '@/api/types';
import { BarList, cx, EmptyState, ErrorState, Panel, RollingNumber, Skeleton, Sparkline, StaggerGroup, StaggerItem } from '@/design-system';
import { ASSET_CLASS_LABELS } from '@/lib/labels';
import {
  formatCompactEur,
  formatDate,
  formatMinute,
  formatPercent,
  formatPrice,
  formatSignedEur,
  formatYield,
  trendClass,
} from '@/lib/format';
import { useElementWidth } from '@/lib/useElementWidth';
import { useLive, useLiveTopic } from '@/realtime/live';
import { contributors, exposureByAssetClass, figuresFor, positionsOfBook } from './dashboardModel';

const CONTRIBUTOR_COUNT = 5;
const MINI_CHART_HEIGHT = 40;

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="label">{label}</dt>
      <dd className={cx('num text-base', trendClass(value))}>{formatSignedEur(value, 0)}</dd>
    </div>
  );
}

function FluidSparkline({ values, label, height, formatPoint }: { values: number[]; label: string; height: number; formatPoint?: (index: number) => string }) {
  const [containerRef, width] = useElementWidth<HTMLDivElement>();

  return (
    <div ref={containerRef} style={{ height }}>
      {width > 0 && <Sparkline values={values} label={label} width={width} height={height} formatPoint={formatPoint} />}
    </div>
  );
}

function MiniChart({ label, value, change, values, points, format }: {
  label: string;
  value: string;
  change?: ReactNode;
  values: number[];
  points: IntradayPoint[];
  format: (value: number) => string;
}) {
  return (
    <Panel className="h-full">
      <div className="flex items-baseline justify-between gap-2">
        <h3 className="label">{label}</h3>
        {change}
      </div>
      <p className="num mt-1 text-xl text-ink">{value}</p>
      <div className="mt-2">
        <FluidSparkline
          values={values}
          label={`${label} depuis minuit`}
          height={MINI_CHART_HEIGHT}
          formatPoint={(index) => `${formatMinute(points[index]?.minute ?? 0)}  ${format(values[index] ?? 0)}`}
        />
      </div>
    </Panel>
  );
}

function BooksTable({ summary }: { summary: PortfolioSummary }) {
  const { bookId, setBookId } = useBookFilter();

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-xs">
        <thead>
          <tr className="text-left">
            <th scope="col" className="label px-3 py-2 font-normal">Book</th>
            <th scope="col" className="label px-3 py-2 text-right font-normal">Valeur</th>
            <th scope="col" className="label px-3 py-2 text-right font-normal">Jour</th>
            <th scope="col" className="label px-3 py-2 text-right font-normal">Cumul</th>
          </tr>
        </thead>
        <tbody>
          {summary.books.map((book) => (
            <tr key={book.bookId} className={cx('border-t border-line-subtle', bookId === book.bookId && 'bg-accent-soft')}>
              <td className="px-3 py-2">
                <button
                  type="button"
                  aria-pressed={bookId === book.bookId}
                  onClick={() => setBookId(bookId === book.bookId ? null : book.bookId)}
                  className="whitespace-nowrap text-left text-ink hover:text-accent"
                >
                  {book.book}
                </button>
              </td>
              <td className="num px-3 py-2 text-right text-ink-2">{formatCompactEur(book.marketValue, false)}</td>
              <td className={cx('num px-3 py-2 text-right', trendClass(book.pnl.day))}>{formatSignedEur(book.pnl.day, 0)}</td>
              <td className={cx('num px-3 py-2 text-right', trendClass(book.pnl.total))}>{formatCompactEur(book.pnl.total)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr className="border-t border-line">
            <th scope="row" className="px-3 py-2 text-left font-medium text-ink">Desk</th>
            <td className="num px-3 py-2 text-right text-ink-2">
              {formatCompactEur(summary.books.reduce((total, book) => total + book.marketValue, 0), false)}
            </td>
            <td className={cx('num px-3 py-2 text-right', trendClass(summary.total.day))}>{formatSignedEur(summary.total.day, 0)}</td>
            <td className={cx('num px-3 py-2 text-right', trendClass(summary.total.total))}>{formatCompactEur(summary.total.total)}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  );
}

function DashboardSkeleton() {
  return (
    <div className="grid grid-cols-1 gap-2 md:grid-cols-2 md:gap-3 xl:grid-cols-12" aria-busy="true" aria-label="Chargement du tableau de bord">
      <Skeleton className="h-36 xl:col-span-4" />
      <Skeleton className="h-36 xl:col-span-5" />
      <Skeleton className="h-36 md:col-span-2 xl:col-span-3" />
      {Array.from({ length: 4 }, (_, index) => <Skeleton key={index} className="h-32 xl:col-span-3" />)}
      <Skeleton className="h-64 md:col-span-2 xl:col-span-5" />
      <Skeleton className="h-64 xl:col-span-3" />
      <Skeleton className="h-64 xl:col-span-4" />
    </div>
  );
}

export default function DashboardPage() {
  useLiveTopic('positions');

  const { tick, positions: livePositions } = useLive();
  const { bookId } = useBookFilter();
  const positionsQuery = usePositions();
  const dashboardQuery = useDashboard();
  const intraday = useIntradayOverview();

  const allPositions = livePositions ?? positionsQuery.data;

  if (positionsQuery.isError && !allPositions) {
    return <ErrorState onRetry={() => void positionsQuery.refetch()} />;
  }

  if (!tick || !allPositions) {
    return <DashboardSkeleton />;
  }

  const figures = figuresFor(tick.summary, bookId);
  const positions = positionsOfBook(allPositions, bookId);
  const exposure = exposureByAssetClass(positions);
  const { top, bottom } = contributors(positions, CONTRIBUTOR_COUNT);
  const history = dashboardQuery.data?.history ?? [];
  const eurUsd = tick.quotes.find((quote) => quote.assetClass === 'Fx');
  const tenYear = tick.curve.points.find((point) => point.months === 120);
  const last = intraday[intraday.length - 1];
  const scope = bookId ? tick.summary.books.find((book) => book.bookId === bookId)?.book : 'Desk consolidé';

  return (
    <StaggerGroup className="grid grid-cols-1 gap-2 md:grid-cols-2 md:gap-3 xl:grid-cols-12">
      <StaggerItem className="xl:col-span-4">
        <Panel className="h-full" title="P&L du jour">
          <RollingNumber text={formatSignedEur(figures.day, 0)} className={cx('text-3xl font-medium tracking-tight', trendClass(figures.day))} />
          <p className="mt-2 text-xs text-ink-2">{scope}, en euros</p>
        </Panel>
      </StaggerItem>

      <StaggerItem className="xl:col-span-5">
        <Panel className="h-full" title="Résultat cumulé">
          <dl className="grid grid-cols-2 gap-x-6 gap-y-3 sm:grid-cols-3">
            <Stat label="Total" value={figures.total} />
            <Stat label="Réalisé" value={figures.realized} />
            <Stat label="Latent" value={figures.unrealized} />
            <Stat label="Effet prix" value={figures.priceEffect} />
            <Stat label="Effet change" value={figures.fxEffect} />
          </dl>
        </Panel>
      </StaggerItem>

      <StaggerItem className="md:col-span-2 xl:col-span-3">
        <Panel className="h-full" title="Desk, 30 dernières clôtures">
          <FluidSparkline
            values={history.map((point) => point.total)}
            label="P&L cumulé du desk sur les 30 dernières clôtures"
            height={72}
            formatPoint={(index) => `${formatDate(history[index]?.date ?? '')}  ${formatCompactEur(history[index]?.total ?? 0)}`}
          />
        </Panel>
      </StaggerItem>

      <StaggerItem className="xl:col-span-3">
        <MiniChart
          label="P&L du desk"
          value={formatCompactEur(last?.dayPnl ?? 0)}
          values={intraday.map((point) => point.dayPnl)}
          points={intraday}
          format={(value) => formatCompactEur(value)}
        />
      </StaggerItem>
      <StaggerItem className="xl:col-span-3">
        <MiniChart
          label="Panier actions"
          value={formatPercent(last?.equityBasketPercent ?? 0)}
          values={intraday.map((point) => point.equityBasketPercent)}
          points={intraday}
          format={(value) => formatPercent(value)}
        />
      </StaggerItem>
      <StaggerItem className="xl:col-span-3">
        <MiniChart
          label="EUR/USD"
          value={formatPrice(eurUsd?.last, 'Fx')}
          change={<span className={cx('num text-xs', trendClass(eurUsd?.changePercent))}>{formatPercent(eurUsd?.changePercent)}</span>}
          values={intraday.map((point) => point.eurUsd)}
          points={intraday}
          format={(value) => formatPrice(value, 'Fx')}
        />
      </StaggerItem>
      <StaggerItem className="xl:col-span-3">
        <MiniChart
          label="Taux 10 ans"
          value={formatYield(tenYear?.yieldPercent)}
          values={intraday.map((point) => point.tenYearPercent)}
          points={intraday}
          format={(value) => formatYield(value)}
        />
      </StaggerItem>

      <StaggerItem className="md:col-span-2 xl:col-span-5">
        <Panel className="h-full" title="Books" flush>
          <BooksTable summary={tick.summary} />
        </Panel>
      </StaggerItem>

      <StaggerItem className="xl:col-span-3">
        <Panel className="h-full" title="Exposition par classe d'actifs">
          {exposure.length === 0 ? (
            <EmptyState title="Aucune position" description="Ce book ne détient rien à cette date." />
          ) : (
            <BarList
              labelWidth="5.5rem"
              items={exposure.map((line) => ({
                key: line.assetClass,
                label: ASSET_CLASS_LABELS[line.assetClass],
                value: line.marketValue,
                valueText: formatCompactEur(line.marketValue, false),
              }))}
            />
          )}
        </Panel>
      </StaggerItem>

      <StaggerItem className="xl:col-span-4">
        <Panel className="h-full" title="Contributeurs du jour">
          {top.length === 0 && bottom.length === 0 ? (
            <EmptyState title="Aucun mouvement" description="Aucune position n'a varié pour l'instant." />
          ) : (
            <BarList
              tone="pnl"
              labelWidth="6rem"
              items={[...top, ...[...bottom].reverse()].map((item) => ({
                key: item.symbol,
                label: item.symbol,
                value: item.dayPnl,
                valueText: formatSignedEur(item.dayPnl, 0),
              }))}
            />
          )}
        </Panel>
      </StaggerItem>
    </StaggerGroup>
  );
}
