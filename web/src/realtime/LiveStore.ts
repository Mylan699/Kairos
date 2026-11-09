import type { QueryClient } from '@tanstack/react-query';
import type { IntradayPoint, MarketTick, Position, RiskReport, Trade } from '@/api/types';

export type ConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';
export type LiveTopic = 'positions' | 'risk';

export interface LiveState {
  status: ConnectionStatus;
  tick: MarketTick | null;
  positions: Position[] | null;
  risk: RiskReport | null;
  /** The points received from the feed since this page opened or the day last restarted. */
  intradayTail: IntradayPoint[];
}

/** The part of a SignalR connection this store relies on, so that tests can provide a fake one. */
export interface HubLike {
  on(method: string, handler: (...args: never[]) => void): void;
  invoke(method: string, ...args: unknown[]): Promise<unknown>;
  start(): Promise<void>;
  onreconnecting(handler: () => void): void;
  onreconnected(handler: () => void): void;
  onclose(handler: () => void): void;
}

const START_RETRY_MILLISECONDS = 3_000;

const TEN_YEARS_IN_MONTHS = 120;

function toIntradayPoint(tick: MarketTick): IntradayPoint {
  const equities = tick.quotes.filter((quote) => quote.assetClass === 'Equity');
  const basket = equities.reduce((total, quote) => total + quote.changePercent, 0) / (equities.length || 1);

  return {
    minute: tick.status.minute,
    eurUsd: tick.quotes.find((quote) => quote.assetClass === 'Fx')?.last ?? 0,
    equityBasketPercent: basket,
    tenYearPercent: tick.curve.points.find((point) => point.months === TEN_YEARS_IN_MONTHS)?.yieldPercent ?? 0,
    dayPnl: tick.summary.total.day,
  };
}

/**
 * Holds what the server pushes and tells React when it changes. It lives outside React,
 * so that there is one connection whichever screens are displayed.
 */
export class LiveStore {
  private state: LiveState = { status: 'connecting', tick: null, positions: null, risk: null, intradayTail: [] };
  private started = false;
  private readonly listeners = new Set<() => void>();
  private readonly tradeListeners = new Set<(trade: Trade) => void>();
  private readonly topicSubscribers = new Map<LiveTopic, number>();

  constructor(
    private readonly connection: HubLike,
    private readonly queryClient: Pick<QueryClient, 'invalidateQueries'>,
  ) {}

  getState = (): LiveState => this.state;

  subscribe = (listener: () => void): (() => void) => {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  };

  onTradeChanged = (listener: (trade: Trade) => void): (() => void) => {
    this.tradeListeners.add(listener);
    return () => this.tradeListeners.delete(listener);
  };

  start(): void {
    if (this.started) {
      return;
    }

    this.started = true;
    this.connection.on('tick', (tick: MarketTick) => this.receiveTick(tick));
    this.connection.on('positions', (positions: Position[]) => this.update({ positions }));
    this.connection.on('risk', (risk: RiskReport) => this.update({ risk }));
    this.connection.on('tradeChanged', (trade: Trade) => this.receiveTrade(trade));
    this.connection.on('dataReset', () => void this.queryClient.invalidateQueries());

    this.connection.onreconnecting(() => this.update({ status: 'reconnecting' }));
    this.connection.onclose(() => this.update({ status: 'disconnected' }));
    this.connection.onreconnected(() => this.resumeAfterReconnection());

    void this.connect();
  }

  /** A screen declares that it displays a heavy topic. The server only sends it while one does. */
  addTopic(topic: LiveTopic): () => void {
    const count = this.topicSubscribers.get(topic) ?? 0;
    this.topicSubscribers.set(topic, count + 1);

    if (count === 0 && this.state.status === 'connected') {
      void this.connection.invoke('Subscribe', topic);
    }

    return () => this.removeTopic(topic);
  }

  private removeTopic(topic: LiveTopic): void {
    const count = (this.topicSubscribers.get(topic) ?? 1) - 1;
    this.topicSubscribers.set(topic, count);

    if (count === 0 && this.state.status === 'connected') {
      void this.connection.invoke('Unsubscribe', topic);
    }
  }

  private async connect(): Promise<void> {
    try {
      await this.connection.start();
      this.update({ status: 'connected' });
      this.subscribeToActiveTopics();
    } catch {
      // Automatic reconnection only covers a connection that once worked.
      this.update({ status: 'disconnected' });
      window.setTimeout(() => void this.connect(), START_RETRY_MILLISECONDS);
    }
  }

  private resumeAfterReconnection(): void {
    this.update({ status: 'connected' });
    this.subscribeToActiveTopics();
    // Trades may have changed during the outage.
    void this.queryClient.invalidateQueries();
  }

  private subscribeToActiveTopics(): void {
    for (const [topic, count] of this.topicSubscribers) {
      if (count > 0) {
        void this.connection.invoke('Subscribe', topic);
      }
    }
  }

  private receiveTick(tick: MarketTick): void {
    const previous = this.state.tick?.status;
    const point = toIntradayPoint(tick);
    const dateChanged = previous !== undefined && previous.date !== tick.status.date;
    const wentBack = previous !== undefined && !dateChanged && tick.status.minute < previous.minute;

    this.update({ tick, intradayTail: this.nextTail(point, dateChanged || wentBack) });

    // Another date is another market: everything fetched for the old one is out of date.
    if (dateChanged) {
      void this.queryClient.invalidateQueries();
    } else if (wentBack) {
      void this.queryClient.invalidateQueries({ queryKey: ['intraday'] });
    }
  }

  private nextTail(point: IntradayPoint, restart: boolean): IntradayPoint[] {
    if (restart) {
      return [point];
    }

    const tail = this.state.intradayTail;
    return tail[tail.length - 1]?.minute === point.minute ? tail : [...tail, point];
  }

  private receiveTrade(trade: Trade): void {
    this.tradeListeners.forEach((listener) => listener(trade));
    void this.queryClient.invalidateQueries({ queryKey: ['trades'] });
    void this.queryClient.invalidateQueries({ queryKey: ['portfolio'] });
    void this.queryClient.invalidateQueries({ queryKey: ['risk'] });
  }

  private update(change: Partial<LiveState>): void {
    this.state = { ...this.state, ...change };
    this.listeners.forEach((listener) => listener());
  }
}
