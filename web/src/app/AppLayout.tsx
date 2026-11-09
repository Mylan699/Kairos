import { AnimatePresence, motion } from 'motion/react';
import { useState } from 'react';
import { Outlet, useLocation } from 'react-router';
import { AlertIcon, duration, ease, Kbd, Modal } from '@/design-system';
import { useTradeTicket } from '@/features/trade-ticket/TicketContext';
import { useLive } from '@/realtime/live';
import { CommandPalette } from './CommandPalette';
import { BottomNavigation, Sidebar } from './Sidebar';
import { TopBar } from './TopBar';
import { useHotkeys } from './useHotkeys';

const SHORTCUTS: ReadonlyArray<[string, string]> = [
  ['Ctrl K', 'Ouvrir la palette de commandes'],
  ['B', 'Saisir un achat'],
  ['S', 'Saisir une vente'],
  ['Échap', 'Fermer le panneau ouvert'],
  ['?', 'Afficher cette aide'],
];

function ConnectionBanner() {
  const { status } = useLive();
  const isDown = status === 'reconnecting' || status === 'disconnected';

  return (
    <AnimatePresence initial={false}>
      {isDown && (
        <motion.div
          role="status"
          initial={{ height: 0 }}
          animate={{ height: 32 }}
          exit={{ height: 0 }}
          transition={{ duration: duration.base, ease: ease.standard }}
          className="shrink-0 overflow-hidden bg-caution-soft"
        >
          <p className="flex h-8 items-center justify-center gap-2 px-3 text-xs text-caution">
            <AlertIcon />
            Flux temps réel interrompu. Reconnexion automatique en cours, les valeurs affichées peuvent être anciennes.
          </p>
        </motion.div>
      )}
    </AnimatePresence>
  );
}

export function AppLayout() {
  const location = useLocation();
  const [helpOpen, setHelpOpen] = useState(false);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const { openTicket } = useTradeTicket();

  useHotkeys({
    '?': () => setHelpOpen(true),
    b: () => openTicket({ side: 'Buy' }),
    s: () => openTicket({ side: 'Sell' }),
  });

  return (
    <div className="flex min-h-dvh bg-canvas">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <TopBar onShowShortcuts={() => setHelpOpen(true)} onOpenPalette={() => setPaletteOpen(true)} />
        <ConnectionBanner />
        <motion.main
          key={location.pathname}
          className="min-h-0 flex-1 p-2 pb-16 md:p-3"
          initial={{ opacity: 0, y: 4 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: duration.base, ease: ease.standard }}
        >
          <Outlet />
        </motion.main>
      </div>
      <BottomNavigation />
      <CommandPalette open={paletteOpen} onOpenChange={setPaletteOpen} />

      <Modal open={helpOpen} onClose={() => setHelpOpen(false)} title="Raccourcis clavier">
        <dl className="flex flex-col gap-2">
          {SHORTCUTS.map(([key, description]) => (
            <div key={key} className="flex items-center justify-between gap-4">
              <dt className="text-sm text-ink-2">{description}</dt>
              <dd><Kbd>{key}</Kbd></dd>
            </div>
          ))}
        </dl>
      </Modal>
    </div>
  );
}
