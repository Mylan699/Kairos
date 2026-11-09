import { useState, type KeyboardEvent, type PointerEvent } from 'react';
import type { IntradayPoint } from '@/api/types';
import { cx } from '@/design-system';
import { formatMinute } from '@/lib/format';
import { useElementWidth } from '@/lib/useElementWidth';
import { LAST_MINUTE, keyboardTarget, minuteFromPosition, positionOfMinute, seriesPath } from './replayModel';

const HEIGHT = 96;
const AXIS_HEIGHT = 20;
const PLOT_TOP = 8;
const HOUR_MARKS = [0, 3, 6, 9, 12, 15, 18, 21, 24];
const NARROW_WIDTH = 560;

interface ReplayTimelineProps {
  minute: number;
  sessionOpenMinute: number;
  sessionCloseMinute: number;
  points: readonly IntradayPoint[];
  onSeek: (minute: number) => void;
}

export function ReplayTimeline({ minute, sessionOpenMinute, sessionCloseMinute, points, onSeek }: ReplayTimelineProps) {
  const [containerRef, width] = useElementWidth<HTMLDivElement>();
  const [draggedMinute, setDraggedMinute] = useState<number | null>(null);

  const shownMinute = draggedMinute ?? minute;
  const plotHeight = HEIGHT - AXIS_HEIGHT;
  const cursorX = positionOfMinute(shownMinute, width);

  const minuteAt = (event: PointerEvent<HTMLDivElement>) => {
    const bounds = event.currentTarget.getBoundingClientRect();
    return minuteFromPosition(event.clientX - bounds.left, bounds.width);
  };

  const onPointerDown = (event: PointerEvent<HTMLDivElement>) => {
    event.currentTarget.setPointerCapture?.(event.pointerId);
    setDraggedMinute(minuteAt(event));
  };

  const onPointerMove = (event: PointerEvent<HTMLDivElement>) => {
    if (draggedMinute !== null) {
      setDraggedMinute(minuteAt(event));
    }
  };

  const onPointerUp = (event: PointerEvent<HTMLDivElement>) => {
    if (draggedMinute !== null) {
      onSeek(minuteAt(event));
      setDraggedMinute(null);
    }
  };

  const onKeyDown = (event: KeyboardEvent) => {
    const target = keyboardTarget(event.key, minute);
    if (target !== null) {
      event.preventDefault();
      onSeek(target);
    }
  };

  return (
    <div
      ref={containerRef}
      role="slider"
      tabIndex={0}
      aria-label="Heure du rejeu"
      aria-valuemin={0}
      aria-valuemax={LAST_MINUTE}
      aria-valuenow={shownMinute}
      aria-valuetext={`${formatMinute(shownMinute)} UTC`}
      onPointerDown={onPointerDown}
      onPointerMove={onPointerMove}
      onPointerUp={onPointerUp}
      onKeyDown={onKeyDown}
      className={cx('relative touch-none select-none rounded-sm', draggedMinute !== null ? 'cursor-grabbing' : 'cursor-pointer')}
      style={{ height: HEIGHT }}
    >
      {width > 0 && (
        <svg width={width} height={HEIGHT} aria-hidden="true">
          <rect
            x={positionOfMinute(sessionOpenMinute, width)}
            y={0}
            width={positionOfMinute(sessionCloseMinute, width) - positionOfMinute(sessionOpenMinute, width)}
            height={plotHeight}
            fill="var(--kairos-surface-3)"
          />
          <text x={positionOfMinute(sessionOpenMinute, width) + 6} y={14} className="num fill-ink-3 text-2xs">Séance de New York</text>

          <line x1={0} x2={width} y1={plotHeight} y2={plotHeight} stroke="var(--kairos-line)" />

          {HOUR_MARKS.filter((hour) => width >= NARROW_WIDTH || hour % 6 === 0).map((hour) => {
            const x = positionOfMinute(hour * 60, width);
            const anchor = hour === 0 ? 'start' : hour === 24 ? 'end' : 'middle';

            return (
              <g key={hour}>
                <line x1={x} x2={x} y1={plotHeight} y2={plotHeight + 4} stroke="var(--kairos-line-strong)" />
                <text x={x} y={HEIGHT - 3} textAnchor={anchor} className="num fill-ink-3 text-2xs">
                  {String(hour).padStart(2, '0')}h
                </text>
              </g>
            );
          })}

          <path
            d={seriesPath(points, width, PLOT_TOP, plotHeight - 4)}
            fill="none"
            stroke="var(--kairos-accent)"
            strokeWidth={1.5}
            strokeLinejoin="round"
          />

          <line x1={cursorX} x2={cursorX} y1={0} y2={plotHeight} stroke="var(--kairos-accent)" />
          <circle cx={cursorX} cy={plotHeight} r={4.25} fill="var(--kairos-accent)" />
        </svg>
      )}
    </div>
  );
}
