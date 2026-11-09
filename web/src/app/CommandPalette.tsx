import { Command } from 'cmdk';
import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { useNavigate } from 'react-router';
import { apiPost, describeError } from '@/api/client';
import { useReferenceData, useRemarkableDays, useScenarios, useTrades } from '@/api/queries';
import { Kbd, SearchIcon, setTheme, useTheme, useToast } from '@/design-system';
import { useTradeTicket } from '@/features/trade-ticket/TicketContext';
import { formatDate } from '@/lib/format';
import { useBookFilter } from './BookFilter';
import { NAVIGATION } from './navigation';

const MAX_TRADE_RESULTS = 20;
const MIN_TRADE_SEARCH_LENGTH = 2;

const ITEM_CLASSES =
  'flex h-8 cursor-default items-center gap-2 rounded-sm px-2 text-sm text-ink-2 aria-selected:bg-surface-4 aria-selected:text-ink';

const GROUP_CLASSES =
  '[&_[cmdk-group-heading]]:label [&_[cmdk-group-heading]]:px-2 [&_[cmdk-group-heading]]:py-1.5 py-1';

function Item({ value, onSelect, children }: { value: string; onSelect: () => void; children: ReactNode }) {
  return <Command.Item value={value} onSelect={onSelect} className={ITEM_CLASSES}>{children}</Command.Item>;
}

export function CommandPalette({ open, onOpenChange }: { open: boolean; onOpenChange: (open: boolean) => void }) {
  const navigate = useNavigate();
  const theme = useTheme();
  const { notify } = useToast();
  const { openTicket } = useTradeTicket();
  const { setBookId } = useBookFilter();
  const [search, setSearch] = useState('');

  const reference = useReferenceData();
  const scenarios = useScenarios();
  const days = useRemarkableDays();
  const trades = useTrades();

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key.toLowerCase() === 'k' && (event.ctrlKey || event.metaKey)) {
        event.preventDefault();
        onOpenChange(!open);
      }
    };

    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [open, onOpenChange]);

  const run = (action: () => void) => {
    onOpenChange(false);
    setSearch('');
    action();
  };

  const matchingTrades = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (term.length < MIN_TRADE_SEARCH_LENGTH) {
      return [];
    }

    return (trades.data ?? [])
      .filter((trade) => `${trade.symbol} ${trade.counterparty} ${trade.id}`.toLowerCase().includes(term))
      .slice(-MAX_TRADE_RESULTS)
      .reverse();
  }, [search, trades.data]);

  const replayDay = async (date: string, startMinute: number) => {
    try {
      await apiPost('/api/replay/load', { date, startMinute });
      navigate('/app/replay');
    } catch (error) {
      notify({ tone: 'error', title: 'Rejeu impossible', description: describeError(error) });
    }
  };

  return (
    <Command.Dialog
      open={open}
      onOpenChange={onOpenChange}
      label="Palette de commandes"
      overlayClassName="fixed inset-0 z-40 bg-canvas/70"
      contentClassName="fixed left-1/2 top-[14vh] z-50 w-[calc(100vw-32px)] max-w-xl -translate-x-1/2 overflow-hidden rounded-lg border border-line-strong bg-surface-2"
    >
      <div className="flex h-11 items-center gap-2 border-b border-line-subtle px-3 text-ink-3">
        <SearchIcon />
        <Command.Input
          value={search}
          onValueChange={setSearch}
          placeholder="Instrument, écran, book, scénario, trade…"
          className="h-full flex-1 bg-transparent text-base text-ink placeholder:text-ink-3 focus:outline-none"
        />
        <Kbd>Échap</Kbd>
      </div>

      <Command.List className="max-h-[52vh] overflow-y-auto p-1">
        <Command.Empty className="p-6 text-center text-sm text-ink-2">Aucun résultat.</Command.Empty>

        <Command.Group heading="Actions" className={GROUP_CLASSES}>
          <Item value="nouvel achat acheter buy" onSelect={() => run(() => openTicket({ side: 'Buy' }))}>
            Saisir un achat <span className="ml-auto"><Kbd>B</Kbd></span>
          </Item>
          <Item value="nouvelle vente vendre sell" onSelect={() => run(() => openTicket({ side: 'Sell' }))}>
            Saisir une vente <span className="ml-auto"><Kbd>S</Kbd></span>
          </Item>
          <Item value="theme clair sombre apparence" onSelect={() => run(() => setTheme(theme === 'dark' ? 'light' : 'dark'))}>
            {theme === 'dark' ? 'Passer au thème clair' : 'Passer au thème sombre'}
          </Item>
        </Command.Group>

        <Command.Group heading="Écrans" className={GROUP_CLASSES}>
          {NAVIGATION.map((item) => (
            <Item key={item.to} value={`aller ${item.label}`} onSelect={() => run(() => navigate(item.to))}>
              <item.icon /> {item.label}
            </Item>
          ))}
        </Command.Group>

        {reference.data && (
          <Command.Group heading="Books" className={GROUP_CLASSES}>
            <Item value="tous les books desk" onSelect={() => run(() => setBookId(null))}>Tous les books</Item>
            {reference.data.books.map((book) => (
              <Item key={book.id} value={`book ${book.name}`} onSelect={() => run(() => setBookId(book.id))}>{book.name}</Item>
            ))}
          </Command.Group>
        )}

        {reference.data && (
          <Command.Group heading="Instruments" className={GROUP_CLASSES}>
            {reference.data.instruments.filter((item) => item.isTradable).map((item) => (
              <Item key={item.id} value={`${item.symbol} ${item.sector ?? ''}`} onSelect={() => run(() => navigate(`/app/markets?instrument=${item.id}`))}>
                <span className="num text-ink">{item.symbol}</span>
                {item.sector && <span className="text-xs text-ink-3">{item.sector}</span>}
              </Item>
            ))}
          </Command.Group>
        )}

        {matchingTrades.length > 0 && (
          <Command.Group heading="Trades" className={GROUP_CLASSES}>
            {matchingTrades.map((trade) => (
              <Item key={trade.id} value={`${trade.symbol} ${trade.counterparty} ${trade.id}`} onSelect={() => run(() => navigate(`/app/blotter?trade=${trade.id}`))}>
                <span className="num text-ink">{trade.symbol}</span>
                <span className="text-xs">{trade.side === 'Buy' ? 'Achat' : 'Vente'} · {trade.counterparty}</span>
                <span className="num ml-auto text-xs text-ink-3">{formatDate(trade.tradeDate)}</span>
              </Item>
            ))}
          </Command.Group>
        )}

        {scenarios.data && (
          <Command.Group heading="Scénarios de stress" className={GROUP_CLASSES}>
            {scenarios.data.map((scenario) => (
              <Item key={scenario.id} value={`stress scenario ${scenario.name}`} onSelect={() => run(() => navigate(`/app/risk?scenario=${scenario.id}`))}>
                {scenario.name}
              </Item>
            ))}
          </Command.Group>
        )}

        {days.data && (
          <Command.Group heading="Rejouer une journée" className={GROUP_CLASSES}>
            {days.data.map((day) => (
              <Item key={day.date} value={`rejeu rejouer ${day.name} ${day.date}`} onSelect={() => run(() => void replayDay(day.date, day.startMinute))}>
                {day.name}
                <span className="num ml-auto text-xs text-ink-3">{formatDate(day.date)}</span>
              </Item>
            ))}
          </Command.Group>
        )}
      </Command.List>
    </Command.Dialog>
  );
}
