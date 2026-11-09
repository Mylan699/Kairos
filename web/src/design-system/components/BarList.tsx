import { cx } from '../cx';

export interface BarItem {
  key: string;
  label: string;
  value: number;
  valueText: string;
}

interface BarListProps {
  items: readonly BarItem[];
  /** `magnitude` draws every bar in the accent. `pnl` uses green and red, as these bars are results. */
  tone?: 'magnitude' | 'pnl';
  labelWidth?: string;
}

/** Horizontal bars measured from zero. When some values are negative, zero sits in the middle. */
export function BarList({ items, tone = 'magnitude', labelWidth = '7.5rem' }: BarListProps) {
  const largest = Math.max(...items.map((item) => Math.abs(item.value)), 1);
  const hasNegative = items.some((item) => item.value < 0);
  const origin = hasNegative ? 50 : 0;
  const halfSpan = hasNegative ? 50 : 100;

  return (
    <ul className="flex flex-col gap-2">
      {items.map((item) => {
        const length = (Math.abs(item.value) / largest) * halfSpan;
        const left = item.value < 0 ? origin - length : origin;
        const color = tone === 'magnitude' ? 'bg-accent' : item.value >= 0 ? 'bg-gain' : 'bg-loss';

        return (
          <li
            key={item.key}
            title={`${item.label} : ${item.valueText}`}
            className="grid items-center gap-2"
            style={{ gridTemplateColumns: `${labelWidth} minmax(2rem, 1fr) auto` }}
          >
            <span className="truncate text-xs text-ink-2">{item.label}</span>
            <span className="relative h-1.5 rounded-xs bg-surface-3">
              {hasNegative && <span className="absolute inset-y-0 left-1/2 w-px bg-line-strong" />}
              <span
                className={cx('absolute inset-y-0 rounded-xs transition-[left,width] duration-[var(--motion-slow)] ease-standard', color)}
                style={{ left: `${left}%`, width: `${length}%` }}
              />
            </span>
            <span className="num min-w-20 text-right text-xs text-ink">{item.valueText}</span>
          </li>
        );
      })}
    </ul>
  );
}
