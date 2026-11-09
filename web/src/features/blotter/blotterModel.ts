import type { AssetClass, Trade, TradeStatus } from '@/api/types';

export type StatusFilter = TradeStatus | 'All';
export type AssetClassFilter = AssetClass | 'All';

export interface BlotterFilters {
  status: StatusFilter;
  assetClass: AssetClassFilter;
  bookId: string | null;
}

export function filterTrades(trades: readonly Trade[], filters: BlotterFilters): Trade[] {
  return trades.filter(
    (trade) =>
      (filters.status === 'All' || trade.status === filters.status) &&
      (filters.assetClass === 'All' || trade.assetClass === filters.assetClass) &&
      (filters.bookId === null || trade.bookId === filters.bookId),
  );
}

/** Newest trade date first and, within a date, the last one touched first. */
export function sortNewestFirst(trades: readonly Trade[]): Trade[] {
  return [...trades].sort(
    (a, b) => b.tradeDate.localeCompare(a.tradeDate) || b.updatedAt.localeCompare(a.updatedAt),
  );
}

export type TradeAction = 'validate' | 'settle' | 'amend' | 'cancel';

/** Which buttons to offer. The server enforces the same rules with its state machine. */
export function availableActions(status: TradeStatus): TradeAction[] {
  switch (status) {
    case 'New':
      return ['validate', 'amend', 'cancel'];
    case 'Validated':
      return ['settle', 'amend', 'cancel'];
    default:
      return [];
  }
}
