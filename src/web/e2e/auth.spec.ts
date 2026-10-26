import AxeBuilder from '@axe-core/playwright'
import { expect, test, type Page } from '@playwright/test'
import { fakeTeacherApi } from './fakeApi.js'

test.use({ serviceWorkers: 'block' })

async function expectNoBlockingViolations(page: Page) {
  const results = await new AxeBuilder({ page }).analyze()
  const blocking = results.violations.filter((v) => v.impact === 'serious' || v.impact === 'critical')
  expect(blocking.map((v) => `${v.id}: ${v.help}`)).toEqual([])
}

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => {
    if (!sessionStorage.getItem('seeded')) {
      localStorage.setItem('slate.language', 'en')
      sessionStorage.setItem('seeded', '1')
    }
  })
})

test('first sign-in: phone + PIN → welcome → Start → greeting step → evening-message step, then straight to Today', async ({ page }, testInfo) => {
  await fakeTeacherApi(page)
  // A headless browser leaves a microphone prompt unanswered; say it was refused.
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'mediaDevices', {
      configurable: true,
      value: { getUserMedia: () => Promise.reject(new DOMException('denied', 'NotAllowedError')) },
    })
  })

  await page.goto('/')
  await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible()
  await expectNoBlockingViolations(page)
  await page.screenshot({ path: testInfo.outputPath('login-first-360.png'), fullPage: true })

  await page.getByLabel('Mobile number').fill('90000 00000')
  await page.getByLabel('PIN').fill('1111')
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page.getByRole('alert')).toHaveText('The mobile number or PIN is wrong. Try again.')
  await expectNoBlockingViolations(page)
  await page.screenshot({ path: testInfo.outputPath('login-wrong-pin-360.png'), fullPage: true })

  await page.getByLabel('PIN').fill('2468')
  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page).toHaveURL(/\/welcome$/)
  await expect(page.getByRole('heading', { level: 1, name: 'Namaskara, Shobha ma’am' })).toBeFocused()
  await expectNoBlockingViolations(page)
  await page.screenshot({ path: testInfo.outputPath('welcome-360.png'), fullPage: true })

  // Start opens the greeting step (T0 step 1). The mic is blocked (D-34) so she continues without a greeting; that
  // finishes setup and opens the evening-message step (T-44), not Today.
  await page.getByRole('button', { name: 'Start' }).click()
  await expect(page).toHaveURL(/\/welcome\/greeting$/)
  await expect(page.getByRole('heading', { level: 1, name: 'A 10-second greeting in your voice' })).toBeFocused()
  await expectNoBlockingViolations(page)
  await page.screenshot({ path: testInfo.outputPath('greeting-360.png'), fullPage: true })
  await page.getByRole('button', { name: 'Record' }).click()
  await page.getByRole('button', { name: 'Continue without a greeting' }).click()
  await expect(page).toHaveURL(/\/setup\/evening$/)

  await page.goto('/')
  await expect(page.getByRole('heading', { level: 1, name: /^Today, / })).toBeVisible()

  await page.reload()
  await expect(page.getByRole('heading', { level: 1, name: /^Today, / })).toBeVisible()
})

test('returning on the same phone: PIN only', async ({ page }, testInfo) => {
  await fakeTeacherApi(page)
  await page.addInitScript(() =>
    localStorage.setItem('slate.teacher', JSON.stringify({ phone: '9000000000', displayName: 'Shobha' })),
  )

  await page.goto('/login')

  await expect(page.getByRole('heading', { level: 1, name: 'Namaskara, Shobha' })).toBeVisible()
  await expect(page.getByLabel('Mobile number')).toHaveCount(0)
  await expectNoBlockingViolations(page)
  await page.screenshot({ path: testInfo.outputPath('login-returning-360.png'), fullPage: true })

  await page.getByLabel('PIN').fill('2468')
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page).toHaveURL(/\/welcome$/)
})

test('sign-in and welcome stay accessible in the dark theme', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'dark' })
  await fakeTeacherApi(page)

  await page.goto('/login')
  await expect(page.getByRole('heading', { level: 1, name: 'Sign in' })).toBeVisible()
  await expectNoBlockingViolations(page)

  await page.getByLabel('Mobile number').fill('9000000000')
  await page.getByLabel('PIN').fill('2468')
  await page.getByRole('button', { name: 'Sign in' }).click()
  await expect(page.getByRole('heading', { level: 1, name: 'Namaskara, Shobha ma’am' })).toBeVisible()
  await expectNoBlockingViolations(page)
})
