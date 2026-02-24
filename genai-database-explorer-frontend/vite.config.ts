/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  // Expose whether a real Aspire backend is available so the app can
  // conditionally start MSW when running standalone with `pnpm dev`.
  define: {
    'import.meta.env.VITE_HAS_ASPIRE_BACKEND': JSON.stringify(
      !!(process.env['services__genaidbexplorer-api__https__0'] || process.env['services__genaidbexplorer-api__http__0']),
    ),
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    include: ['src/**/*.{test,spec}.{ts,tsx}', 'tests/**/*.{test,spec}.{ts,tsx}'],
  },
  server: {
    // Proxy API requests to backend service.
    // When running via Aspire AppHost, service discovery env vars are injected:
    //   services__genaidbexplorer-api__https__0, services__genaidbexplorer-api__http__0
    // For standalone dev, use VITE_API_BASE_URL or fallback to localhost:5000.
    proxy: {
      '/api': {
        target:
          process.env['services__genaidbexplorer-api__https__0'] ||
          process.env['services__genaidbexplorer-api__http__0'] ||
          process.env.VITE_API_BASE_URL ||
          'http://localhost:5000',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
