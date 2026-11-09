import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { applyMotionTokens } from '@/design-system/motion';
import './index.css';

applyMotionTokens();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <main className="grid min-h-dvh place-items-center bg-canvas">
      <p className="label">Kairos</p>
    </main>
  </StrictMode>,
);
