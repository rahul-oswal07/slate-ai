import type { Page } from '@playwright/test'

export const teacher = {
  teacherId: '6abf3cc9-9aa8-5786-a833-62325a6418f4',
  displayName: 'Shobha',
  classId: '0a1e03b3-23fa-5a7d-87e3-490d6ddae335',
  setupComplete: false,
}

/**
 * Stands in for the API (the preview server has none): signed out until the right PIN, then the
 * welcome sets setupComplete. Tests using it block service workers so page.route sees every request.
 */
export async function fakeTeacherApi(page: Page, { pin = '2468' }: { pin?: string } = {}) {
  let me: typeof teacher | null = null

  await page.route('**/api/me', (route) =>
    me ? route.fulfill({ json: me }) : route.fulfill({ status: 401, body: '' }),
  )
  await page.route('**/api/auth/pin', async (route) => {
    const body = route.request().postDataJSON() as { phone: string; pin: string }
    if (body.pin !== pin) {
      return route.fulfill({ status: 401, json: { title: 'Phone or PIN is wrong' } })
    }
    me = me ?? { ...teacher }
    return route.fulfill({ json: me })
  })
  await page.route('**/api/me/welcome', (route) => {
    me = { ...teacher, setupComplete: true }
    return route.fulfill({ json: me })
  })
  await page.route('**/api/auth/logout', (route) => {
    me = null
    return route.fulfill({ status: 204, body: '' })
  })
}
