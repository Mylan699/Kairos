import { describe, expect, it } from 'vitest';
import {
  formatBasisPoints,
  formatCompactEur,
  formatDate,
  formatEur,
  formatLongDate,
  formatMinute,
  formatPercent,
  formatPrice,
  formatQuantity,
  formatSignedEur,
  formatYield,
  trendClass,
} from './format';

/** Intl uses non-breaking spaces: tests read better with normal ones. */
const plain = (text: string) => text.replace(/[  ]/g, ' ');

describe('prices', () => {
  it('uses the decimals of each asset class', () => {
    expect(formatPrice(158.3, 'Equity')).toBe('158,30');
    expect(formatPrice(1.0916, 'Fx')).toBe('1,09160');
    expect(formatPrice(98.14145, 'Bond')).toBe('98,141');
  });

  it('separates thousands', () => {
    expect(plain(formatPrice(1234.5, 'Equity'))).toBe('1 234,50');
  });

  it('shows a dash when there is no value', () => {
    expect(formatPrice(null, 'Equity')).toBe('—');
    expect(formatPrice(undefined, 'Fx')).toBe('—');
    expect(formatPrice(Number.NaN, 'Bond')).toBe('—');
  });
});

describe('quantities', () => {
  it('shows whole numbers with separators', () => {
    expect(plain(formatQuantity(1500000))).toBe('1 500 000');
    expect(plain(formatQuantity(-250))).toBe('−250');
  });
});

describe('amounts in euros', () => {
  it('has two decimals and the currency after the figure', () => {
    expect(plain(formatEur(1234.5))).toBe('1 234,50 €');
  });

  it('shows the sign of a result, with a true minus', () => {
    expect(plain(formatSignedEur(1234.5))).toBe('+1 234,50 €');
    expect(plain(formatSignedEur(-1234.5))).toBe('−1 234,50 €');
  });

  it('gives no sign to zero', () => {
    expect(plain(formatSignedEur(0))).toBe('0,00 €');
  });

  it('abbreviates large amounts', () => {
    expect(plain(formatCompactEur(-31404704))).toBe('−31,4 M€');
    expect(plain(formatCompactEur(2500))).toBe('+2,5 k€');
    expect(plain(formatCompactEur(1200000000))).toBe('+1,2 Md€');
    expect(plain(formatCompactEur(420))).toBe('+420 €');
  });

  it('can abbreviate without a sign', () => {
    expect(plain(formatCompactEur(2500000, false))).toBe('2,5 M€');
  });
});

describe('rates', () => {
  it('shows the sign of a percentage change', () => {
    expect(plain(formatPercent(1.25))).toBe('+1,25 %');
    expect(plain(formatPercent(-12.99))).toBe('−12,99 %');
  });

  it('shows a yield level without a sign', () => {
    expect(plain(formatYield(2.5))).toBe('2,500 %');
  });

  it('shows basis points with their sign', () => {
    expect(formatBasisPoints(19)).toBe('+19 pb');
    expect(formatBasisPoints(-21)).toBe('−21 pb');
  });
});

describe('colour of a variation', () => {
  it('is green for a rise, red for a fall and neutral otherwise', () => {
    expect(trendClass(1)).toBe('text-gain');
    expect(trendClass(-1)).toBe('text-loss');
    expect(trendClass(0)).toBe('text-ink-2');
    expect(trendClass(null)).toBe('text-ink-2');
  });
});

describe('dates and times', () => {
  it('reads an ISO date without shifting the day', () => {
    expect(formatDate('2016-06-24')).toBe('24/06/2016');
    expect(formatLongDate('2020-03-16')).toBe('Lundi 16 mars 2020');
  });

  it('turns minutes since midnight into a clock time', () => {
    expect(formatMinute(0)).toBe('00:00');
    expect(formatMinute(810)).toBe('13:30');
    expect(formatMinute(1439)).toBe('23:59');
  });
});
