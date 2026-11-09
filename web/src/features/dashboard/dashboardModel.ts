import type { AssetClass, PnlFigures, Position, PortfolioSummary } from '@/api/types';

const NO_FIGURES: PnlFigures = { realized: 0, unrealized: 0, priceEffect: 0, fxEffect: 0, total: 0, day: 0 };

/** The figures of one book, or of the whole desk when none is selected. */
export function figuresFor(summary: PortfolioSummary, bookId: string | null): PnlFigures {
  return bookId ? summary.books.find((book) => book.bookId === bookId)?.pnl ?? NO_FIGURES : summary.total;
}

export const positionsOfBook = (positions: readonly Position[], bookId: string | null): readonly Position[] =>
  bookId ? positions.filter((position) => position.bookId === bookId) : positions;

export interface ExposureLine {
  assetClass: AssetClass;
  marketValue: number;
}

export function exposureByAssetClass(positions: readonly Position[]): ExposureLine[] {
  const order: AssetClass[] = ['Equity', 'Fx', 'Bond'];

  return order
    .map((assetClass) => ({
      assetClass,
      marketValue: positions
        .filter((position) => position.assetClass === assetClass)
        .reduce((total, position) => total + position.marketValue, 0),
    }))
    .filter((line) => positions.some((position) => position.assetClass === line.assetClass));
}

export interface Contributor {
  symbol: string;
  dayPnl: number;
}

/** Adds up an instrument across books, then returns the largest gains and the largest losses. */
export function contributors(positions: readonly Position[], count: number): { top: Contributor[]; bottom: Contributor[] } {
  const totals = new Map<string, number>();

  for (const position of positions) {
    totals.set(position.symbol, (totals.get(position.symbol) ?? 0) + position.pnl.day);
  }

  const all = [...totals].map(([symbol, dayPnl]) => ({ symbol, dayPnl }));

  return {
    top: all.filter((item) => item.dayPnl > 0).sort((a, b) => b.dayPnl - a.dayPnl).slice(0, count),
    bottom: all.filter((item) => item.dayPnl < 0).sort((a, b) => a.dayPnl - b.dayPnl).slice(0, count),
  };
}
