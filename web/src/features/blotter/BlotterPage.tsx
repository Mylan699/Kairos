import type { ColDef, ICellRendererParams, RowClassParams, ValueFormatterParams } from 'ag-grid-community';
import type { AgGridReact } from 'ag-grid-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useBookFilter } from '@/app/BookFilter';
import { useTrades } from '@/api/queries';
import type { Trade } from '@/api/types';
import { Button, CONTROL_CLASSES, cx, DownloadIcon, EmptyState, ErrorState, Panel, SearchIcon, SegmentedControl, Select, Skeleton } from '@/design-system';
import { KairosGrid } from '@/design-system/grid/KairosGrid';
import { formatDate, formatPrice, formatQuantity } from '@/lib/format';
import { ASSET_CLASS_LABELS, SIDE_LABELS, STATUS_LABELS } from '@/lib/labels';
import { useElementWidth } from '@/lib/useElementWidth';
import { useLive, useLiveStore } from '@/realtime/live';
import { filterTrades, sortNewestFirst, type AssetClassFilter, type StatusFilter } from './blotterModel';
import { StatusBadge } from './StatusBadge';
import { TradeDetailDrawer } from './TradeDetailDrawer';

const NARROW_WIDTH = 720;
const HIGHLIGHT_MILLISECONDS = 1_200;
const NEW_ROW_CLASS = 'kairos-row-new';

const STATUS_SEGMENTS = [
  { value: 'All', label: 'Tous' },
  { value: 'New', label: STATUS_LABELS.New },
  { value: 'Validated', label: STATUS_LABELS.Validated },
  { value: 'Settled', label: STATUS_LABELS.Settled },
  { value: 'Cancelled', label: STATUS_LABELS.Cancelled },
] as const satisfies ReadonlyArray<{ value: StatusFilter; label: string }>;

const ASSET_OPTIONS = [
  { value: 'All', label: 'Toutes classes' },
  { value: 'Equity', label: ASSET_CLASS_LABELS.Equity },
  { value: 'Fx', label: ASSET_CLASS_LABELS.Fx },
  { value: 'Bond', label: ASSET_CLASS_LABELS.Bond },
] as const satisfies ReadonlyArray<{ value: AssetClassFilter; label: string }>;

const NUMBER_CELL = 'num text-right';
const NUMBER_HEADER = 'ag-right-aligned-header';

const StatusCell = ({ data }: ICellRendererParams<Trade>) => (data ? <StatusBadge status={data.status} /> : null);

function buildColumns(isNarrow: boolean): Array<ColDef<Trade>> {
  return [
    { field: 'tradeDate', headerName: 'Date', width: 96, cellClass: 'num', valueFormatter: ({ value }: ValueFormatterParams<Trade, string>) => (value ? formatDate(value) : '') },
    { field: 'symbol', headerName: 'Instrument', width: 116, cellClass: 'num' },
    { field: 'assetClass', headerName: 'Classe', width: 92, hide: isNarrow, valueFormatter: ({ value }: ValueFormatterParams<Trade, Trade['assetClass']>) => (value ? ASSET_CLASS_LABELS[value] : '') },
    { field: 'side', headerName: 'Sens', width: 76, valueFormatter: ({ value }: ValueFormatterParams<Trade, Trade['side']>) => (value ? SIDE_LABELS[value] : '') },
    { field: 'quantity', headerName: 'Quantité', width: 112, cellClass: NUMBER_CELL, headerClass: NUMBER_HEADER, valueFormatter: ({ value }: ValueFormatterParams<Trade, number>) => formatQuantity(value) },
    { field: 'price', headerName: 'Prix', width: 92, cellClass: NUMBER_CELL, headerClass: NUMBER_HEADER, hide: isNarrow, valueFormatter: ({ value, data }: ValueFormatterParams<Trade, number>) => (data ? formatPrice(value, data.assetClass) : '') },
    { field: 'book', headerName: 'Book', flex: 1.2, minWidth: 130, hide: isNarrow },
    { field: 'counterparty', headerName: 'Contrepartie', flex: 1, minWidth: 104, hide: isNarrow },
    { field: 'trader', headerName: 'Trader', flex: 1, minWidth: 112, hide: isNarrow },
    { field: 'valueDate', headerName: 'Valeur', width: 96, cellClass: 'num', hide: isNarrow, valueFormatter: ({ value }: ValueFormatterParams<Trade, string>) => (value ? formatDate(value) : '') },
    { field: 'status', headerName: 'Statut', width: 92, cellRenderer: StatusCell },
    { field: 'version', headerName: 'V.', width: 52, cellClass: NUMBER_CELL, headerClass: NUMBER_HEADER, hide: isNarrow },
  ];
}

function BlotterSkeleton() {
  return (
    <div className="flex flex-col gap-px p-3" aria-busy="true" aria-label="Chargement du blotter">
      <Skeleton className="mb-2 h-5 w-full" />
      {Array.from({ length: 14 }, (_, index) => <Skeleton key={index} className="my-1.5 h-4 w-full" />)}
    </div>
  );
}

export default function BlotterPage() {
  const { data, isError, refetch } = useTrades();
  const { tick } = useLive();
  const store = useLiveStore();
  const { bookId } = useBookFilter();
  const [searchParams, setSearchParams] = useSearchParams();
  const [status, setStatus] = useState<StatusFilter>('All');
  const [assetClass, setAssetClass] = useState<AssetClassFilter>('All');
  const [search, setSearch] = useState('');
  const [containerRef, width] = useElementWidth<HTMLDivElement>();

  const gridRef = useRef<AgGridReact<Trade>>(null);
  const justChanged = useRef(new Set<string>());

  // A changed trade slides into the grid and stays highlighted for a moment.
  useEffect(
    () =>
      store.onTradeChanged((trade) => {
        justChanged.current.add(trade.id);
        window.setTimeout(() => justChanged.current.delete(trade.id), HIGHLIGHT_MILLISECONDS);
      }),
    [store],
  );

  const rows = useMemo(
    () => sortNewestFirst(filterTrades(data ?? [], { status, assetClass, bookId })),
    [data, status, assetClass, bookId],
  );

  const isNarrow = width > 0 && width < NARROW_WIDTH;
  const columns = useMemo(() => buildColumns(isNarrow), [isNarrow]);
  const rowClassRules = useMemo(
    () => ({ [NEW_ROW_CLASS]: (params: RowClassParams<Trade>) => (params.data ? justChanged.current.has(params.data.id) : false) }),
    [],
  );

  const selectedTradeId = searchParams.get('trade');

  const setSelectedTrade = (id: string | null) =>
    setSearchParams((previous) => {
      const next = new URLSearchParams(previous);
      if (id) {
        next.set('trade', id);
      } else {
        next.delete('trade');
      }
      return next;
    });

  const exportCsv = () =>
    gridRef.current?.api.exportDataAsCsv({
      fileName: `kairos-blotter-${tick?.status.date ?? 'export'}.csv`,
      columnSeparator: ';',
    });

  return (
    <div className="flex h-[calc(100dvh-8.5rem)] min-h-96 flex-col md:h-[calc(100dvh-4.5rem)]">
      <Panel
        flush
        className="h-full"
        title="Blotter"
        aside={<span className="num text-xs text-ink-2">{formatQuantity(rows.length)} trades</span>}
      >
        <div className="flex h-full flex-col">
          <div className="flex flex-wrap items-center gap-2 border-b border-line-subtle p-2">
            <div className="relative w-full sm:w-56">
              <SearchIcon className="pointer-events-none absolute left-2.5 top-2 text-ink-3" />
              <input
                type="search"
                aria-label="Rechercher dans le blotter"
                placeholder="Rechercher"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                className={cx(CONTROL_CLASSES, 'pl-8')}
              />
            </div>
            <div className="max-w-full overflow-x-auto">
              <SegmentedControl label="Statut" value={status} segments={STATUS_SEGMENTS} onChange={setStatus} />
            </div>
            <div className="w-40">
              <Select label="Classe d'actifs" hideLabel value={assetClass} options={ASSET_OPTIONS} onChange={setAssetClass} />
            </div>
            <div className="flex-1" />
            <Button onClick={exportCsv} disabled={rows.length === 0}>
              <DownloadIcon /> Export CSV
            </Button>
          </div>

          <div ref={containerRef} className="min-h-0 flex-1">
            {isError ? (
              <ErrorState onRetry={() => void refetch()} />
            ) : !data ? (
              <BlotterSkeleton />
            ) : rows.length === 0 ? (
              <EmptyState title="Aucun trade" description="Aucun trade ne correspond à ces filtres à cette date." />
            ) : (
              <KairosGrid<Trade>
                gridRef={gridRef}
                label="Liste des trades"
                rowData={rows}
                columnDefs={columns}
                getRowId={({ data: trade }) => trade.id}
                quickFilterText={search}
                rowClassRules={rowClassRules}
                onRowClicked={({ data: trade }) => trade && setSelectedTrade(trade.id)}
              />
            )}
          </div>
        </div>
      </Panel>

      <TradeDetailDrawer tradeId={selectedTradeId} onClose={() => setSelectedTrade(null)} />
    </div>
  );
}
