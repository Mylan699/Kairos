import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Route, Routes } from 'react-router';
import type { MarketTick } from '@/api/types';
import { renderWithLive } from '@/test/renderWithLive';
import { TradeTicketProvider } from '@/features/trade-ticket/TicketContext';
import { AppLayout } from './AppLayout';
import { useHotkeys } from './useHotkeys';

const tick = {
  status: { date: '2020-03-16', minute: 810, speed: 120, isPlaying: true, sessionOpenMinute: 810, sessionCloseMinute: 1200 },
  quotes: [],
  curve: { date: '2020-03-16', points: [] },
  summary: {
    marketDate: '2020-03-16',
    books: [],
    total: { realized: 0, unrealized: 0, priceEffect: 0, fxEffect: 0, total: 0, day: -1250 },
  },
} as MarketTick;

const renderLayout = () =>
  renderWithLive(
    <TradeTicketProvider>
      <Routes>
        <Route path="/app" element={<AppLayout />}>
          <Route index element={<p>Contenu</p>} />
        </Route>
      </Routes>
    </TradeTicketProvider>,
  );

describe('AppLayout', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn(async (url: string) =>
      new Response(url.endsWith('/reference') ? JSON.stringify({ instruments: [], books: [], traders: [], counterparties: [] }) : '[]')));
  });

  it('shows the page and the navigation', () => {
    renderLayout();

    expect(screen.getByText('Contenu')).toBeInTheDocument();
    expect(screen.getAllByRole('navigation', { name: 'Navigation principale' })).toHaveLength(2);
  });

  it('shows the simulated date, time and day result from the feed', async () => {
    const { hub } = renderLayout();
    await waitFor(() => expect(screen.getByText('En direct')).toBeInTheDocument());

    act(() => hub.emit('tick', tick));

    expect(screen.getByText(/lundi 16 mars 2020/i)).toBeInTheDocument();
    expect(screen.getByText('13:30 UTC')).toBeInTheDocument();
    expect(screen.getByRole('text', { name: /1\s250\s€/ })).toBeInTheDocument();
  });

  it('warns when the live feed is lost', async () => {
    const { hub } = renderLayout();
    await waitFor(() => expect(screen.getByText('En direct')).toBeInTheDocument());

    act(() => hub.loseConnection());

    expect(screen.getByText(/Flux temps réel interrompu/)).toBeInTheDocument();
    expect(screen.getByText('Reconnexion')).toBeInTheDocument();
  });

  it('opens the shortcut help with the question mark', async () => {
    renderLayout();

    await userEvent.keyboard('?');

    expect(screen.getByRole('dialog', { name: 'Raccourcis clavier' })).toBeInTheDocument();
  });
});

describe('workstation shortcuts', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn(async (url: string) =>
      new Response(url.endsWith('/reference') ? JSON.stringify({ instruments: [], books: [], traders: [], counterparties: [] }) : '[]')));
  });

  it('opens a ticket with B and closes it with Escape', async () => {
    renderLayout();

    await userEvent.keyboard('b');
    expect(await screen.findByRole('dialog', { name: 'Nouveau trade' })).toBeInTheDocument();

    await userEvent.keyboard('{Escape}');
    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Nouveau trade' })).not.toBeInTheDocument());
  });

  it('opens the command palette with Ctrl K', async () => {
    renderLayout();

    await userEvent.keyboard('{Control>}k{/Control}');

    expect(await screen.findByPlaceholderText(/Instrument, écran/)).toBeInTheDocument();
  });
});

describe('useHotkeys', () => {
  function Harness({ onPress }: { onPress: () => void }) {
    useHotkeys({ b: onPress });
    return <input aria-label="Saisie" />;
  }

  it('reacts to the key, whatever its case', async () => {
    const onPress = vi.fn();
    render(<Harness onPress={onPress} />);

    await userEvent.keyboard('b');
    await userEvent.keyboard('B');

    expect(onPress).toHaveBeenCalledTimes(2);
  });

  it('ignores the key while the user is typing', async () => {
    const onPress = vi.fn();
    render(<Harness onPress={onPress} />);

    await userEvent.type(screen.getByLabelText('Saisie'), 'b');

    expect(onPress).not.toHaveBeenCalled();
  });

  it('ignores the key when combined with a modifier', async () => {
    const onPress = vi.fn();
    render(<Harness onPress={onPress} />);

    await userEvent.keyboard('{Control>}b{/Control}');

    expect(onPress).not.toHaveBeenCalled();
  });
});
