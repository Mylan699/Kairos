import { motion } from 'motion/react';
import { useState, type PointerEvent } from 'react';
import type { Curve } from '@/api/types';
import { formatYield } from '@/lib/format';
import { useElementWidth } from '@/lib/useElementWidth';
import { curvePath, xOf, yieldRange, yOf, type PlotArea } from './curveModel';

export interface CurveSeries {
  key: string;
  label: string;
  curve: Curve;
  stroke: string;
  dashed?: boolean;
}

const HEIGHT = 280;
const MARGIN = { left: 48, right: 16, top: 16, bottom: 28 };
const LIVE_LINE_WIDTH = 2;

/** Slightly shorter than a tick, so that the line keeps moving without ever lagging behind. */
const MORPH_SECONDS = 0.45;

function Chart({ series, width }: { series: CurveSeries[]; width: number }) {
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);
  const area: PlotArea = { width, height: HEIGHT, ...MARGIN };
  const reference = series[0]?.curve.points ?? [];
  const range = yieldRange(series.map((item) => item.curve.points));

  const onPointerMove = (event: PointerEvent<SVGSVGElement>) => {
    const bounds = event.currentTarget.getBoundingClientRect();
    const ratio = (event.clientX - bounds.left - area.left) / (width - area.left - area.right);
    setHoverIndex(Math.min(Math.max(Math.round(ratio * (reference.length - 1)), 0), reference.length - 1));
  };

  const hoverX = hoverIndex === null ? 0 : xOf(hoverIndex, reference.length, area);
  const tooltipOnLeft = hoverIndex !== null && hoverIndex > reference.length / 2;

  return (
    <div className="relative">
      <svg
        width={width}
        height={HEIGHT}
        role="img"
        aria-label="Courbe des taux du Trésor américain par maturité"
        onPointerMove={onPointerMove}
        onPointerLeave={() => setHoverIndex(null)}
      >
        {range.ticks.map((tick) => (
          <g key={tick}>
            <line x1={area.left} x2={width - area.right} y1={yOf(tick, range, area)} y2={yOf(tick, range, area)} stroke="var(--kairos-line-subtle)" />
            <text x={area.left - 8} y={yOf(tick, range, area) + 4} textAnchor="end" className="num fill-ink-3 text-2xs">
              {formatYield(tick, tick % 1 === 0 ? 0 : 2)}
            </text>
          </g>
        ))}

        {reference.map((point, index) => (
          <text key={point.tenor} x={xOf(index, reference.length, area)} y={HEIGHT - 8} textAnchor="middle" className="num fill-ink-3 text-2xs">
            {point.tenor}
          </text>
        ))}

        {hoverIndex !== null && (
          <line x1={hoverX} x2={hoverX} y1={area.top} y2={HEIGHT - area.bottom} stroke="var(--kairos-line-strong)" />
        )}

        {[...series].reverse().map((item) => (
          <motion.path
            key={item.key}
            fill="none"
            stroke={item.stroke}
            strokeWidth={item.key === 'live' ? LIVE_LINE_WIDTH : 1.5}
            strokeDasharray={item.dashed ? '4 4' : undefined}
            strokeLinecap="round"
            strokeLinejoin="round"
            initial={false}
            animate={{ d: curvePath(item.curve.points, range, area) }}
            transition={{ duration: MORPH_SECONDS, ease: 'linear' }}
          />
        ))}
      </svg>

      {hoverIndex !== null && (
        <div
          className="num pointer-events-none absolute top-3 rounded-sm border border-line-strong bg-surface-4 px-2 py-1 text-2xs"
          style={tooltipOnLeft ? { right: width - hoverX + 8 } : { left: hoverX + 8 }}
        >
          <p className="text-ink-2">{reference[hoverIndex]?.tenor}</p>
          {series.map((item) => (
            <p key={item.key} className="whitespace-nowrap text-ink">
              {item.label} : {formatYield(item.curve.points[hoverIndex]?.yieldPercent)}
            </p>
          ))}
        </div>
      )}
    </div>
  );
}

/** The curve deforms as the replay runs, against up to two past dates. */
export function YieldCurveChart({ series }: { series: CurveSeries[] }) {
  const [containerRef, width] = useElementWidth<HTMLDivElement>();
  const reference = series[0]?.curve.points ?? [];

  return (
    <div>
      <ul className="mb-2 flex flex-wrap gap-x-4 gap-y-1">
        {series.map((item) => (
          <li key={item.key} className="flex items-center gap-1.5 text-xs text-ink-2">
            <svg width={18} height={6} aria-hidden="true">
              <line x1={1} x2={17} y1={3} y2={3} stroke={item.stroke} strokeWidth={2} strokeDasharray={item.dashed ? '4 3' : undefined} strokeLinecap="round" />
            </svg>
            {item.label}
          </li>
        ))}
      </ul>

      <div ref={containerRef} style={{ height: HEIGHT }}>
        {width > 0 && reference.length > 0 && <Chart series={series} width={width} />}
      </div>

      {/* The wrapper is what hides it: a table ignores the height set on itself. */}
      <div className="sr-only">
        <table>
          <caption>Rendement par maturité</caption>
          <thead>
            <tr>
              <th scope="col">Maturité</th>
              {series.map((item) => <th key={item.key} scope="col">{item.label}</th>)}
            </tr>
          </thead>
          <tbody>
            {reference.map((point, index) => (
              <tr key={point.tenor}>
                <th scope="row">{point.tenor}</th>
                {series.map((item) => <td key={item.key}>{formatYield(item.curve.points[index]?.yieldPercent)}</td>)}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
