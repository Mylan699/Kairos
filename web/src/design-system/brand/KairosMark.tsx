import { motion, useReducedMotion } from 'motion/react';
import { duration, ease } from '../motion';

/** The geometry of brand/kairos-mark-*.svg. It is never altered: a test compares it to the files. */
export const MARK_GEOMETRY = {
  viewBox: '0 0 48 48',
  stem: { x: 9, y: 8, width: 6, height: 32, rx: 3 },
  leg: { d: 'M24 25.5 L37 39', strokeWidth: 5.5 },
  curve: { d: 'M15 30 L21.5 23.5 L26.5 26.5 L36 13', strokeWidth: 5 },
  dot: { cx: 38, cy: 10.5, r: 4.25 },
} as const;

/**
 * - `still`: the plain symbol.
 * - `draw`: the curve draws itself, then the last tick appears with a slight pulse.
 * - `live`: the last tick pulses while the feed is connected and turns grey when it is not.
 */
export type MarkAnimation = 'still' | 'draw' | 'live';

interface KairosMarkProps {
  size?: number;
  animation?: MarkAnimation;
  /** Only used by the `live` animation. */
  connected?: boolean;
  /** Draws the whole symbol in the current text colour. */
  mono?: boolean;
  className?: string;
  title?: string;
}

export function KairosMark({
  size = 24,
  animation = 'still',
  connected = true,
  mono = false,
  className,
  title = 'Kairos',
}: KairosMarkProps) {
  const reducedMotion = useReducedMotion();
  const drawing = animation === 'draw' && !reducedMotion;

  const inkColor = mono ? 'currentColor' : 'var(--kairos-ink)';
  const accentColor = mono ? 'currentColor' : 'var(--kairos-accent)';
  const isOffline = animation === 'live' && !connected;
  const dotColor = isOffline ? 'var(--kairos-idle)' : accentColor;

  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox={MARK_GEOMETRY.viewBox}
      width={size}
      height={size}
      role="img"
      aria-label={title}
      className={className}
    >
      <rect {...MARK_GEOMETRY.stem} fill={inkColor} />
      <path
        d={MARK_GEOMETRY.leg.d}
        stroke={inkColor}
        strokeWidth={MARK_GEOMETRY.leg.strokeWidth}
        strokeLinecap="round"
        fill="none"
      />
      <motion.path
        data-part="curve"
        d={MARK_GEOMETRY.curve.d}
        stroke={accentColor}
        strokeWidth={MARK_GEOMETRY.curve.strokeWidth}
        strokeLinecap="round"
        strokeLinejoin="round"
        fill="none"
        initial={drawing ? { pathLength: 0 } : false}
        animate={{ pathLength: 1 }}
        transition={{ duration: duration.draw, ease: ease.standard }}
      />
      {animation === 'live' && connected && (
        <circle data-part="pulse" {...MARK_GEOMETRY.dot} fill={accentColor} className="kairos-pulse" />
      )}
      <motion.circle
        data-part="dot"
        {...MARK_GEOMETRY.dot}
        fill={dotColor}
        style={{ transformBox: 'fill-box', transformOrigin: 'center', transition: `fill ${duration.base}s` }}
        initial={drawing ? { scale: 0, opacity: 0 } : false}
        animate={drawing ? { scale: [0, 1.35, 1], opacity: 1 } : { scale: 1, opacity: 1 }}
        transition={{ delay: drawing ? duration.draw : 0, duration: duration.slow * 2, ease: ease.standard }}
      />
    </svg>
  );
}
