import AxeBuilder from '@axe-core/playwright'
import { expect, test, type Page } from '@playwright/test'
import { attendanceDay, teacher } from './fakeApi.js'

test.use({ serviceWorkers: 'block' })

const signedIn = { ...teacher, setupComplete: true }

async function expectNoBlockingViolations(page: Page) {
  const results = await new AxeBuilder({ page }).analyze()
  const blocking = results.violations.filter((v) => v.impact === 'serious' || v.impact === 'critical')
  expect(blocking.map((v) => `${v.id}: ${v.help}`)).toEqual([])
}

/** Signed in; the register comes from `register()`; saves succeed unless `offline()` is true. */
/** Taps a child's tile, as a teacher does (the checkbox inside is visually hidden). */
const tile = (page: Page, name: string) => page.locator('label', { has: page.getByRole('checkbox', { name }) })

async function fakeToday(page: Page, register: () => object, offline: () => boolean = () => false) {
  const posts: unknown[] = []
  await page.addInitScript(() => localStorage.setItem('slate.language', 'en'))
  await page.route('**/api/me', (route) => route.fulfill({ json: signedIn }))
  await page.route('**/api/classes/*/attendance/*', (route) => route.fulfill({ json: register() }))
  await page.route('**/api/classes/*/attendance', (route) => {
    if (offline()) {
      return route.abort('internetdisconnected')
    }
    posts.push(route.request().postDataJSON())
    return route.fulfill({ json: { date: '', present: 0, absent: 0 } })
  })
  return posts
}

test('SATS register → correct one child → "corrected by you"', async ({ page }, testInfo) => {
  let register = attendanceDay()
  const posts = await fakeToday(page, () => register)

  await page.goto('/')
  await expect(page.getByRole('region', { name: /SATS \(simulated\), 12:00/ })).toContainText('3 absent')
  await expectNoBlockingViolations(page)
  await page.screenshot({ path: testInfo.outputPath('today-sats-360.png'), fullPage: true })

  await page.getByRole('button', { name: 'Wrong? Correct it' }).click()
  await expect(page.getByRole('heading', { name: 'Correct attendance' })).toBeFocused()
  await expectNoBlockingViolations(page)
  await page.screenshot({ path: testInfo.outputPath('today-correcting-360.png'), fullPage: true })

  register = attendanceDay({ corrected: true, absentChildIds: ['child-3', 'child-10'] })
  await tile(page, '4 Ravi, absent').click()
  await page.getByRole('button', { name: 'Save' }).click()

  await expect(page.getByRole('region', { name: /corrected by you/ })).toContainText('2 absent')
  await expect(page.getByText('Saved', { exact: true })).toBeVisible()
  expect(posts).toHaveLength(1)
  await page.screenshot({ path: testInfo.outputPath('today-corrected-360.png'), fullPage: true })
})

test('SATS not in yet → mark it here', async ({ page }, testInfo) => {
  await fakeToday(page, () =>
    attendanceDay({ status: 'waiting', receivedAt: null, syncAt: '12:00:00', retryAt: '14:00:00', absentChildIds: [] }),
  )

  await page.goto('/')
  await expect(page.getByText('SATS attendance has not come in yet')).toBeVisible()
  await expectNoBlockingViolations(page)
  await page.screenshot({ path: testInfo.outputPath('today-waiting-360.png'), fullPage: true })

  await page.getByRole('button', { name: 'Mark it here if you prefer' }).click()
  await expect(page.getByText('0 absent · 12 present')).toBeVisible()
})

test('still missing after the 2 pm retry → Mark attendance (D-67)', async ({ page }, testInfo) => {
  await fakeToday(page, () => attendanceDay({ status: 'missing', receivedAt: null, absentChildIds: [] }))

  await page.goto('/')
  await expect(page.getByText('Still missing after 2 pm')).toBeVisible()
  await expectNoBlockingViolations(page)
  await page.screenshot({ path: testInfo.outputPath('today-missing-360.png'), fullPage: true })

  await page.getByRole('button', { name: 'Mark attendance' }).click()
  await expect(page.getByRole('heading', { name: 'Mark attendance here' })).toBeVisible()
})

test('offline save waits in the outbox, then sends once', async ({ page }, testInfo) => {
  let offline = true
  const posts = await fakeToday(page, () => attendanceDay(), () => offline)

  await page.goto('/')
  await page.getByRole('button', { name: 'Wrong? Correct it' }).click()
  await tile(page, '1 Ananya, present').click()
  await page.getByRole('button', { name: 'Save' }).click()

  await expect(page.getByText('1 waiting to send')).toBeVisible()
  await expect(page.getByText('4 absent')).toBeVisible()
  await page.screenshot({ path: testInfo.outputPath('today-waiting-to-send-360.png'), fullPage: true })

  offline = false
  await page.evaluate(() => window.dispatchEvent(new Event('online')))

  await expect(page.getByText('Saved', { exact: true })).toBeVisible()
  expect(posts).toHaveLength(1)
})

test('Today stays accessible in the dark theme', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'dark' })
  await fakeToday(page, () => attendanceDay())

  await page.goto('/')
  await expect(page.getByText('3 absent')).toBeVisible()
  await expectNoBlockingViolations(page)
  await page.getByRole('button', { name: 'Wrong? Correct it' }).click()
  await expectNoBlockingViolations(page)
})
