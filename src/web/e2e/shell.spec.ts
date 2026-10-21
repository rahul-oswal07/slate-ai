import AxeBuilder from '@axe-core/playwright'
import { expect, test } from '@playwright/test'
import { fakeTeacherApi } from './fakeApi.js'

type ManifestIcon = { src: string; sizes: string; purpose?: string }
type Manifest = { name?: string; start_url?: string; display?: string; icons?: ManifestIcon[] }

test.describe('signed out', () => {
  // page.route only sees requests the service worker doesn't handle.
  test.use({ serviceWorkers: 'block' })

  test.beforeEach(async ({ page }) => fakeTeacherApi(page))

  test('loads in Kannada and remembers a language change', async ({ page }) => {
    await page.goto('/')
    await expect(page.locator('html')).toHaveAttribute('lang', 'kn')
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('ಸೈನ್ ಇನ್ ಮಾಡಿ')

    await page.getByRole('button', { name: 'తెలుగు' }).click()
    await expect(page.locator('html')).toHaveAttribute('lang', 'te')
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('సైన్ ఇన్ చేయండి')

    await page.reload()
    await expect(page.getByRole('heading', { level: 1 })).toHaveText('సైన్ ఇన్ చేయండి')
  })

  test('has no serious or critical accessibility violations', async ({ page }, testInfo) => {
    await page.goto('/')
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    const results = await new AxeBuilder({ page }).analyze()
    const blocking = results.violations.filter((v) => v.impact === 'serious' || v.impact === 'critical')
    expect(blocking.map((v) => `${v.id}: ${v.help}`)).toEqual([])

    await page.screenshot({ path: testInfo.outputPath('shell-360.png'), fullPage: true })
  })

  test('dark theme follows the phone setting and stays accessible', async ({ page }, testInfo) => {
    await page.emulateMedia({ colorScheme: 'dark' })
    await page.goto('/')
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    // D-17: dark canvas from tokens.css, not the light one.
    const background = await page.evaluate(() => getComputedStyle(document.documentElement).backgroundColor)
    expect(background).toBe('rgb(21, 21, 19)')

    const results = await new AxeBuilder({ page }).analyze()
    const blocking = results.violations.filter((v) => v.impact === 'serious' || v.impact === 'critical')
    expect(blocking.map((v) => `${v.id}: ${v.help}`)).toEqual([])

    await page.screenshot({ path: testInfo.outputPath('shell-360-dark.png'), fullPage: true })
  })
})

test('is installable: valid manifest and an active service worker', async ({ page, request }) => {
  await page.goto('/')

  const href = await page.locator('link[rel="manifest"]').getAttribute('href')
  expect(href).toBeTruthy()
  const manifest = (await (await request.get(href ?? '')).json()) as Manifest
  expect(manifest.name).toBe('Tappida Paatha')
  expect(manifest.start_url).toBe('/')
  expect(manifest.display).toBe('standalone')
  expect(manifest.icons?.some((icon) => icon.sizes === '512x512' && icon.purpose === undefined)).toBe(true)
  expect(manifest.icons?.some((icon) => icon.purpose === 'maskable')).toBe(true)

  const active = await page.evaluate(async () => (await navigator.serviceWorker.ready).active !== null)
  expect(active).toBe(true)
})
