import { fileURLToPath, URL } from 'node:url';
import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import type { Plugin } from 'vite';
import { defineConfig } from 'vitest/config';

const apiTarget = process.env.KAIROS_API_URL ?? 'http://localhost:5080';

/** Social networks need absolute links: the public address is given at build time. */
const siteUrl = (): Plugin => ({
  name: 'kairos-site-url',
  transformIndexHtml: (html) => html.replaceAll('__SITE_URL__', process.env.VITE_SITE_URL ?? ''),
});

export default defineConfig({
  plugins: [react(), tailwindcss(), siteUrl()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    // The tests compare the logo with the files of the brand folder, one level up.
    fs: { allow: ['..'] },
    proxy: {
      '/api': apiTarget,
      '/hubs': { target: apiTarget, ws: true },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/test/setup.ts',
    css: { include: [/tokens.css/] },
  },
});
