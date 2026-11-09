import { QueryClient } from '@tanstack/react-query';

/** Live values come from the feed, so fetched data only goes stale when the feed says so. */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 60_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});
