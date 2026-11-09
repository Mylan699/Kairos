import { HttpTransportType, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import type { QueryClient } from '@tanstack/react-query';
import { createContext, use, useEffect, useSyncExternalStore, type ReactNode } from 'react';
import { hubUrl } from '@/api/client';
import { LiveStore, type LiveState, type LiveTopic } from './LiveStore';

const MAXIMUM_RETRY_MILLISECONDS = 15_000;

export function createLiveStore(queryClient: QueryClient): LiveStore {
  const connection = new HubConnectionBuilder()
    .withUrl(hubUrl, { transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling })
    // Never gives up, waiting a little longer each time up to a ceiling.
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: ({ previousRetryCount }) =>
        Math.min(1_000 * 2 ** previousRetryCount, MAXIMUM_RETRY_MILLISECONDS),
    })
    .configureLogging(LogLevel.None)
    .build();

  return new LiveStore(connection, queryClient);
}

const LiveContext = createContext<LiveStore | null>(null);

export function LiveProvider({ store, children }: { store: LiveStore; children: ReactNode }) {
  useEffect(() => store.start(), [store]);

  return <LiveContext value={store}>{children}</LiveContext>;
}

export function useLiveStore(): LiveStore {
  const store = use(LiveContext);
  if (!store) {
    throw new Error('useLiveStore must be used within a LiveProvider.');
  }
  return store;
}

export function useLive(): LiveState {
  const store = useLiveStore();
  return useSyncExternalStore(store.subscribe, store.getState);
}

/** Receives a heavy topic for as long as the calling screen is displayed. */
export function useLiveTopic(topic: LiveTopic): void {
  const store = useLiveStore();
  useEffect(() => store.addTopic(topic), [store, topic]);
}
