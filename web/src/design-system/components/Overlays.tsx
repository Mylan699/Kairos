import { AnimatePresence, motion } from 'motion/react';
import { useEffect, useId, useRef, type ReactNode, type RefObject } from 'react';
import { createPortal } from 'react-dom';
import { duration, ease } from '../motion';
import { IconButton } from './Button';
import { CloseIcon } from './Icons';

const FOCUSABLE = 'button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])';

/** Keeps keyboard focus inside an open overlay, closes it on Escape, and gives focus back afterwards. */
function useOverlayBehaviour(open: boolean, onClose: () => void, panelRef: RefObject<HTMLDivElement | null>) {
  const onCloseRef = useRef(onClose);

  useEffect(() => {
    onCloseRef.current = onClose;
  }, [onClose]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const previouslyFocused = document.activeElement as HTMLElement | null;
    panelRef.current?.querySelector<HTMLElement>(FOCUSABLE)?.focus();

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onCloseRef.current();
        return;
      }

      if (event.key !== 'Tab' || !panelRef.current) {
        return;
      }

      const focusable = [...panelRef.current.querySelectorAll<HTMLElement>(FOCUSABLE)];
      const first = focusable[0];
      const last = focusable[focusable.length - 1];

      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last?.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first?.focus();
      }
    };

    document.addEventListener('keydown', onKeyDown);

    return () => {
      document.removeEventListener('keydown', onKeyDown);
      previouslyFocused?.focus();
    };
  }, [open, panelRef]);
}

interface OverlayProps {
  open: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
}

const Scrim = ({ onClose }: { onClose: () => void }) => (
  <motion.div
    aria-hidden="true"
    onClick={onClose}
    className="fixed inset-0 z-40 bg-canvas/70"
    initial={{ opacity: 0 }}
    animate={{ opacity: 1 }}
    exit={{ opacity: 0 }}
    transition={{ duration: duration.base, ease: ease.standard }}
  />
);

/** A side panel that keeps the page visible behind it, as used for the trade ticket. */
export function Drawer({ open, onClose, title, children }: OverlayProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const titleId = useId();
  useOverlayBehaviour(open, onClose, panelRef);

  return createPortal(
    <AnimatePresence>
      {open && (
        <>
          <Scrim onClose={onClose} />
          <motion.div
            ref={panelRef}
            role="dialog"
            aria-modal="true"
            aria-labelledby={titleId}
            className="fixed inset-y-0 right-0 z-50 flex w-full max-w-[420px] flex-col border-l border-line bg-surface-2"
            initial={{ x: '100%' }}
            animate={{ x: 0 }}
            exit={{ x: '100%' }}
            transition={{ duration: duration.slow, ease: ease.standard }}
          >
            <header className="flex h-12 shrink-0 items-center justify-between border-b border-line-subtle pl-4 pr-2">
              <h2 id={titleId} className="text-base font-semibold">{title}</h2>
              <IconButton label="Fermer" onClick={onClose}><CloseIcon /></IconButton>
            </header>
            <div className="min-h-0 flex-1 overflow-y-auto">{children}</div>
          </motion.div>
        </>
      )}
    </AnimatePresence>,
    document.body,
  );
}

/** A short centred dialog, for a question that blocks the task at hand. */
export function Modal({ open, onClose, title, children }: OverlayProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const titleId = useId();
  useOverlayBehaviour(open, onClose, panelRef);

  return createPortal(
    <AnimatePresence>
      {open && (
        <>
          <Scrim onClose={onClose} />
          <motion.div
            ref={panelRef}
            role="dialog"
            aria-modal="true"
            aria-labelledby={titleId}
            className="fixed left-1/2 top-[20vh] z-50 w-[calc(100vw-32px)] max-w-md -translate-x-1/2 rounded-lg border border-line bg-surface-2"
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: 8 }}
            transition={{ duration: duration.base, ease: ease.standard }}
          >
            <header className="flex h-12 items-center justify-between border-b border-line-subtle pl-4 pr-2">
              <h2 id={titleId} className="text-base font-semibold">{title}</h2>
              <IconButton label="Fermer" onClick={onClose}><CloseIcon /></IconButton>
            </header>
            <div className="p-4">{children}</div>
          </motion.div>
        </>
      )}
    </AnimatePresence>,
    document.body,
  );
}
