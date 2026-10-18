import { defineConfig, devices } from '@playwright/test'

const port = 4173
const isCi = Boolean(process.env.CI)

// Runs against the production build (`npm run build` first), served by `vite preview`.
export default defineConfig({
  testDir: './e2e',
  forbidOnly: isCi,
  retries: isCi ? 1 : 0,
  reporter: isCi ? [['github'], ['html', { open: 'never' }]] : [['list']],
  use: {
    baseURL: `http://localhost:${port}`,
    trace: 'retain-on-failure',
  },
  projects: [
    {
      // Spec targets low-end Android phones; CLAUDE.md screenshots are at 360 px.
      name: 'phone-360',
      use: { ...devices['Desktop Chrome'], viewport: { width: 360, height: 740 } },
    },
  ],
  webServer: {
    command: `npm run preview -- --port ${port} --strictPort`,
    url: `http://localhost:${port}`,
    reuseExistingServer: !isCi,
  },
})
