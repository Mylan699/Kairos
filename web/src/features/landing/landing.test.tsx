import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { describe, expect, it } from 'vitest';
import type { Quote } from '@/api/types';
import LandingPage from './LandingPage';
import { biggestMove, largestDayMoves } from './LiveFeed';

const renderPage = () => render(<MemoryRouter><LandingPage /></MemoryRouter>);

describe('LandingPage', () => {
  it('says what the product is, in one main heading', () => {
    renderPage();

    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Trois marchés.Un seul P&L.');
  });

  it('leads to the demo', () => {
    renderPage();

    const links = screen.getAllByRole('link', { name: 'Lancer la démo' });
    expect(links.length).toBeGreaterThan(1);
    links.forEach((link) => expect(link).toHaveAttribute('href', '/app'));
  });

  it('carries the demonstration notice', () => {
    renderPage();

    expect(screen.getByText(/aucune donnée réelle de client, pas de conseil en investissement/)).toBeInTheDocument();
  });

  it('credits the data sources with their licences', () => {
    renderPage();

    expect(screen.getByText(/AGPL-3.0/)).toBeInTheDocument();
    expect(screen.getByText(/CC BY-NC 4.0/)).toBeInTheDocument();
  });

  it('shows a worked example with its price effect, currency effect and total', () => {
    renderPage();

    expect(screen.getByText('+750,00 €')).toBeInTheDocument();
    expect(screen.getByText('−250,00 €')).toBeInTheDocument();
    expect(screen.getByText('+500,00 €')).toBeInTheDocument();
  });
});

describe('largestDayMoves', () => {
  const quote = (id: string, changePercent: number) => ({ instrumentId: id, symbol: id, changePercent }) as Quote;

  it('ranks by the size of the move, up or down', () => {
    const moves = largestDayMoves([quote('a', 1), quote('b', -5), quote('c', 3)], 2);

    expect(moves.map((item) => item.instrumentId)).toEqual(['b', 'c']);
  });

  it('leaves the quotes it was given in their order', () => {
    const quotes = [quote('a', 1), quote('b', -5)];

    largestDayMoves(quotes, 2);

    expect(quotes[0]?.instrumentId).toBe('a');
  });
});

describe('biggestMove', () => {
  const quote = (id: string, last: number) => ({ instrumentId: id, symbol: id, last }) as Quote;

  it('picks the largest move, up or down', () => {
    const previous = [quote('a', 100), quote('b', 100)];
    const current = [quote('a', 101), quote('b', 97)];

    expect(biggestMove(previous, current)?.instrumentId).toBe('b');
  });

  it('compares moves in percent rather than in price', () => {
    const previous = [quote('expensive', 1000), quote('cheap', 1)];
    const current = [quote('expensive', 1001), quote('cheap', 1.05)];

    expect(biggestMove(previous, current)?.instrumentId).toBe('cheap');
  });

  it('finds nothing when no price moved', () => {
    expect(biggestMove([quote('a', 100)], [quote('a', 100)])).toBeNull();
  });

  it('ignores an instrument it has not seen before', () => {
    expect(biggestMove([], [quote('a', 100)])).toBeNull();
  });
});
