import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { cx } from '../cx';

type Variant = 'primary' | 'secondary' | 'ghost';
type Size = 'sm' | 'md';

const VARIANTS: Record<Variant, string> = {
  primary: 'bg-accent text-on-accent hover:bg-accent-hover active:bg-accent-active',
  secondary: 'border border-line bg-surface-2 text-ink hover:border-line-strong hover:bg-surface-3 active:bg-surface-4',
  ghost: 'text-ink-2 hover:bg-surface-3 hover:text-ink active:bg-surface-4',
};

const SIZES: Record<Size, string> = {
  sm: 'h-7 gap-1.5 px-2.5 text-xs',
  md: 'h-8 gap-2 px-3 text-sm',
};

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
  children: ReactNode;
}

export function Button({ variant = 'secondary', size = 'md', className, type = 'button', ...props }: ButtonProps) {
  return (
    <button
      type={type}
      className={cx(
        'inline-flex shrink-0 select-none items-center justify-center rounded-md font-medium',
        'transition-colors duration-[var(--motion-fast)] ease-standard',
        'disabled:pointer-events-none disabled:opacity-40',
        VARIANTS[variant],
        SIZES[size],
        className,
      )}
      {...props}
    />
  );
}

interface IconButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  /** Read by screen readers, as the button only shows an icon. */
  label: string;
  children: ReactNode;
}

export function IconButton({ label, className, type = 'button', ...props }: IconButtonProps) {
  return (
    <button
      type={type}
      aria-label={label}
      title={label}
      className={cx(
        'inline-grid size-8 shrink-0 place-items-center rounded-md text-ink-2',
        'transition-colors duration-[var(--motion-fast)] ease-standard hover:bg-surface-3 hover:text-ink',
        'disabled:pointer-events-none disabled:opacity-40',
        className,
      )}
      {...props}
    />
  );
}
