import { describe, expect, it } from 'vitest';
import type { Curve, CurvePoint } from '@/api/types';
import { comparisonOptions, curvePath, slope2s10s, xOf, yieldRange, yOf, type PlotArea } from './curveModel';
import { minuteToTimestamp } from './PriceChart';

const point = (tenor: string, months: number, yieldPercent: number): CurvePoint => ({ tenor, months, yieldPercent });

const area: PlotArea = { width: 140, height: 120, left: 20, right: 20, top: 10, bottom: 10 };

describe('yieldRange', () => {
  it('covers every curve with round bounds', () => {
    const range = yieldRange([[point('2Y', 24, 1.1)], [point('10Y', 120, 2.9)]]);

    expect(range.min).toBe(1);
    expect(range.max).toBe(3);
    expect(range.ticks).toEqual([1, 1.5, 2, 2.5, 3]);
  });

  it('stays usable for a flat curve', () => {
    const range = yieldRange([[point('2Y', 24, 2), point('10Y', 120, 2)]]);

    expect(range.max).toBeGreaterThan(range.min);
  });
});

describe('scales', () => {
  const range = { min: 0, max: 10, ticks: [] };

  it('spreads maturities evenly across the plot', () => {
    expect(xOf(0, 3, area)).toBe(20);
    expect(xOf(1, 3, area)).toBe(70);
    expect(xOf(2, 3, area)).toBe(120);
  });

  it('puts high yields at the top', () => {
    expect(yOf(10, range, area)).toBe(10);
    expect(yOf(0, range, area)).toBe(110);
  });

  it('draws a path through every point', () => {
    const path = curvePath([point('2Y', 24, 0), point('10Y', 120, 10)], range, area);

    expect(path).toBe('M20.0 110.0 L120.0 10.0');
  });
});

describe('slope2s10s', () => {
  const curve = (points: CurvePoint[]): Curve => ({ date: '2020-03-16', points });

  it('is the ten-year minus the two-year, in basis points', () => {
    expect(slope2s10s(curve([point('2Y', 24, 1.5), point('10Y', 120, 2)]))).toBeCloseTo(50);
  });

  it('is negative when the curve is inverted', () => {
    expect(slope2s10s(curve([point('2Y', 24, 2), point('10Y', 120, 1.75)]))).toBeCloseTo(-25);
  });

  it('is unknown when a maturity is missing', () => {
    expect(slope2s10s(curve([point('10Y', 120, 2)]))).toBeNull();
  });
});

describe('comparisonOptions', () => {
  const days = Array.from({ length: 300 }, (_, index) => `day-${String(index).padStart(3, '0')}`);

  it('offers real market days at the usual distances', () => {
    const options = comparisonOptions(days, [], 'z');

    expect(options.map((option) => option.label)).toEqual(['Veille', 'Il y a 1 mois', 'Il y a 3 mois', 'Il y a 1 an']);
    expect(options[0]?.value).toBe('day-299');
    expect(options[3]?.value).toBe('day-048');
  });

  it('leaves out distances longer than the available history', () => {
    expect(comparisonOptions(days.slice(0, 30), [], 'z').map((option) => option.label)).toEqual(['Veille', 'Il y a 1 mois']);
  });

  it('adds past remarkable days, never future ones', () => {
    const options = comparisonOptions([], [{ date: '2016-06-24', name: 'Brexit' }, { date: '2020-03-16', name: 'Krach' }], '2018-01-01');

    expect(options).toEqual([{ value: '2016-06-24', label: 'Brexit' }]);
  });
});

describe('minuteToTimestamp', () => {
  it('counts seconds from midnight UTC', () => {
    expect(minuteToTimestamp('1970-01-02', 1)).toBe(86_460);
  });
});
