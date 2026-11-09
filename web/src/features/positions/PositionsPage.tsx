import type { ColDef, ICellRendererParams, ValueFormatterParams } from 'ag-grid-community';
import { useMemo, useState } from 'react';
import { useBookFilter } from '@/app/BookFilter';
import { usePositions } from '@/api/queries';
import type { Position } from '@/api/types';
import { BarList, CONTROL_CLASSES, cx, EmptyState, ErrorState, FlashValue, Panel, SearchIcon, SegmentedControl, Skeleton } from '@/design-system';
import { KairosGrid } from '@/design-system/grid/KairosGrid';
import { formatCompactEur, formatPrice, formatQuantity, formatSignedEur, trendClass } from '@/lib/format';
import { ASSET_CLASS_LABELS, sectorLabel } from '@/lib/labels';
import { useElementWidth } from '@/lib/useElementWidth';
import { useLive, useLiveTopic } from '@/realtime/live';
import { decompositionByBook, filterPositions, totals, type AssetClassFilter } from './positionsModel';

const NARROW_WIDTH = 720;
const COMFORTABLE_WIDTH = 1400;
const NUMBER_CELL = 'num text-right';
const NUMBER_HEADER = 'ag-right-aligned-header';

const ASSET_SEGMENTS = [
  { value: 'All', label: 'Tout' },
  { value: 'Equity', label: ASSET_CLASS_LABELS.Equity },
  { value: 'Fx', label: ASSET_CLASS_LABELS.Fx },
  { value: 'Bond', label: ASSET_CLASS_LABELS.Bond },
] as const satisfies ReadonlyArray<{ value: AssetClassFilter; label: string }>;

const SCOPE_SEGMENTS = [
  { value: 'open', label: 'Ouvertes' },
  { value: 'all', label: 'Toutes' },
] as const;

/** A price that flashes when it moves, without re-rendering the rest of the grid. */
function PriceCell({ value, data }: ICellRendererParams<Position, number>) {
  if (value === undefined || value === null || !data) {
    return null;
  }

  return <FlashValue value={value}>{formatPrice(value, data.assetClass)}</FlashValue>;
}

// Results all move together: flashing them would only add noise, unlike a single price.
function DayPnlCell({ value }: ICellRendererParams<Position, number>) {
  return value === undefined || value === null ? null : <span className={trendClass(value)}>{formatSignedEur(value, 0)}</span>;
}

/** Cumulative results are large: the compact form keeps every column readable. */
function CumulativePnlCell({ value }: ICellRendererParams<Position, number>) {
  return value === undefined || value === null ? null : <span className={trendClass(value)}>{formatCompactEur(value)}</span>;
}

function buildColumns(isNarrow: boolean, isComfortable: boolean): Array<ColDef<Position>> {
  const pnlColumn = (
    headerName: string,
    field: 'pnl.day' | 'pnl.unrealized' | 'pnl.realized' | 'pnl.priceEffect' | 'pnl.fxEffect',
    width: number,
    hide = false,
  ): ColDef<Position> => ({
    field,
    headerName,
    width,
    hide,
    cellClass: NUMBER_CELL,
    headerClass: NUMBER_HEADER,
    cellRenderer: field === 'pnl.day' ? DayPnlCell : CumulativePnlCell,
  });

  return [
    { field: 'symbol', headerName: 'Instrument', width: 116, cellClass: 'num', pinned: 'left' },
    { field: 'book', headerName: 'Book', flex: 1, minWidth: 116, hide: isNarrow },
    { field: 'assetClass', headerName: 'Classe', width: 84, hide: isNarrow || !isComfortable, valueFormatter: ({ value }: ValueFormatterParams<Position, Position['assetClass']>) => (value ? ASSET_CLASS_LABELS[value] : '') },
    { field: 'sector', headerName: 'Secteur', flex: 1, minWidth: 104, hide: isNarrow, valueFormatter: ({ value }: ValueFormatterParams<Position, string | null>) => (value ? sectorLabel(value) : '—') },
    { field: 'quantity', headerName: 'Quantité', width: 96, cellClass: NUMBER_CELL, headerClass: NUMBER_HEADER, valueFormatter: ({ value }: ValueFormatterParams<Position, number>) => formatQuantity(value) },
    { field: 'averagePrice', headerName: 'Coût moyen', width: 88, cellClass: NUMBER_CELL, headerClass: NUMBER_HEADER, hide: isNarrow, valueFormatter: ({ value, data }: ValueFormatterParams<Position, number>) => (data ? formatPrice(value, data.assetClass) : '') },
    { field: 'markPrice', headerName: 'Cours', width: 88, cellClass: NUMBER_CELL, headerClass: NUMBER_HEADER, cellRenderer: PriceCell },
    { field: 'marketValue', headerName: 'Valeur EUR', width: 96, cellClass: NUMBER_CELL, headerClass: NUMBER_HEADER, hide: isNarrow, valueFormatter: ({ value }: ValueFormatterParams<Position, number>) => formatCompactEur(value, false) },
    pnlColumn('Jour', 'pnl.day', 108),
    pnlColumn('Latent', 'pnl.unrealized', 100, isNarrow),
    pnlColumn('Réalisé', 'pnl.realized', 100, isNarrow),
    pnlColumn('Effet prix', 'pnl.priceEffect', 104, isNarrow),
    pnlColumn('Effet change', 'pnl.fxEffect', 112, isNarrow),
  ];
}

const Figure = ({ label, value }: { label: string; value: number }) => (
  <div className="flex flex-col gap-0.5">
    <dt className="label">{label}</dt>
    <dd className={cx('num text-lg', trendClass(value))}>{formatCompactEur(value)}</dd>
  </div>
);

function PositionsSkeleton() {
  return (
    <div className="flex flex-col gap-2 md:gap-3" aria-busy="true" aria-label="Chargement des positions">
      <div className="grid gap-2 md:grid-cols-3 md:gap-3">
        <Skeleton className="h-36" />
        <Skeleton className="h-36" />
        <Skeleton className="h-36" />
      </div>
      <Skeleton className="h-[60vh]" />
    </div>
  );
}

export default function PositionsPage() {
  useLiveTopic('positions');

  const { positions: livePositions } = useLive();
  const query = usePositions();
  const { bookId } = useBookFilter();
  const [assetClass, setAssetClass] = useState<AssetClassFilter>('All');
  const [scope, setScope] = useState<'open' | 'all'>('open');
  const [search, setSearch] = useState('');
  const [containerRef, width] = useElementWidth<HTMLDivElement>();

  const positions = livePositions ?? query.data;
  const isNarrow = width > 0 && width < NARROW_WIDTH;
  const isComfortable = width >= COMFORTABLE_WIDTH;
  const columns = useMemo(() => buildColumns(isNarrow, isComfortable), [isNarrow, isComfortable]);

  const rows = useMemo(
    () => filterPositions(positions ?? [], { bookId, assetClass, openOnly: scope === 'open' }),
    [positions, bookId, assetClass, scope],
  );

  if (query.isError && !positions) {
    return <ErrorState onRetry={() => void query.refetch()} />;
  }

  if (!positions) {
    return <PositionsSkeleton />;
  }

  const inBook = filterPositions(positions, { bookId, assetClass: 'All', openOnly: false });
  const sums = totals(inBook);
  const decomposition = decompositionByBook(inBook);

  return (
    <div className="flex flex-col gap-2 md:h-[calc(100dvh-4.5rem)] md:gap-3">
      <div className="grid shrink-0 gap-2 md:grid-cols-3 md:gap-3">
        <Panel title="Résultat en euros">
          <dl className="grid grid-cols-2 gap-3">
            <Figure label="Latent" value={sums.unrealized} />
            <Figure label="Réalisé" value={sums.realized} />
            <Figure label="Effet prix" value={sums.priceEffect} />
            <Figure label="Effet change" value={sums.fxEffect} />
          </dl>
        </Panel>
        <Panel title="Effet prix par book">
          <BarList
            tone="pnl"
            labelWidth="8.5rem"
            items={decomposition.map((line) => ({ key: line.bookId, label: line.book, value: line.priceEffect, valueText: formatCompactEur(line.priceEffect) }))}
          />
        </Panel>
        <Panel title="Effet change par book">
          <BarList
            tone="pnl"
            labelWidth="8.5rem"
            items={decomposition.map((line) => ({ key: line.bookId, label: line.book, value: line.fxEffect, valueText: formatCompactEur(line.fxEffect) }))}
          />
        </Panel>
      </div>

      <Panel flush className="min-h-96 flex-1" title="Positions" aside={<span className="num text-xs text-ink-2">{formatQuantity(rows.length)} lignes</span>}>
        <div className="flex h-[60vh] flex-col md:h-full">
          <div className="flex flex-wrap items-center gap-2 border-b border-line-subtle p-2">
            <div className="relative w-full sm:w-56">
              <SearchIcon className="pointer-events-none absolute left-2.5 top-2 text-ink-3" />
              <input
                type="search"
                aria-label="Rechercher une position"
                placeholder="Rechercher"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                className={cx(CONTROL_CLASSES, 'pl-8')}
              />
            </div>
            <SegmentedControl label="Classe d'actifs" value={assetClass} segments={ASSET_SEGMENTS} onChange={setAssetClass} />
            <SegmentedControl label="Portée" value={scope} segments={SCOPE_SEGMENTS} onChange={setScope} />
          </div>
          <div ref={containerRef} className="min-h-0 flex-1">
            {rows.length === 0 ? (
              <EmptyState title="Aucune position" description="Aucune position ne correspond à ces filtres à cette date." />
            ) : (
              <KairosGrid<Position>
                label="Positions par instrument"
                rowData={rows}
                columnDefs={columns}
                quickFilterText={search}
                getRowId={({ data }) => `${data.bookId}:${data.instrumentId}`}
              />
            )}
          </div>
        </div>
      </Panel>
    </div>
  );
}
