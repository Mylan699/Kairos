import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import type { ReactNode } from 'react';
import { MemoryRouter } from 'react-router';
import { vi } from 'vitest';
import { BookFilterProvider } from '@/app/BookFilter';
import { ToastProvider } from '@/design-system';
import { LiveProvider } from '@/realtime/live';
import { LiveStore, type HubLike } from '@/realtime/LiveStore';

export class TestHub implements HubLike {
  private handlers = new Map<string, (payload: unknown) => void>();
  private reconnecting = () => undefined as void;

  invoke = vi.fn(async () => undefined);
  start = vi.fn(async () => undefined);
  onreconnected = vi.fn();
  onclose = vi.fn();

  on(method: string, handler: (...args: never[]) => void) {
    this.handlers.set(method, handler as (payload: unknown) => void);
  }

  onreconnecting(handler: () => void) {
    this.reconnecting = handler;
  }

  emit(method: string, payload?: unknown) {
    this.handlers.get(method)?.(payload);
  }

  loseConnection() {
    this.reconnecting();
  }
}

/** Renders a screen with every provider it needs, on a fake live feed. */
export function renderWithLive(ui: ReactNode, route = '/app') {
  const hub = new TestHub();
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const store = new LiveStore(hub, queryClient);

  const view = render(
    <QueryClientProvider client={queryClient}>
      <LiveProvider store={store}>
        <ToastProvider>
          <BookFilterProvider>
            <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
          </BookFilterProvider>
        </ToastProvider>
      </LiveProvider>
    </QueryClientProvider>,
  );

  return { ...view, hub, queryClient };
}
