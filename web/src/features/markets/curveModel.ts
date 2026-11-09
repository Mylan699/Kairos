import type { Curve, CurvePoint } from '@/api/types';

export interface PlotArea {
  width: number;
  height: number;
  left: number;
  right: number;
  top: number;
  bottom: number;
}

export interface YieldRange {
  min: number;
  max: number;
  ticks: number[];
}

const NICE_STEPS = [0.1, 0.25, 0.5, 1, 2, 5];
const TARGET_TICK_COUNT = 4;

/** A readable vertical axis covering every curve on the chart. */
export function yieldRange(curves: ReadonlyArray<readonly CurvePoint[]>): YieldRange {
  const yields = curves.flat().map((point) => point.yieldPercent);
  const lowest = Math.min(...yields);
  const highest = Math.max(...yields);
  const rawStep = (highest - lowest || 1) / TARGET_TICK_COUNT;
  const step = NICE_STEPS.find((candidate) => candidate >= rawStep) ?? 10;

  const min = Math.floor(lowest / step) * step;
  const max = Math.ceil(highest / step) * step;
  const ticks: number[] = [];

  for (let tick = min; tick <= max + step / 2; tick += step) {
    ticks.push(Number(tick.toFixed(4)));
  }

  return { min, max: Math.max(max, min + step), ticks };
}

/** Maturities are spaced evenly: on a true time scale, the short end would be unreadable. */
export const xOf = (index: number, count: number, area: PlotArea): number =>
  area.left + (index / Math.max(count - 1, 1)) * (area.width - area.left - area.right);

export const yOf = (yieldPercent: number, range: YieldRange, area: PlotArea): number =>
  area.top + (1 - (yieldPercent - range.min) / (range.max - range.min)) * (area.height - area.top - area.bottom);

/** Every path has the same number of points, which is what allows one curve to morph into the next. */
export function curvePath(points: readonly CurvePoint[], range: YieldRange, area: PlotArea): string {
  return points
    .map((point, index) => `${index === 0 ? 'M' : 'L'}${xOf(index, points.length, area).toFixed(1)} ${yOf(point.yieldPercent, range, area).toFixed(1)}`)
    .join(' ');
}

const TWO_YEARS = 24;
const TEN_YEARS = 120;

/** The gap between the ten-year and two-year yields, in basis points. Negative when the curve is inverted. */
export function slope2s10s(curve: Curve): number | null {
  const twoYear = curve.points.find((point) => point.months === TWO_YEARS);
  const tenYear = curve.points.find((point) => point.months === TEN_YEARS);

  return twoYear && tenYear ? (tenYear.yieldPercent - twoYear.yieldPercent) * 100 : null;
}

export interface ComparisonOption {
  value: string;
  label: string;
}

const RELATIVE_OFFSETS: ReadonlyArray<[number, string]> = [
  [1, 'Veille'],
  [21, 'Il y a 1 mois'],
  [63, 'Il y a 3 mois'],
  [252, 'Il y a 1 an'],
];

/** Dates that can be compared with today: real market days, taken from the history before it. */
export function comparisonOptions(
  marketDaysBefore: readonly string[],
  remarkableDays: ReadonlyArray<{ date: string; name: string }>,
  currentDate: string,
): ComparisonOption[] {
  const relative = RELATIVE_OFFSETS.flatMap(([offset, label]) => {
    const date = marketDaysBefore[marketDaysBefore.length - offset];
    return date ? [{ value: date, label }] : [];
  });

  const known = new Set(relative.map((option) => option.value));
  const remarkable = remarkableDays
    .filter((day) => day.date < currentDate && !known.has(day.date))
    .map((day) => ({ value: day.date, label: day.name }));

  return [...relative, ...remarkable];
}
