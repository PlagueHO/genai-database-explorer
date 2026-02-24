import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';

/**
 * Initialize MSW for standalone development mode.
 * When running via Aspire AppHost the real backend is available and MSW is skipped.
 * When running with `pnpm dev` without Aspire, MSW intercepts API calls with mock data.
 */
async function initializeApp() {
  const isBackendConfigured =
    import.meta.env.VITE_HAS_ASPIRE_BACKEND === 'true' ||
    import.meta.env.VITE_HAS_ASPIRE_BACKEND === true ||
    (import.meta.env.VITE_API_BASE_URL && import.meta.env.VITE_API_BASE_URL !== '');

  if (import.meta.env.DEV && !isBackendConfigured) {
    try {
      const { worker } = await import('./__mocks__/browser');
      await worker.start({ onUnhandledRequest: 'bypass' });
      console.log('[MSW] Mock Service Worker started — running in standalone dev mode');
    } catch (error) {
      console.warn('[MSW] Failed to start Mock Service Worker:', error);
    }
  }

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <App />
    </StrictMode>,
  );
}

initializeApp();
