import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import type { DemoChild, DemoInfo, FamilyMessage } from './types'

const i18n = initI18n()

const info: DemoInfo = {
  classId: teacher.classId,
  parentChildId: 'manjunath-1',
  lastResetAt: null,
  parentChildName: 'Manjunath',
  parentLanguage: 'kn',
}
const rohan: DemoChild = { id: 'rohan-1', firstName: 'Rohan', language: 'te' }
const evening: FamilyMessage = { kind: 'evening', date: '2026-10-22', sentAt: '2026-10-22T12:30:00Z', token: 'tok-rohan' }

function setup(extra: Record<string, FakeRoute> = {}) {
  return fakeApi({
    'POST /api/demo/login': () => ({ status: 200, body: teacher }),
    'GET /api/demo': () => ({ status: 200, body: info }),
    'GET /api/demo/children/rohan-1': () => ({ status: 200, body: rohan }),
    'GET /api/demo/family/rohan-1': () => ({ status: 200, body: [evening] }),
    ...extra,
  })
}

beforeEach(async () => {
  localStorage.clear()
  await i18n.changeLanguage('en')
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('DemoFamilyScreen (T-126)', () => {
  it("shows the family phone of the child in the link, not only Manjunath's", async () => {
    const user = userEvent.setup()
    const calls = setup()
    renderApp('/demo/family/rohan-1')

    expect(await screen.findByRole('heading', { level: 1, name: "Rohan's family's phone · Telugu" })).toBeInTheDocument()
    expect(screen.getByRole('main', { name: "Rohan's family's phone · Telugu" })).toBeInTheDocument()
    await user.click(await screen.findByRole('button', { name: /Evening message · 22 Oct/ }))
    expect(screen.getByTitle("Page sent to Rohan's family")).toHaveAttribute('src', '/p/tok-rohan')
    // Signs in first, then asks who the child is.
    expect(calls.map((c) => c.key).slice(0, 3)).toEqual([
      'POST /api/demo/login',
      'GET /api/demo',
      'GET /api/demo/children/rohan-1',
    ])
    // Only the family phone: no teacher phone, no AI table.
    expect(screen.queryByTitle("Teacher's phone")).not.toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  it('says so for a child who is not in the demo class', async () => {
    setup()
    renderApp('/demo/family/nobody')

    expect(await screen.findByRole('alert')).toHaveTextContent('No child with this link in the demo class')
    expect(screen.getByRole('link', { name: '‹ Back to the demo' })).toHaveAttribute('href', '/demo')
  })

  it('says so when the demo sign-in fails', async () => {
    setup({ 'POST /api/demo/login': () => ({ status: 404 }) })
    renderApp('/demo/family/rohan-1')

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not sign in to the demo')
  })
})
