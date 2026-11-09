import { QueryClientProvider } from '@tanstack/react-query';
import { MotionConfig } from 'motion/react';
import { lazy, Suspense } from 'react';
import { Route, Routes } from 'react-router';
import { EmptyState, ToastProvider } from '@/design-system';
import { TradeTicketProvider } from '@/features/trade-ticket/TicketContext';
import { queryClient } from '@/lib/queryClient';
import { LiveProvider } from '@/realtime/live';
import { getLiveStore } from '@/realtime/sharedStore';
import { AppLayout } from './AppLayout';
import { BookFilterProvider } from './BookFilter';

const DashboardPage = lazy(() => import('@/features/dashboard/DashboardPage'));
const BlotterPage = lazy(() => import('@/features/blotter/BlotterPage'));
const PositionsPage = lazy(() => import('@/features/positions/PositionsPage'));

const ComingSoon = () => <EmptyState title="Écran en cours de construction" />;

export default function Workstation() {
  return (
    <QueryClientProvider client={queryClient}>
      <LiveProvider store={getLiveStore()}>
        <MotionConfig reducedMotion="user">
          <ToastProvider>
            <BookFilterProvider>
              <TradeTicketProvider>
                <Routes>
                  <Route element={<AppLayout />}>
                    <Route index element={<Suspense><DashboardPage /></Suspense>} />
                    <Route path="blotter" element={<Suspense><BlotterPage /></Suspense>} />
                    <Route path="positions" element={<Suspense><PositionsPage /></Suspense>} />
                    <Route path="markets" element={<ComingSoon />} />
                    <Route path="risk" element={<ComingSoon />} />
                    <Route path="replay" element={<ComingSoon />} />
                  </Route>
                </Routes>
              </TradeTicketProvider>
            </BookFilterProvider>
          </ToastProvider>
        </MotionConfig>
      </LiveProvider>
    </QueryClientProvider>
  );
}
