import { lazy, Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router';

// The workstation is loaded on demand, so that the public landing page stays light.
const Workstation = lazy(() => import('./Workstation'));

export function App() {
  return (
    <Suspense fallback={<div className="min-h-dvh bg-canvas" />}>
      <Routes>
        <Route path="/" element={<Navigate to="/app" replace />} />
        <Route path="/app/*" element={<Workstation />} />
      </Routes>
    </Suspense>
  );
}
