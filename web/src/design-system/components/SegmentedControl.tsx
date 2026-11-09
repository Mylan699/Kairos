import { motion } from 'motion/react';
import { useId } from 'react';
import { cx } from '../cx';
import { spring } from '../motion';

interface Segment<T extends string> {
  value: T;
  label: string;
}

interface SegmentedControlProps<T extends string> {
  label: string;
  value: T;
  segments: ReadonlyArray<Segment<T>>;
  onChange: (value: T) => void;
  /** Stretches the segments across the available width. */
  fullWidth?: boolean;
}

/** A choice between a few views. The marker slides on a spring to show what changed. */
export function SegmentedControl<T extends string>({ label, value, segments, onChange, fullWidth = false }: SegmentedControlProps<T>) {
  const groupId = useId();

  return (
    <div role="radiogroup" aria-label={label} className={cx('h-8 rounded-md border border-line bg-surface-1 p-0.5', fullWidth ? 'flex' : 'inline-flex')}>
      {segments.map((segment) => {
        const isActive = segment.value === value;

        return (
          <button
            key={segment.value}
            type="button"
            role="radio"
            aria-checked={isActive}
            onClick={() => onChange(segment.value)}
            className={cx(
              'relative rounded-sm px-2.5 text-xs font-medium transition-colors duration-[var(--motion-fast)]',
              fullWidth && 'flex-1',
              isActive ? 'text-ink' : 'text-ink-2 hover:text-ink',
            )}
          >
            {isActive && (
              <motion.span layoutId={groupId} transition={spring.layout} className="absolute inset-0 rounded-sm bg-surface-4" />
            )}
            <span className="relative">{segment.label}</span>
          </button>
        );
      })}
    </div>
  );
}
