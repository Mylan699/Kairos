import { createContext, use, useCallback, useMemo, useState, type ReactNode } from 'react';
import type { Side, Trade } from '@/api/types';
import { Drawer } from '@/design-system';
import { TradeTicketForm } from './TradeTicketForm';

export interface TicketIntent {
  side: Side;
  instrumentId?: string;
  /** Present when the ticket amends an existing trade. */
  amend?: Trade;
}

interface TicketApi {
  openTicket: (intent: TicketIntent) => void;
}

const TicketContext = createContext<TicketApi | null>(null);

export function useTradeTicket(): TicketApi {
  const api = use(TicketContext);
  if (!api) {
    throw new Error('useTradeTicket must be used within a TradeTicketProvider.');
  }
  return api;
}

export function TradeTicketProvider({ children }: { children: ReactNode }) {
  const [intent, setIntent] = useState<TicketIntent | null>(null);
  const [formKey, setFormKey] = useState(0);

  const openTicket = useCallback((next: TicketIntent) => {
    setFormKey((key) => key + 1);
    setIntent(next);
  }, []);

  const close = useCallback(() => setIntent(null), []);
  const api = useMemo(() => ({ openTicket }), [openTicket]);

  return (
    <TicketContext value={api}>
      {children}
      <Drawer open={intent !== null} onClose={close} title={intent?.amend ? 'Amender le trade' : 'Nouveau trade'}>
        {intent && <TradeTicketForm key={formKey} intent={intent} onDone={close} />}
      </Drawer>
    </TicketContext>
  );
}
