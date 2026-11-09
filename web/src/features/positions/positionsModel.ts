import type { AssetClass, Position } from '@/api/types';

export type AssetClassFilter = AssetClass | 'All';

export interface PositionFilters {
  bookId: string | null;
  assetClass: AssetClassFilter;
  openOnly: boolean;
}

export function filterPositions(positions: readonly Position[], filters: PositionFilters): Position[] {
  return positions.filter(
    (position) =>
      (filters.bookId === null || position.bookId === filters.bookId) &&
      (filters.assetClass === 'All' || position.assetClass === filters.assetClass) &&
      (!filters.openOnly || position.quantity !== 0),
  );
}

export interface BookDecomposition {
  bookId: string;
  book: string;
  priceEffect: number;
  fxEffect: number;
}

/** Sums the two sources of the euro result, book by book. */
export function decompositionByBook(positions: readonly Position[]): BookDecomposition[] {
  const books = new Map<string, BookDecomposition>();

  for (const position of positions) {
    const book = books.get(position.bookId) ?? { bookId: position.bookId, book: position.book, priceEffect: 0, fxEffect: 0 };
    book.priceEffect += position.pnl.priceEffect;
    book.fxEffect += position.pnl.fxEffect;
    books.set(position.bookId, book);
  }

  return [...books.values()].sort((a, b) => a.book.localeCompare(b.book));
}

export interface PnlTotals {
  unrealized: number;
  realized: number;
  priceEffect: number;
  fxEffect: number;
}

export function totals(positions: readonly Position[]): PnlTotals {
  return positions.reduce<PnlTotals>(
    (sum, { pnl }) => ({
      unrealized: sum.unrealized + pnl.unrealized,
      realized: sum.realized + pnl.realized,
      priceEffect: sum.priceEffect + pnl.priceEffect,
      fxEffect: sum.fxEffect + pnl.fxEffect,
    }),
    { unrealized: 0, realized: 0, priceEffect: 0, fxEffect: 0 },
  );
}
