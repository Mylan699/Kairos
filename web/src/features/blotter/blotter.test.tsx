import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { Trade, TradeDetail, TradeStatus } from '@/api/types';
import { TradeTicketProvider } from '@/features/trade-ticket/TicketContext';
import { renderWithLive } from '@/test/renderWithLive';
import { availableActions, filterTrades, sortNewestFirst } from './blotterModel';
import { TradeDetailDrawer } from './TradeDetailDrawer';

const trade = (change: Partial<Trade> = {}): Trade => ({
  id: 't1',
  version: 1,
  status: 'New',
  instrumentId: 'i',
  symbol: 'MSFT',
  assetClass: 'Equity',
  side: 'Buy',
  quantity: 100,
  price: 150,
  counterpartyId: 'c',
  counterparty: 'Cpty Orion',
  bookId: 'b1',
  book: 'Cross-Asset',
  traderId: 'tr',
  trader: 'Inès Moreau',
  tradeDate: '2020-03-16',
  valueDate: '2020-03-18',
  updatedAt: '2020-03-16T14:00:00Z',
  ...change,
});

describe('filterTrades', () => {
  const trades = [
    trade({ id: 'a' }),
    trade({ id: 'b', status: 'Settled', assetClass: 'Fx' }),
    trade({ id: 'c', status: 'Settled', bookId: 'b2' }),
  ];
  const ids = (filtered: Trade[]) => filtered.map((item) => item.id);

  it('keeps everything without filters', () => {
    expect(ids(filterTrades(trades, { status: 'All', assetClass: 'All', bookId: null }))).toEqual(['a', 'b', 'c']);
  });

  it('combines status, asset class and book', () => {
    expect(ids(filterTrades(trades, { status: 'Settled', assetClass: 'All', bookId: null }))).toEqual(['b', 'c']);
    expect(ids(filterTrades(trades, { status: 'Settled', assetClass: 'Fx', bookId: null }))).toEqual(['b']);
    expect(ids(filterTrades(trades, { status: 'All', assetClass: 'All', bookId: 'b2' }))).toEqual(['c']);
  });
});

describe('sortNewestFirst', () => {
  it('sorts by trade date, then by last change', () => {
    const sorted = sortNewestFirst([
      trade({ id: 'old', tradeDate: '2019-01-01' }),
      trade({ id: 'earlier', updatedAt: '2020-03-16T10:00:00Z' }),
      trade({ id: 'later', updatedAt: '2020-03-16T15:00:00Z' }),
    ]);

    expect(sorted.map((item) => item.id)).toEqual(['later', 'earlier', 'old']);
  });

  it('does not modify the list it receives', () => {
    const original = [trade({ id: 'a', tradeDate: '2019-01-01' }), trade({ id: 'b' })];

    sortNewestFirst(original);

    expect(original[0]?.id).toBe('a');
  });
});

describe('availableActions', () => {
  it.each<[TradeStatus, string[]]>([
    ['New', ['validate', 'amend', 'cancel']],
    ['Validated', ['settle', 'amend', 'cancel']],
    ['Settled', []],
    ['Cancelled', []],
  ])('offers the right actions on a %s trade', (status, expected) => {
    expect(availableActions(status)).toEqual(expected);
  });
});

describe('TradeDetailDrawer', () => {
  const detail = (status: TradeStatus): TradeDetail => ({
    trade: trade({ status, version: 2 }),
    versions: [
      { number: 1, side: 'Buy', quantity: 100, price: 150, counterparty: 'Cpty Orion', book: 'Cross-Asset', trader: 'Inès Moreau', tradeDate: '2020-03-16', valueDate: '2020-03-18', createdAt: '2020-03-16T14:00:00Z', createdBy: 'Inès Moreau' },
      { number: 2, side: 'Buy', quantity: 120, price: 150, counterparty: 'Cpty Orion', book: 'Cross-Asset', trader: 'Inès Moreau', tradeDate: '2020-03-16', valueDate: '2020-03-18', createdAt: '2020-03-16T14:05:00Z', createdBy: 'Clara Nguyen' },
    ],
    auditTrail: [
      { at: '2020-03-16T14:00:00Z', actor: 'Inès Moreau', action: 'Booked', resultingStatus: 'New', version: 1, details: '' },
      { at: '2020-03-16T14:05:00Z', actor: 'Clara Nguyen', action: 'Amended', resultingStatus: 'New', version: 2, details: 'Quantity: 100 -> 120' },
    ],
  });

  let posts: string[];

  const stub = (status: TradeStatus) => {
    posts = [];
    vi.stubGlobal('fetch', vi.fn(async (url: string, init?: RequestInit) => {
      if (init?.method === 'POST') {
        posts.push(`${url} ${init.body as string}`);
        return new Response(JSON.stringify(trade()));
      }
      return new Response(JSON.stringify(detail(status)));
    }));
  };

  const open = () => renderWithLive(<TradeTicketProvider><TradeDetailDrawer tradeId="t1" onClose={vi.fn()} /></TradeTicketProvider>);

  beforeEach(() => stub('New'));

  it('shows the versions and the audit trail', async () => {
    open();

    expect(await screen.findByText('Piste d\'audit')).toBeInTheDocument();
    expect(screen.getByText('v2')).toBeInTheDocument();
    expect(screen.getByText('Quantity: 100 -> 120')).toBeInTheDocument();
    expect(screen.getByText('Amendement')).toBeInTheDocument();
  });

  it('validates a new trade', async () => {
    open();

    await userEvent.click(await screen.findByRole('button', { name: 'Valider' }));

    await waitFor(() => expect(posts[0]).toContain('/api/trades/t1/validate'));
    expect(await screen.findByText('Trade validé')).toBeInTheDocument();
  });

  it('asks for a reason before cancelling', async () => {
    open();

    await userEvent.click(await screen.findByRole('button', { name: 'Annuler le trade' }));
    expect(await screen.findByRole('button', { name: "Confirmer l'annulation" })).toBeDisabled();

    await userEvent.type(screen.getByLabelText('Motif'), 'Doublon');
    await userEvent.click(screen.getByRole('button', { name: "Confirmer l'annulation" }));

    await waitFor(() => expect(posts[0]).toContain('{"reason":"Doublon"}'));
  });

  it('offers no action on a settled trade', async () => {
    stub('Settled');
    open();

    await screen.findByText('Piste d\'audit');

    expect(screen.queryByRole('button', { name: 'Valider' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Amender' })).not.toBeInTheDocument();
  });
});
