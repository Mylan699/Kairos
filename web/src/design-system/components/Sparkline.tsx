import { motion, useReducedMotion } from 'motion/react';
import { useState, type PointerEvent } from 'react';
import { duration, ease } from '../motion';

interface SparklineProps {
  values: readonly number[];
  /** Describes the trend for screen readers, and is shown when no point is hovered. */
  label: string;
  /** Text shown for the hovered point. */
  formatPoint?: (index: number) => string;
  width?: number;
  height?: number;
}

const PADDING = 4;
const LAST_TICK_RADIUS = 2.5;

export function buildPoints(values: readonly number[], width: number, height: number): Array<[number, number]> {
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = max - min || 1;
  const stepX = values.length > 1 ? (width - PADDING * 2) / (values.length - 1) : 0;

  return values.map((value, index) => [
    PADDING + index * stepX,
    height - PADDING - ((value - min) / range) * (height - PADDING * 2),
  ]);
}

/** A trend on one line. Like the logo, it ends on a dot: the last tick. */
export function Sparkline({ values, label, formatPoint, width = 120, height = 32 }: SparklineProps) {
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);
  const reducedMotion = useReducedMotion();

  if (values.length === 0) {
    return <div style={{ width, height }} />;
  }

  const points = buildPoints(values, width, height);
  const path = points.map(([x, y], index) => `${index === 0 ? 'M' : 'L'}${x.toFixed(1)} ${y.toFixed(1)}`).join(' ');
  const shownIndex = hoverIndex ?? points.length - 1;
  const [dotX = 0, dotY = 0] = points[shownIndex] ?? [];

  const onPointerMove = (event: PointerEvent<SVGSVGElement>) => {
    const bounds = event.currentTarget.getBoundingClientRect();
    const ratio = (event.clientX - bounds.left) / bounds.width;
    setHoverIndex(Math.min(Math.max(Math.round(ratio * (values.length - 1)), 0), values.length - 1));
  };

  return (
    <div className="relative inline-block">
      <svg
        width={width}
        height={height}
        viewBox={`0 0 ${width} ${height}`}
        role="img"
        aria-label={label}
        onPointerMove={onPointerMove}
        onPointerLeave={() => setHoverIndex(null)}
      >
        <motion.path
          d={path}
          fill="none"
          stroke="var(--kairos-accent)"
          strokeWidth={1.5}
          strokeLinecap="round"
          strokeLinejoin="round"
          initial={reducedMotion ? false : { pathLength: 0 }}
          animate={{ pathLength: 1 }}
          transition={{ duration: duration.draw, ease: ease.standard }}
        />
        <circle cx={dotX} cy={dotY} r={LAST_TICK_RADIUS} fill="var(--kairos-accent)" />
      </svg>
      {hoverIndex !== null && formatPoint && (
        <span className="num pointer-events-none absolute -top-6 right-0 whitespace-nowrap rounded-sm border border-line-strong bg-surface-4 px-1.5 text-2xs text-ink">
          {formatPoint(hoverIndex)}
        </span>
      )}
    </div>
  );
}
