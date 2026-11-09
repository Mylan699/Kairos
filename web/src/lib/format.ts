/**
 * Financial formatting, French conventions. Every figure on screen goes through here,
 * so that decimals, separators and signs are the same everywhere.
 */

export type AssetClass = 'Equity' | 'Fx' | 'Bond';

const LOCALE = 'fr-FR';
const TRUE_MINUS = '−';
const PLACEHOLDER = '—';

const PRICE_DECIMALS: Record<AssetClass, number> = { Equity: 2, Fx: 5, Bond: 3 };

const formatters = new Map<string, Intl.NumberFormat>();

function formatNumber(value: number, options: Intl.NumberFormatOptions): string {
  const key = JSON.stringify(options);
  let formatter = formatters.get(key);

  if (!formatter) {
    formatter = new Intl.NumberFormat(LOCALE, options);
    formatters.set(key, formatter);
  }

  return formatter.format(value).replace('-', TRUE_MINUS);
}

function fixed(decimals: number, signed: boolean): Intl.NumberFormatOptions {
  return {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
    signDisplay: signed ? 'exceptZero' : 'auto',
  };
}

const isMissing = (value: number | null | undefined): value is null | undefined =>
  value === null || value === undefined || Number.isNaN(value);

export function formatPrice(value: number | null | undefined, assetClass: AssetClass): string {
  return isMissing(value) ? PLACEHOLDER : formatNumber(value, fixed(PRICE_DECIMALS[assetClass], false));
}

/** Shares are whole numbers; EUR/USD and bonds are amounts of nominal. */
export function formatQuantity(value: number | null | undefined): string {
  return isMissing(value) ? PLACEHOLDER : formatNumber(value, { maximumFractionDigits: 0 });
}

export function formatEur(value: number | null | undefined, decimals = 2): string {
  return isMissing(value) ? PLACEHOLDER : `${formatNumber(value, fixed(decimals, false))} €`;
}

/** A result or a variation: always shows its sign. */
export function formatSignedEur(value: number | null | undefined, decimals = 2): string {
  return isMissing(value) ? PLACEHOLDER : `${formatNumber(value, fixed(decimals, true))} €`;
}

/** Large amounts for headlines: +31,4 M€. */
export function formatCompactEur(value: number | null | undefined, signed = true): string {
  if (isMissing(value)) {
    return PLACEHOLDER;
  }

  const magnitude = Math.abs(value);
  const [divisor, unit] =
    magnitude >= 1e9 ? [1e9, 'Md€'] : magnitude >= 1e6 ? [1e6, 'M€'] : magnitude >= 1e3 ? [1e3, 'k€'] : [1, '€'];

  return `${formatNumber(value / divisor, fixed(divisor === 1 ? 0 : 1, signed))} ${unit}`;
}

/** The value is already in percent: 1.25 means 1.25 %. */
export function formatPercent(value: number | null | undefined, decimals = 2): string {
  return isMissing(value) ? PLACEHOLDER : `${formatNumber(value, fixed(decimals, true))} %`;
}

/** A yield level, which has no sign to show. */
export function formatYield(value: number | null | undefined, decimals = 3): string {
  return isMissing(value) ? PLACEHOLDER : `${formatNumber(value, fixed(decimals, false))} %`;
}

export function formatBasisPoints(value: number | null | undefined, decimals = 0): string {
  return isMissing(value) ? PLACEHOLDER : `${formatNumber(value, fixed(decimals, true))} pb`;
}

/** Colour of a variation: green and red are reserved for this. */
export function trendClass(value: number | null | undefined): string {
  if (isMissing(value) || value === 0) {
    return 'text-ink-2';
  }

  return value > 0 ? 'text-gain' : 'text-loss';
}

/** Dates travel as yyyy-MM-dd and are never interpreted in the browser's time zone. */
function parseIsoDate(iso: string): Date {
  const [year = 0, month = 1, day = 1] = iso.split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day));
}

const shortDate = new Intl.DateTimeFormat(LOCALE, { day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'UTC' });
const longDate = new Intl.DateTimeFormat(LOCALE, { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' });

export const formatDate = (iso: string): string => shortDate.format(parseIsoDate(iso));

export const formatLongDate = (iso: string): string => longDate.format(parseIsoDate(iso));

/** Minutes since midnight UTC, shown as a clock time. */
export function formatMinute(minute: number): string {
  const hours = Math.floor(minute / 60);
  return `${String(hours).padStart(2, '0')}:${String(minute % 60).padStart(2, '0')}`;
}
