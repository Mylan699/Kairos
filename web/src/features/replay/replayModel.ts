import type { IntradayPoint } from '@/api/types';

export const LAST_MINUTE = 1_439;

const MINUTES_IN_DAY = 1_440;
const KEYBOARD_STEP_MINUTES = 5;
const KEYBOARD_PAGE_MINUTES = 60;

export function minuteFromPosition(x: number, width: number): number {
  if (width <= 0) {
    return 0;
  }

  return Math.min(Math.max(Math.round((x / width) * MINUTES_IN_DAY), 0), LAST_MINUTE);
}

export const positionOfMinute = (minute: number, width: number): number => (minute / MINUTES_IN_DAY) * width;

/** Where a key moves the replay, or null when the key is not a command. */
export function keyboardTarget(key: string, minute: number): number | null {
  const clamp = (value: number) => Math.min(Math.max(value, 0), LAST_MINUTE);

  switch (key) {
    case 'ArrowRight':
    case 'ArrowUp':
      return clamp(minute + KEYBOARD_STEP_MINUTES);
    case 'ArrowLeft':
    case 'ArrowDown':
      return clamp(minute - KEYBOARD_STEP_MINUTES);
    case 'PageUp':
      return clamp(minute + KEYBOARD_PAGE_MINUTES);
    case 'PageDown':
      return clamp(minute - KEYBOARD_PAGE_MINUTES);
    case 'Home':
      return 0;
    case 'End':
      return LAST_MINUTE;
    default:
      return null;
  }
}

/** The day P&L on the timeline: a flat series is drawn in the middle. */
export function seriesPath(points: readonly IntradayPoint[], width: number, top: number, bottom: number): string {
  if (points.length === 0) {
    return '';
  }

  const values = points.map((point) => point.dayPnl);
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = max - min;

  return points
    .map((point, index) => {
      const x = positionOfMinute(point.minute, width);
      const y = range === 0 ? (top + bottom) / 2 : bottom - ((point.dayPnl - min) / range) * (bottom - top);
      return `${index === 0 ? 'M' : 'L'}${x.toFixed(1)} ${y.toFixed(1)}`;
    })
    .join(' ');
}

/** Replay speed is in simulated seconds per real second. */
export const speedLabel = (speed: number): string => `×${speed}`;

/** How long a full day takes at this speed, in real minutes. */
export function realMinutesPerDay(speed: number): number {
  return Math.round((MINUTES_IN_DAY * 60) / speed / 60);
}

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

export const isIsoDate = (text: string): boolean => ISO_DATE.test(text);
