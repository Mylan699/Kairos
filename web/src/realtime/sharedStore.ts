import { queryClient } from '@/lib/queryClient';
import { createLiveStore } from './live';
import type { LiveStore } from './LiveStore';

let store: LiveStore | null = null;

/** The one live connection of the page, created the first time a screen needs it. */
export function getLiveStore(): LiveStore {
  store ??= createLiveStore(queryClient);
  return store;
}
