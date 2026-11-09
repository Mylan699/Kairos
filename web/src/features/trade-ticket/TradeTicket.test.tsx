import { act, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { MarketTick, ReferenceData } from '@/api/types';
import { renderWithLive } from '@/test/renderWithLive';
import { TradeTicketProvider, useTradeTicket } from './TicketContext';

const reference: ReferenceData = {
  instruments: [
    { id: 'msft', symbol: 'MSFT', assetClass: 'Equity', sector: 'Technology', isTradable: true, maturityDate: null, couponRate: null },
    { id: 'old', symbol: 'UST 2Y 2010', assetClass: 'Bond', sector: null, isTradable: false, maturityDate: '2012-01-04', couponRate: 0.01 },
  ],
  books: [{ id: 'book', name: 'Cross-Asset' }],
  traders: [{ id: 'trader', name: 'Inès Moreau' }],
  counterparties: [{ id: 'cpty', name: 'Cpty Orion' }],
};

const tick = {
  status: { date: '2020-03-16' },
  quotes: [{ instrumentId: 'msft', symbol: 'MSFT', assetClass: 'Equity', bid: 150.1, ask: 150.3, last: 150.2, changePercent: 1, yieldPercent: null }],
  curve: { points: [] },
  summary: { books: [], total: {} },
} as unknown as MarketTick;

const impact = { notionalEur: 13_636, pnlImpactEur: -9.09, positionBefore: 0, positionAfter: 100, dv01ChangeEur: 0, totalDv01AfterEur: 500 };

function Opener() {
  const { openTicket } = useTradeTicket();
  return <button onClick={() => openTicket({ side: 'Buy' })}>Ouvrir</button>;
}

describe('trade ticket', () => {
  let posted: Array<{ url: string; body: unknown }>;

  beforeEach(() => {
    posted = [];
    vi.stubGlobal('fetch', vi.fn(async (url: string, init?: RequestInit) => {
      if (init?.method === 'POST') {
        posted.push({ url, body: JSON.parse(init.body as string) });
        return new Response(JSON.stringify(url.endsWith('estimate') ? impact : { id: 'new', side: 'Buy', quantity: 100, symbol: 'MSFT' }));
      }
      return new Response(JSON.stringify(reference));
    }));
  });

  async function openTicket() {
    const view = renderWithLive(<TradeTicketProvider><Opener /></TradeTicketProvider>);
    await waitFor(() => expect(view.hub.start).toHaveBeenCalled());
    act(() => view.hub.emit('tick', tick));
    await userEvent.click(screen.getByRole('button', { name: 'Ouvrir' }));
    await screen.findByRole('button', { name: 'Acheter MSFT' });
    return view;
  }

  it('shows the live bid and ask and follows the market on the buy side', async () => {
    await openTicket();

    expect(screen.getByText('150,10')).toBeInTheDocument();
    expect(screen.getByText('150,30')).toBeInTheDocument();
    expect(screen.getByLabelText('Prix (marché)')).toHaveValue('150,30');
  });

  it('switches to the bid when selling', async () => {
    await openTicket();

    await userEvent.click(screen.getByRole('radio', { name: 'Vente' }));

    expect(screen.getByLabelText('Prix (marché)')).toHaveValue('150,10');
    expect(screen.getByRole('button', { name: 'Vendre MSFT' })).toBeInTheDocument();
  });

  it('stops following the market once a price is typed', async () => {
    const { hub } = await openTicket();

    await userEvent.clear(screen.getByLabelText('Prix (marché)'));
    await userEvent.type(screen.getByLabelText('Prix'), '149');
    act(() => hub.emit('tick', { ...tick }));

    expect(screen.getByLabelText('Prix')).toHaveValue('149');
  });

  it('does not offer instruments that can no longer trade', async () => {
    await openTicket();

    await userEvent.click(screen.getByRole('combobox', { name: 'Instrument' }));

    expect(screen.queryByRole('option', { name: 'UST 2Y 2010' })).not.toBeInTheDocument();
  });

  it('cannot be submitted before a quantity is entered', async () => {
    await openTicket();

    expect(screen.getByRole('button', { name: 'Acheter MSFT' })).toBeDisabled();
  });

  it('estimates the impact, then books the trade and confirms it', async () => {
    await openTicket();

    await userEvent.type(screen.getByLabelText('Quantité (actions)'), '100');

    expect(await screen.findByText(/−9,09/, undefined, { timeout: 3_000 })).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Acheter MSFT' }));

    await waitFor(() => expect(posted.some((call) => call.url === '/api/trades')).toBe(true));
    expect(posted.find((call) => call.url === '/api/trades')?.body).toMatchObject({
      instrumentId: 'msft',
      side: 'Buy',
      quantity: 100,
      price: 150.3,
      bookId: 'book',
    });
    expect(await screen.findByText('Trade enregistré')).toBeInTheDocument();
  });
});
