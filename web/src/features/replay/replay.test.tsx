import { act, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { IntradayPoint, MarketTick } from '@/api/types';
import { renderWithLive } from '@/test/renderWithLive';
import { isIsoDate, keyboardTarget, minuteFromPosition, positionOfMinute, realMinutesPerDay, seriesPath, speedLabel } from './replayModel';
import ReplayPage from './ReplayPage';

describe('timeline geometry', () => {
  it('turns a position into a minute of the day', () => {
    expect(minuteFromPosition(0, 1440)).toBe(0);
    expect(minuteFromPosition(720, 1440)).toBe(720);
  });

  it('stays within the day', () => {
    expect(minuteFromPosition(-10, 1440)).toBe(0);
    expect(minuteFromPosition(2000, 1440)).toBe(1439);
    expect(minuteFromPosition(10, 0)).toBe(0);
  });

  it('turns a minute back into a position', () => {
    expect(positionOfMinute(720, 800)).toBe(400);
  });
});

describe('keyboard control', () => {
  it.each([
    ['ArrowRight', 105],
    ['ArrowLeft', 95],
    ['PageUp', 160],
    ['PageDown', 40],
    ['Home', 0],
    ['End', 1439],
  ])('%s moves the replay', (key, expected) => {
    expect(keyboardTarget(key, 100)).toBe(expected);
  });

  it('stops at both ends of the day', () => {
    expect(keyboardTarget('ArrowLeft', 2)).toBe(0);
    expect(keyboardTarget('PageUp', 1430)).toBe(1439);
  });

  it('ignores other keys', () => {
    expect(keyboardTarget('a', 100)).toBeNull();
  });
});

describe('seriesPath', () => {
  const point = (minute: number, dayPnl: number) => ({ minute, dayPnl }) as IntradayPoint;

  it('scales the series between the top and the bottom', () => {
    expect(seriesPath([point(0, 0), point(720, 10)], 100, 10, 50)).toBe('M0.0 50.0 L50.0 10.0');
  });

  it('draws a flat series in the middle', () => {
    expect(seriesPath([point(0, 5), point(720, 5)], 100, 10, 50)).toBe('M0.0 30.0 L50.0 30.0');
  });

  it('draws nothing without points', () => {
    expect(seriesPath([], 100, 10, 50)).toBe('');
  });
});

describe('labels', () => {
  it('shows the speed', () => {
    expect(speedLabel(120)).toBe('×120');
  });

  it('gives the real duration of a day', () => {
    expect(realMinutesPerDay(120)).toBe(12);
    expect(realMinutesPerDay(1800)).toBe(1);
  });

  it('recognises an ISO date', () => {
    expect(isIsoDate('2016-06-24')).toBe(true);
    expect(isIsoDate('24/06/2016')).toBe(false);
  });
});

describe('ReplayPage', () => {
  const tick = {
    status: { date: '2020-03-16', minute: 810, speed: 120, isPlaying: true, sessionOpenMinute: 810, sessionCloseMinute: 1200 },
    quotes: [],
    curve: { date: '2020-03-16', points: [] },
    summary: { marketDate: '2020-03-16', books: [], total: { realized: 0, unrealized: 0, priceEffect: 0, fxEffect: 0, total: 0, day: 0 } },
  } as unknown as MarketTick;

  const days = [
    { date: '2016-06-24', name: 'Brexit', description: 'Le vote.', startMinute: 0 },
    { date: '2020-03-16', name: 'Krach', description: 'La chute.', startMinute: 780 },
  ];

  let posts: Array<{ url: string; body: unknown }>;

  beforeEach(() => {
    posts = [];
    vi.stubGlobal('fetch', vi.fn(async (url: string, init?: RequestInit) => {
      if (init?.method === 'POST') {
        posts.push({ url, body: JSON.parse(init.body as string) });
        return new Response('{}');
      }
      return new Response(JSON.stringify(url.endsWith('/days') ? days : []));
    }));
  });

  async function open() {
    const view = renderWithLive(<ReplayPage />);
    await waitFor(() => expect(view.hub.start).toHaveBeenCalled());
    act(() => view.hub.emit('tick', tick));
    return view;
  }

  it('shows the simulated date and time', async () => {
    await open();

    expect(screen.getByText(/lundi 16 mars 2020/i)).toBeInTheDocument();
    expect(screen.getByText('13:30')).toBeInTheDocument();
  });

  it('pauses a running replay', async () => {
    await open();

    await userEvent.click(screen.getByRole('button', { name: 'Mettre en pause' }));

    await waitFor(() => expect(posts[0]?.url).toBe('/api/replay/pause'));
  });

  it('changes the speed', async () => {
    await open();

    await userEvent.click(screen.getByRole('radio', { name: '×600' }));

    await waitFor(() => expect(posts[0]).toEqual({ url: '/api/replay/speed', body: { speed: 600 } }));
  });

  it('replays a remarkable day from its starting minute', async () => {
    await open();

    await userEvent.click(await screen.findByRole('button', { name: /Brexit/ }));

    await waitFor(() => expect(posts[0]).toEqual({ url: '/api/replay/load', body: { date: '2016-06-24', startMinute: 0 } }));
  });

  it('marks the day being replayed', async () => {
    await open();

    expect(await screen.findByRole('button', { name: /Krach/ })).toHaveAttribute('aria-pressed', 'true');
  });

  it('seeks with the keyboard on the timeline', async () => {
    await open();

    screen.getByRole('slider', { name: 'Heure du rejeu' }).focus();
    await userEvent.keyboard('{ArrowRight}');

    await waitFor(() => expect(posts[0]).toEqual({ url: '/api/replay/seek', body: { minute: 815 } }));
  });

  it('only loads a typed date when it is complete', async () => {
    await open();
    const button = screen.getByRole('button', { name: 'Rejouer' });

    expect(button).toBeDisabled();

    await userEvent.type(screen.getByLabelText('Autre date à rejouer'), '2015-03-02');
    await userEvent.click(button);

    await waitFor(() => expect(posts[0]?.body).toEqual({ date: '2015-03-02', startMinute: 0 }));
  });
});
