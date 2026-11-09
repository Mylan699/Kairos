import { describe, expect, it } from 'vitest';
import type { AssetClass, Position } from '@/api/types';
import { decompositionByBook, filterPositions, totals } from './positionsModel';

const position = (id: string, bookId: string, assetClass: AssetClass, quantity: number, priceEffect = 0, fxEffect = 0): Position => ({
  bookId,
  book: `Book ${bookId}`,
  instrumentId: id,
  symbol: id,
  assetClass,
  sector: null,
  quantity,
  averagePrice: 1,
  markPrice: 1,
  marketValue: 1,
  pnl: { realized: 1, unrealized: 2, priceEffect, fxEffect, total: 3, day: 0 },
});

const positions = [position('a', 'x', 'Equity', 10), position('b', 'x', 'Fx', 0), position('c', 'y', 'Equity', -5)];
const ids = (list: Position[]) => list.map((item) => item.symbol);

describe('filterPositions', () => {
  it('can show everything', () => {
    expect(ids(filterPositions(positions, { bookId: null, assetClass: 'All', openOnly: false }))).toEqual(['a', 'b', 'c']);
  });

  it('hides flat positions when only open ones are wanted, and keeps shorts', () => {
    expect(ids(filterPositions(positions, { bookId: null, assetClass: 'All', openOnly: true }))).toEqual(['a', 'c']);
  });

  it('filters by book and asset class', () => {
    expect(ids(filterPositions(positions, { bookId: 'y', assetClass: 'All', openOnly: false }))).toEqual(['c']);
    expect(ids(filterPositions(positions, { bookId: null, assetClass: 'Fx', openOnly: false }))).toEqual(['b']);
  });
});

describe('decompositionByBook', () => {
  it('adds up the price and currency effects of each book', () => {
    const lines = decompositionByBook([
      position('a', 'x', 'Equity', 1, 10, -2),
      position('b', 'x', 'Equity', 1, 5, 1),
      position('c', 'y', 'Bond', 1, -3, 4),
    ]);

    expect(lines).toEqual([
      { bookId: 'x', book: 'Book x', priceEffect: 15, fxEffect: -1 },
      { bookId: 'y', book: 'Book y', priceEffect: -3, fxEffect: 4 },
    ]);
  });
});

describe('totals', () => {
  it('adds up every figure', () => {
    expect(totals([position('a', 'x', 'Equity', 1, 10, -2), position('b', 'x', 'Equity', 1, 5, 1)])).toEqual({
      unrealized: 4,
      realized: 2,
      priceEffect: 15,
      fxEffect: -1,
    });
  });

  it('is zero without positions', () => {
    expect(totals([])).toEqual({ unrealized: 0, realized: 0, priceEffect: 0, fxEffect: 0 });
  });
});
