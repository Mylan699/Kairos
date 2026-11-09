import { useId, type InputHTMLAttributes, type ReactNode } from 'react';
import { cx } from '../cx';

export const CONTROL_CLASSES = cx(
  'h-8 w-full rounded-md border border-line bg-surface-1 px-2.5 text-sm text-ink',
  'transition-colors duration-[var(--motion-fast)] ease-standard',
  'hover:border-line-strong focus:border-accent focus:outline-none',
  'placeholder:text-ink-3 disabled:opacity-50',
);

interface FieldShellProps {
  label: string;
  htmlFor?: string;
  error?: string;
  hint?: ReactNode;
  children: ReactNode;
}

/** The label, control and message of a form field. Errors use the caution colour, never red. */
export function FieldShell({ label, htmlFor, error, hint, children }: FieldShellProps) {
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={htmlFor} className="label">
        {label}
      </label>
      {children}
      {error ? (
        <p role="alert" className="text-xs text-caution">
          {error}
        </p>
      ) : (
        hint && <p className="text-xs text-ink-3">{hint}</p>
      )}
    </div>
  );
}

interface TextFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
  hint?: ReactNode;
  /** Numbers are set in the mono font, with tabular figures. */
  numeric?: boolean;
}

export function TextField({ label, error, hint, numeric = false, className, id, ...props }: TextFieldProps) {
  const generatedId = useId();
  const inputId = id ?? generatedId;

  return (
    <FieldShell label={label} htmlFor={inputId} error={error} hint={hint}>
      <input
        id={inputId}
        aria-invalid={Boolean(error)}
        inputMode={numeric ? 'decimal' : undefined}
        className={cx(CONTROL_CLASSES, numeric && 'num text-right', error && 'border-caution', className)}
        {...props}
      />
    </FieldShell>
  );
}
