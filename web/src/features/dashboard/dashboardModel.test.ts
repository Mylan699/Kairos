import { describe, expect, it } from 'vitest';
import type { AssetClass, PnlFigures, Position, PortfolioSummary } from '@/api/types';
import { contributors, exposureByAssetClass, figuresFor, positionsOfBook } from './dashboardModel';

const pnl = (day: number): PnlFigures => ({ realized: 0, unrealized: 0, priceEffect: 0, fxEffect: 0, total: day, day });

const position = (symbol: string, bookId: string, day: number, assetClass: AssetClass = 'Equity', marketValue = 100): Position => ({
  bookId,
  book: bookId,
  instrumentId: symbol,
  symbol,
  assetClass,
  sector: null,
  quantity: 1,
  averagePrice: 1,
  markPrice: 1,
  marketValue,
  pnl: pnl(day),
});

const summary: PortfolioSummary = {
  marketDate: '2020-03-16',
  books: [{ bookId: 'a', book: 'A', marketValue: 1, pnl: pnl(10) }],
  total: pnl(99),
};

describe('figuresFor', () => {
  it('returns the desk when no book is selected', () => {
    expect(figuresFor(summary, null).day).toBe(99);
  });

  it('returns the selected book', () => {
    expect(figuresFor(summary, 'a').day).toBe(10);
  });

  it('returns zeros for a book without positions', () => {
    expect(figuresFor(summary, 'unknown').day).toBe(0);
  });
});

describe('positionsOfBook', () => {
  const positions = [position('A', 'a', 1), position('B', 'b', 1)];

  it('keeps everything for the desk', () => {
    expect(positionsOfBook(positions, null)).toHaveLength(2);
  });

  it('keeps only the selected book', () => {
    expect(positionsOfBook(positions, 'a').map((item) => item.symbol)).toEqual(['A']);
  });
});

describe('exposureByAssetClass', () => {
  it('adds up market values in a stable order', () => {
    const lines = exposureByAssetClass([
      position('B', 'a', 0, 'Bond', 50),
      position('A', 'a', 0, 'Equity', 100),
      position('C', 'a', 0, 'Equity', -30),
    ]);

    expect(lines).toEqual([
      { assetClass: 'Equity', marketValue: 70 },
      { assetClass: 'Bond', marketValue: 50 },
    ]);
  });

  it('is empty without positions', () => {
    expect(exposureByAssetClass([])).toEqual([]);
  });
});

describe('contributors', () => {
  it('adds an instrument across books', () => {
    const { top } = contributors([position('A', 'a', 10), position('A', 'b', 5)], 5);

    expect(top).toEqual([{ symbol: 'A', dayPnl: 15 }]);
  });

  it('ranks gains from the largest and losses from the worst', () => {
    const { top, bottom } = contributors(
      [position('A', 'a', 1), position('B', 'a', 3), position('C', 'a', -1), position('D', 'a', -9)],
      5,
    );

    expect(top.map((item) => item.symbol)).toEqual(['B', 'A']);
    expect(bottom.map((item) => item.symbol)).toEqual(['D', 'C']);
  });

  it('respects the requested count and ignores flat positions', () => {
    const { top, bottom } = contributors([position('A', 'a', 1), position('B', 'a', 2), position('C', 'a', 0)], 1);

    expect(top.map((item) => item.symbol)).toEqual(['B']);
    expect(bottom).toEqual([]);
  });
});
