import { AnimatePresence, motion } from 'motion/react';
import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react';
import { cx } from '../cx';
import { duration, ease } from '../motion';
import { CONTROL_CLASSES, FieldShell } from './Field';
import { CheckIcon, ChevronDownIcon } from './Icons';

export interface SelectOption<T extends string> {
  value: T;
  label: string;
}

interface SelectProps<T extends string> {
  label: string;
  value: T | null;
  options: ReadonlyArray<SelectOption<T>>;
  onChange: (value: T) => void;
  placeholder?: string;
  error?: string;
  hideLabel?: boolean;
}

/** A listbox drawn entirely by the design system: a native select opens an unstyled system menu. */
export function Select<T extends string>({
  label,
  value,
  options,
  onChange,
  placeholder = 'Sélectionner',
  error,
  hideLabel = false,
}: SelectProps<T>) {
  const id = useId();
  const listId = `${id}-list`;
  const rootRef = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);

  const selected = options.find((option) => option.value === value);

  useEffect(() => {
    if (!open) {
      return;
    }

    const closeWhenOutside = (event: PointerEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    document.addEventListener('pointerdown', closeWhenOutside);
    return () => document.removeEventListener('pointerdown', closeWhenOutside);
  }, [open]);

  const openList = () => {
    setActiveIndex(Math.max(options.findIndex((option) => option.value === value), 0));
    setOpen(true);
  };

  const choose = (index: number) => {
    const option = options[index];
    if (option) {
      onChange(option.value);
    }
    setOpen(false);
  };

  const onKeyDown = (event: KeyboardEvent) => {
    if (!open) {
      if (['ArrowDown', 'ArrowUp', 'Enter', ' '].includes(event.key)) {
        event.preventDefault();
        openList();
      }
      return;
    }

    const moves: Record<string, number> = {
      ArrowDown: Math.min(activeIndex + 1, options.length - 1),
      ArrowUp: Math.max(activeIndex - 1, 0),
      Home: 0,
      End: options.length - 1,
    };

    const nextIndex = moves[event.key];
    if (nextIndex !== undefined) {
      event.preventDefault();
      setActiveIndex(nextIndex);
    } else if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      choose(activeIndex);
    } else if (event.key === 'Escape') {
      // Closes the list only, not the drawer that may contain it.
      event.stopPropagation();
      setOpen(false);
    } else if (event.key === 'Tab') {
      setOpen(false);
    }
  };

  const control = (
    <div ref={rootRef} className="relative">
      <button
        id={id}
        type="button"
        role="combobox"
        aria-label={hideLabel ? label : undefined}
        aria-expanded={open}
        aria-haspopup="listbox"
        aria-controls={listId}
        aria-activedescendant={open ? `${id}-option-${activeIndex}` : undefined}
        onClick={() => (open ? setOpen(false) : openList())}
        onKeyDown={onKeyDown}
        className={cx(CONTROL_CLASSES, 'flex items-center justify-between gap-2 text-left', error && 'border-caution')}
      >
        <span className={cx('truncate', !selected && 'text-ink-3')}>{selected?.label ?? placeholder}</span>
        <ChevronDownIcon className={cx('shrink-0 text-ink-3 transition-transform duration-[var(--motion-fast)]', open && 'rotate-180')} />
      </button>

      <AnimatePresence>
        {open && (
          <motion.ul
            id={listId}
            role="listbox"
            aria-label={label}
            initial={{ opacity: 0, y: -4 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -4 }}
            transition={{ duration: duration.fast, ease: ease.standard }}
            className="absolute inset-x-0 top-full z-40 mt-1 max-h-64 min-w-40 overflow-y-auto rounded-md border border-line-strong bg-surface-3 py-1"
          >
            {options.map((option, index) => (
              <li
                key={option.value}
                id={`${id}-option-${index}`}
                role="option"
                aria-selected={option.value === value}
                onPointerEnter={() => setActiveIndex(index)}
                onClick={() => choose(index)}
                className={cx(
                  'flex h-7 cursor-default items-center justify-between gap-2 px-2.5 text-sm text-ink',
                  index === activeIndex && 'bg-surface-4',
                )}
              >
                <span className="truncate">{option.label}</span>
                {option.value === value && <CheckIcon className="shrink-0 text-accent" />}
              </li>
            ))}
          </motion.ul>
        )}
      </AnimatePresence>
    </div>
  );

  return hideLabel ? control : <FieldShell label={label} htmlFor={id} error={error}>{control}</FieldShell>;
}
