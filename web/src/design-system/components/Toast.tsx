import { AnimatePresence, motion } from 'motion/react';
import { createContext, use, useCallback, useMemo, useRef, useState, type ReactNode } from 'react';
import { cx } from '../cx';
import { duration, ease, spring } from '../motion';
import { IconButton } from './Button';
import { AlertIcon, CheckIcon, CloseIcon } from './Icons';

type ToastTone = 'success' | 'error';

interface ToastMessage {
  id: number;
  tone: ToastTone;
  title: string;
  description?: string;
}

interface ToastApi {
  notify: (toast: Omit<ToastMessage, 'id'>) => void;
}

const VISIBLE_MILLISECONDS = 4_500;

const ToastContext = createContext<ToastApi | null>(null);

export function useToast(): ToastApi {
  const api = use(ToastContext);
  if (!api) {
    throw new Error('useToast must be used within a ToastProvider.');
  }
  return api;
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);
  const nextId = useRef(1);

  const dismiss = useCallback((id: number) => {
    setToasts((current) => current.filter((toast) => toast.id !== id));
  }, []);

  const notify = useCallback<ToastApi['notify']>((toast) => {
    const id = nextId.current++;
    setToasts((current) => [...current, { ...toast, id }]);
    window.setTimeout(() => dismiss(id), VISIBLE_MILLISECONDS);
  }, [dismiss]);

  const api = useMemo(() => ({ notify }), [notify]);

  return (
    <ToastContext value={api}>
      {children}
      <div className="pointer-events-none fixed bottom-4 right-4 z-[60] flex w-[calc(100vw-32px)] max-w-sm flex-col gap-2">
        <AnimatePresence initial={false}>
          {toasts.map((toast) => (
            <motion.div
              key={toast.id}
              layout
              role={toast.tone === 'error' ? 'alert' : 'status'}
              initial={{ opacity: 0, x: 24 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: 24, transition: { duration: duration.fast, ease: ease.exit } }}
              transition={{ layout: spring.layout, duration: duration.base, ease: ease.standard }}
              className="pointer-events-auto flex items-start gap-2 rounded-md border border-line-strong bg-surface-3 py-2 pl-3 pr-1"
            >
              <span className={cx('mt-0.5 shrink-0', toast.tone === 'error' ? 'text-caution' : 'text-accent')}>
                {toast.tone === 'error' ? <AlertIcon /> : <CheckIcon />}
              </span>
              <div className="min-w-0 flex-1 py-0.5">
                <p className="text-sm font-medium text-ink">{toast.title}</p>
                {toast.description && <p className="mt-0.5 text-xs text-ink-2">{toast.description}</p>}
              </div>
              <IconButton label="Fermer" className="size-6" onClick={() => dismiss(toast.id)}>
                <CloseIcon />
              </IconButton>
            </motion.div>
          ))}
        </AnimatePresence>
      </div>
    </ToastContext>
  );
}
