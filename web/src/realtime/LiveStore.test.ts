import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { MarketTick, Trade } from '@/api/types';
import { LiveStore, type HubLike } from './LiveStore';

class FakeHub implements HubLike {
  handlers = new Map<string, (...args: never[]) => void>();
  invoke = vi.fn(async () => undefined);
  start = vi.fn(async () => undefined);
  private reconnecting = () => undefined as void;
  private reconnected = () => undefined as void;
  private closed = () => undefined as void;

  on(method: string, handler: (...args: never[]) => void) {
    this.handlers.set(method, handler);
  }

  onreconnecting(handler: () => void) {
    this.reconnecting = handler;
  }

  onreconnected(handler: () => void) {
    this.reconnected = handler;
  }

  onclose(handler: () => void) {
    this.closed = handler;
  }

  emit(method: string, payload?: unknown) {
    (this.handlers.get(method) as (value: unknown) => void)(payload);
  }

  loseConnection = () => this.reconnecting();
  recover = () => this.reconnected();
  close = () => this.closed();
}

const tick = (date: string) => ({ status: { date } }) as MarketTick;
const flush = () => new Promise((resolve) => setTimeout(resolve));

describe('LiveStore', () => {
  let hub: FakeHub;
  let queryClient: { invalidateQueries: ReturnType<typeof vi.fn> };
  let store: LiveStore;

  beforeEach(() => {
    hub = new FakeHub();
    queryClient = { invalidateQueries: vi.fn() };
    store = new LiveStore(hub, queryClient as never);
  });

  it('becomes connected once the hub has started', async () => {
    expect(store.getState().status).toBe('connecting');

    store.start();
    await flush();

    expect(store.getState().status).toBe('connected');
  });

  it('starts only once, even when React mounts twice', async () => {
    store.start();
    store.start();
    await flush();

    expect(hub.start).toHaveBeenCalledOnce();
  });

  it('keeps the last tick and notifies subscribers', async () => {
    const listener = vi.fn();
    store.subscribe(listener);
    store.start();
    await flush();
    listener.mockClear();

    hub.emit('tick', tick('2016-06-24'));

    expect(store.getState().tick?.status.date).toBe('2016-06-24');
    expect(listener).toHaveBeenCalledOnce();
  });

  it('refetches everything when the market date changes, and only then', async () => {
    store.start();
    await flush();

    hub.emit('tick', tick('2016-06-24'));
    hub.emit('tick', tick('2016-06-24'));
    expect(queryClient.invalidateQueries).not.toHaveBeenCalled();

    hub.emit('tick', tick('2020-03-16'));
    expect(queryClient.invalidateQueries).toHaveBeenCalledOnce();
  });

  it('reports an outage, then refetches after reconnecting', async () => {
    store.start();
    await flush();

    hub.loseConnection();
    expect(store.getState().status).toBe('reconnecting');

    hub.recover();
    expect(store.getState().status).toBe('connected');
    expect(queryClient.invalidateQueries).toHaveBeenCalledOnce();
  });

  it('reports a closed connection', async () => {
    store.start();
    await flush();

    hub.close();

    expect(store.getState().status).toBe('disconnected');
  });

  it('retries when the first connection fails', async () => {
    vi.useFakeTimers();
    hub.start.mockRejectedValueOnce(new Error('offline'));

    store.start();
    await vi.advanceTimersByTimeAsync(0);
    expect(store.getState().status).toBe('disconnected');

    await vi.advanceTimersByTimeAsync(3_000);
    expect(store.getState().status).toBe('connected');

    vi.useRealTimers();
  });

  it('subscribes to a topic once, and leaves when the last screen does', async () => {
    store.start();
    await flush();

    const removeFirst = store.addTopic('positions');
    const removeSecond = store.addTopic('positions');
    expect(hub.invoke).toHaveBeenCalledTimes(1);
    expect(hub.invoke).toHaveBeenCalledWith('Subscribe', 'positions');

    removeFirst();
    expect(hub.invoke).toHaveBeenCalledTimes(1);

    removeSecond();
    expect(hub.invoke).toHaveBeenLastCalledWith('Unsubscribe', 'positions');
  });

  it('subscribes to topics requested before the connection opened', async () => {
    store.addTopic('risk');

    store.start();
    await flush();

    expect(hub.invoke).toHaveBeenCalledWith('Subscribe', 'risk');
  });

  it('subscribes again after a reconnection, as groups do not survive it', async () => {
    store.start();
    await flush();
    store.addTopic('positions');
    hub.invoke.mockClear();

    hub.loseConnection();
    hub.recover();

    expect(hub.invoke).toHaveBeenCalledWith('Subscribe', 'positions');
  });

  it('announces a changed trade and refreshes what depends on it', async () => {
    const listener = vi.fn();
    store.onTradeChanged(listener);
    store.start();
    await flush();
    const trade = { id: 't1' } as Trade;

    hub.emit('tradeChanged', trade);

    expect(listener).toHaveBeenCalledWith(trade);
    expect(queryClient.invalidateQueries).toHaveBeenCalledWith({ queryKey: ['trades'] });
    expect(queryClient.invalidateQueries).toHaveBeenCalledWith({ queryKey: ['portfolio'] });
  });

  it('refetches everything after the nightly reset', async () => {
    store.start();
    await flush();

    hub.emit('dataReset');

    expect(queryClient.invalidateQueries).toHaveBeenCalledWith();
  });
});
