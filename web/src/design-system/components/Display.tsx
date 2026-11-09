import type { HTMLAttributes, ReactNode } from 'react';
import { cx } from '../cx';
import { Button } from './Button';
import { AlertIcon } from './Icons';

interface PanelProps extends HTMLAttributes<HTMLElement> {
  title?: string;
  /** Controls or a figure shown on the right of the title. */
  aside?: ReactNode;
  flush?: boolean;
}

/** The base surface of a screen: separated by a fine border rather than a shadow. */
export function Panel({ title, aside, flush = false, className, children, ...props }: PanelProps) {
  return (
    <section className={cx('flex min-w-0 flex-col rounded-md border border-line-subtle bg-surface-2', className)} {...props}>
      {(title || aside) && (
        <header className="flex min-h-10 shrink-0 items-center justify-between gap-3 border-b border-line-subtle px-3">
          {title && <h2 className="label">{title}</h2>}
          {aside}
        </header>
      )}
      <div className={cx('min-h-0 flex-1', !flush && 'p-3')}>{children}</div>
    </section>
  );
}

type BadgeTone = 'neutral' | 'accent' | 'caution' | 'muted';

const BADGE_TONES: Record<BadgeTone, string> = {
  neutral: 'bg-surface-4 text-ink',
  accent: 'bg-accent-soft text-accent',
  caution: 'bg-caution-soft text-caution',
  muted: 'bg-surface-3 text-ink-3',
};

export function Badge({ tone = 'neutral', children }: { tone?: BadgeTone; children: ReactNode }) {
  return (
    <span className={cx('inline-flex h-5 items-center whitespace-nowrap rounded-sm px-1.5 text-2xs font-medium', BADGE_TONES[tone])}>
      {children}
    </span>
  );
}

export const Kbd = ({ children }: { children: ReactNode }) => (
  <kbd className="num inline-grid h-5 min-w-5 place-items-center whitespace-nowrap rounded-sm border border-line bg-surface-1 px-1 text-2xs text-ink-2">
    {children}
  </kbd>
);

/** Reserves the exact shape of the content to come, so that nothing moves when it arrives. */
export const Skeleton = ({ className }: { className?: string }) => (
  <div aria-hidden="true" className={cx('kairos-skeleton rounded-sm bg-surface-3', className)} />
);

interface MessageProps {
  title: string;
  description?: string;
  action?: ReactNode;
}

export function EmptyState({ title, description, action }: MessageProps) {
  return (
    <div className="grid h-full min-h-40 place-items-center p-6 text-center">
      <div className="flex max-w-xs flex-col items-center gap-1">
        <p className="text-sm font-medium text-ink">{title}</p>
        {description && <p className="text-xs text-ink-2">{description}</p>}
        {action && <div className="mt-2">{action}</div>}
      </div>
    </div>
  );
}

export function ErrorState({ title = 'Impossible de charger ces données', description, onRetry }: Omit<MessageProps, 'action' | 'title'> & { title?: string; onRetry?: () => void }) {
  return (
    <div role="alert" className="grid h-full min-h-40 place-items-center p-6 text-center">
      <div className="flex max-w-xs flex-col items-center gap-1">
        <AlertIcon className="mb-1 text-caution" />
        <p className="text-sm font-medium text-ink">{title}</p>
        {description && <p className="text-xs text-ink-2">{description}</p>}
        {onRetry && <Button size="sm" className="mt-2" onClick={onRetry}>Réessayer</Button>}
      </div>
    </div>
  );
}
