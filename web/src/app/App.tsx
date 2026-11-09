import { lazy, Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router';
import LandingPage from '@/features/landing/LandingPage';

// The workstation is loaded on demand, so that the public landing page stays light.
const Workstation = lazy(() => import('./Workstation'));

export function App() {
  return (
    <Suspense fallback={<div className="min-h-dvh bg-canvas" />}>
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/app/*" element={<Workstation />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </Suspense>
  );
}
