/// <reference types="vitest/config" />
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

// scripts/run-local.sh sets API_URL when the API runs on another port.
const apiUrl = process.env.API_URL ?? 'http://localhost:5080'

// Manifest colours mirror --color-primary / --color-canvas (light) in src/styles/tokens.css.
const themeColor = '#115e59'
const backgroundColor = '#dce4df'

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['icons/icon.svg'],
      manifest: {
        name: 'Tappida Paatha',
        short_name: 'Tappida Paatha',
        description: 'A home-language catch-up of the lesson a child missed.',
        lang: 'kn',
        start_url: '/',
        scope: '/',
        display: 'standalone',
        theme_color: themeColor,
        background_color: backgroundColor,
        icons: [
          { src: '/icons/icon-192.png', sizes: '192x192', type: 'image/png' },
          { src: '/icons/icon-512.png', sizes: '512x512', type: 'image/png' },
          { src: '/icons/icon-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        navigateFallback: '/index.html',
        navigateFallbackDenylist: [/^\/api\//, /^\/health/, /^\/swagger/, /^\/openapi\//],
      },
    }),
  ],
  build: {
    // D-10: the API serves the SPA, so the build goes straight into its wwwroot (git-ignored).
    outDir: '../api/SlateAi.Api/wwwroot',
    emptyOutDir: true,
  },
  server: {
    proxy: { '/api': apiUrl, '/health': apiUrl, '/swagger': apiUrl, '/openapi': apiUrl },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    // Node 25 ships an experimental global localStorage that shadows jsdom's; turn it off in test workers.
    execArgv: ['--no-experimental-webstorage'],
  },
})
