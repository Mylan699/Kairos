import { motion } from 'motion/react';
import { useEffect, useRef, type ReactNode } from 'react';
import { cx } from '../cx';
import { cubicBezier, duration, ease } from '../motion';

const DIGITS = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

function RollingDigit({ digit }: { digit: number }) {
  return (
    <span aria-hidden="true" className="relative inline-block h-[1lh] w-[1ch] overflow-hidden">
      <motion.span
        className="absolute inset-x-0 top-0 flex flex-col"
        initial={false}
        animate={{ y: `${-digit * 10}%` }}
        transition={{ duration: duration.slow, ease: ease.standard }}
      >
        {DIGITS.map((value) => (
          <span key={value} className="h-[1lh] text-center">{value}</span>
        ))}
      </motion.span>
    </span>
  );
}

/**
 * A figure whose digits roll to their new value. Positions are counted from the right,
 * so that units stay under units when the number of digits changes.
 */
export function RollingNumber({ text, className }: { text: string; className?: string }) {
  const characters = [...text];

  return (
    <span className={cx('num inline-flex whitespace-pre', className)} role="text" aria-label={text}>
      {characters.map((character, index) => {
        const positionFromRight = characters.length - index;

        return /\d/.test(character) ? (
          <RollingDigit key={positionFromRight} digit={Number(character)} />
        ) : (
          <span key={positionFromRight} aria-hidden="true">{character}</span>
        );
      })}
    </span>
  );
}

/**
 * Briefly colours its background when the value moves: green up, red down. It uses the
 * Web Animations API rather than state, so that a table of prices never re-renders for it.
 */
export function FlashValue({ value, children, className }: { value: number; children: ReactNode; className?: string }) {
  const elementRef = useRef<HTMLSpanElement>(null);
  const previousValue = useRef(value);

  useEffect(() => {
    const previous = previousValue.current;
    previousValue.current = value;

    if (value === previous || window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) {
      return;
    }

    const color = value > previous ? 'var(--kairos-gain-soft)' : 'var(--kairos-loss-soft)';
    elementRef.current?.animate?.(
      [{ backgroundColor: color }, { backgroundColor: 'transparent' }],
      { duration: duration.flash * 1000, easing: cubicBezier(ease.enter) },
    );
  }, [value]);

  return (
    <span ref={elementRef} className={cx('num rounded-xs px-1', className)}>
      {children}
    </span>
  );
}
