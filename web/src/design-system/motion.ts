/**
 * The only place where motion is defined. Motion components import these values,
 * and CSS reads the same ones through the variables written by applyMotionTokens.
 */

type Bezier = readonly [number, number, number, number];

/** Durations in seconds. Micro-interactions stay between 150 and 250 ms. */
export const duration = {
  fast: 0.15,
  base: 0.2,
  slow: 0.25,
  /** A price cell keeps its colour just long enough to be noticed. */
  flash: 0.7,
  /** How long a new line stays highlighted in the blotter. */
  highlight: 1,
  /** A chart or the logo drawing itself. */
  draw: 0.9,
} as const;

export const ease = {
  standard: [0.2, 0, 0, 1],
  enter: [0, 0, 0.2, 1],
  exit: [0.4, 0, 1, 1],
} as const satisfies Record<string, Bezier>;

/** Layout changes use a spring, so that an interrupted movement stays continuous. */
export const spring = {
  layout: { type: 'spring', stiffness: 480, damping: 38, mass: 0.8 },
} as const;

const cubicBezier = (curve: Bezier) => `cubic-bezier(${curve.join(', ')})`;

export function applyMotionTokens(root: HTMLElement = document.documentElement) {
  root.style.setProperty('--motion-ease-standard', cubicBezier(ease.standard));
  root.style.setProperty('--motion-ease-enter', cubicBezier(ease.enter));
  root.style.setProperty('--motion-ease-exit', cubicBezier(ease.exit));
  root.style.setProperty('--motion-fast', `${duration.fast * 1000}ms`);
  root.style.setProperty('--motion-base', `${duration.base * 1000}ms`);
  root.style.setProperty('--motion-slow', `${duration.slow * 1000}ms`);
  root.style.setProperty('--motion-flash', `${duration.flash * 1000}ms`);
  root.style.setProperty('--motion-highlight', `${duration.highlight * 1000}ms`);
}
