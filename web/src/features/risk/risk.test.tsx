import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { RiskReport } from '@/api/types';
import { renderWithLive } from '@/test/renderWithLive';
import { sensitivitiesFor, validateShocks } from './riskModel';
import { StressPanel } from './StressPanel';

describe('validateShocks', () => {
  it('reads the three shocks', () => {
    expect(validateShocks({ equity: '-10', eurUsd: '-5', rates: '100' }).request).toEqual({
      equityShockPercent: -10,
      eurUsdShockPercent: -5,
      rateShockBasisPoints: 100,
    });
  });

  it('treats an empty field as no shock', () => {
    expect(validateShocks({ equity: '', eurUsd: '-5', rates: '' }).request).toEqual({
      equityShockPercent: 0,
      eurUsdShockPercent: -5,
      rateShockBasisPoints: 0,
    });
  });

  it('accepts a decimal comma', () => {
    expect(validateShocks({ equity: '-2,5', eurUsd: '', rates: '' }).request?.equityShockPercent).toBe(-2.5);
  });

  it.each([
    [{ equity: '-95', eurUsd: '', rates: '' }, 'equity'],
    [{ equity: '', eurUsd: '60', rates: '' }, 'eurUsd'],
    [{ equity: '', eurUsd: '', rates: '900' }, 'rates'],
    [{ equity: 'abc', eurUsd: '', rates: '' }, 'equity'],
  ])('refuses %o', (fields, field) => {
    const { request, errors } = validateShocks(fields);

    expect(request).toBeNull();
    expect(errors).toHaveProperty(field);
  });
});

describe('sensitivitiesFor', () => {
  const report = {
    equityDelta: 100,
    fxDelta: 200,
    dv01: 300,
    books: [{ bookId: 'a', book: 'A', equityDelta: 1, fxDelta: 2, dv01: 3 }],
  } as RiskReport;

  it('returns the desk without a selected book', () => {
    expect(sensitivitiesFor(report, null).dv01).toBe(300);
  });

  it('returns the selected book', () => {
    expect(sensitivitiesFor(report, 'a').dv01).toBe(3);
  });

  it('returns zero for a book without positions', () => {
    expect(sensitivitiesFor(report, 'none').equityDelta).toBe(0);
  });
});

describe('StressPanel', () => {
  const scenarios = [{ id: 'brexit', name: 'Référendum sur le Brexit', description: 'Le choc du vote.', from: '2016-06-23', to: '2016-06-27' }];
  const result = {
    scenario: 'Scénario paramétrable',
    totalEur: -1250,
    byBook: [{ bookId: 'a', book: 'Book A', eur: -1250 }],
    byAssetClass: [{ key: 'Equity', eur: -1250 }],
  };

  let posts: Array<{ url: string; body: unknown }>;

  beforeEach(() => {
    posts = [];
    vi.stubGlobal('fetch', vi.fn(async (url: string, init?: RequestInit) => {
      if (init?.method === 'POST') {
        posts.push({ url, body: JSON.parse(init.body as string) });
        return new Response(JSON.stringify(result));
      }
      return new Response(JSON.stringify(scenarios));
    }));
  });

  it('starts with an invitation to run a scenario', () => {
    renderWithLive(<StressPanel />);

    expect(screen.getByText('Aucun scénario lancé')).toBeInTheDocument();
  });

  it('runs a parametric scenario and shows its result by book and asset class', async () => {
    renderWithLive(<StressPanel />);

    await userEvent.click(screen.getByRole('button', { name: 'Lancer le scénario' }));

    expect(await screen.findByText('Book A')).toBeInTheDocument();
    expect(screen.getByText('Actions')).toBeInTheDocument();
    expect(posts[0]).toEqual({
      url: '/api/risk/stress/parametric',
      body: { equityShockPercent: -10, eurUsdShockPercent: -5, rateShockBasisPoints: 100 },
    });
  });

  it('fills the fields from a preset', async () => {
    renderWithLive(<StressPanel />);

    await userEvent.click(screen.getByRole('button', { name: 'Taux +100 pb' }));

    expect(screen.getByLabelText('Actions (%)')).toHaveValue('');
    expect(screen.getByLabelText('Taux (pb)')).toHaveValue('100');
  });

  it('blocks an out-of-range shock', async () => {
    renderWithLive(<StressPanel />);

    await userEvent.clear(screen.getByLabelText('Taux (pb)'));
    await userEvent.type(screen.getByLabelText('Taux (pb)'), '900');

    expect(screen.getByRole('button', { name: 'Lancer le scénario' })).toBeDisabled();
    expect(screen.getByText('Entre -500 et +500 pb.')).toBeInTheDocument();
  });

  it('runs a historical scenario over its period', async () => {
    renderWithLive(<StressPanel />);

    await userEvent.click(screen.getByRole('radio', { name: 'Historique' }));
    await userEvent.click(await screen.findByRole('button', { name: /Référendum sur le Brexit/ }));

    await waitFor(() => expect(posts[0]?.url).toBe('/api/risk/stress/historical'));
    expect(posts[0]?.body).toEqual({ from: '2016-06-23', to: '2016-06-27', name: 'Référendum sur le Brexit' });
  });

  it('runs at once a scenario chosen from the command palette', async () => {
    renderWithLive(<StressPanel />, '/app/risk?scenario=brexit');

    await waitFor(() => expect(posts[0]?.url).toBe('/api/risk/stress/historical'));
  });
});
