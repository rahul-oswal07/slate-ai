import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import { istToday } from '../today/schoolTime'
import type { AiCall, DemoInfo, FamilyMessage } from './types'

const i18n = initI18n()

const info: DemoInfo = { classId: teacher.classId, parentChildId: 'rohan-1', lastResetAt: null }
const resetAt = '2026-10-23T04:30:00Z'

const evening: FamilyMessage = { kind: 'evening', date: '2026-10-22', sentAt: '2026-10-22T12:30:00Z', token: 'tok-evening' }

const call: AiCall = {
  id: 'call-1',
  createdAt: '2026-10-23T05:00:00Z',
  purpose: 'a3-script',
  model: 'claude-opus-5-5',
  promptVersion: 2,
  inputTokens: 900,
  outputTokens: 400,
  cacheReadTokens: 0,
  latencyMs: 5200,
  verdict: 'ready',
  error: null,
}

function setup({
  login = () => ({ status: 200, body: teacher }),
  demo = info,
  family = [],
  calls = [],
  extra = {},
}: {
  login?: FakeRoute
  demo?: DemoInfo
  family?: FamilyMessage[]
  calls?: AiCall[]
  extra?: Record<string, FakeRoute>
} = {}) {
  return fakeApi({
    'POST /api/demo/login': login,
    'GET /api/demo': () => ({ status: 200, body: demo }),
    'GET /api/demo/family/rohan-1': () => ({ status: 200, body: family }),
    'GET /api/demo/ai-calls': () => ({ status: 200, body: calls }),
    [`GET /api/demo/ai-calls?since=${encodeURIComponent(resetAt)}`]: () => ({ status: 200, body: [] }),
    'POST /api/demo/reset': () => ({
      status: 200,
      body: { resetAt, preMade: true },
    }),
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

describe('DemoScreen (T-18)', () => {
  it('signs in as the demo teacher with one tap and shows both phones in frames', async () => {
    const calls = setup()
    renderApp('/demo')

    expect(await screen.findByRole('heading', { name: 'Try the loop in 5 minutes' })).toBeInTheDocument()
    expect(calls.map((c) => c.key).slice(0, 2)).toEqual(['POST /api/demo/login', 'GET /api/demo'])
    expect(screen.getByTitle("Teacher's phone")).toHaveAttribute('src', '/')
    expect(await screen.findByText(/No messages yet/)).toBeInTheDocument()
  })

  it('says so when the demo sign-in fails', async () => {
    setup({ login: () => ({ status: 404 }) })
    renderApp('/demo')

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not sign in to the demo')
    expect(screen.getByRole('button', { name: 'Try again' })).toBeInTheDocument()
  })

  it("opens a message on Rohan's family's phone as the family's page", async () => {
    const user = userEvent.setup()
    setup({ family: [evening] })
    renderApp('/demo')

    await user.click(await screen.findByRole('button', { name: /Evening message · 22 Oct/ }))

    expect(screen.getByTitle("Page sent to Rohan's family")).toHaveAttribute('src', '/p/tok-evening')
    await user.click(screen.getByRole('button', { name: '‹ Messages' }))
    expect(screen.getByRole('button', { name: /Evening message/ })).toBeInTheDocument()
  })

  it('lists AI calls with step, model, prompt version and verdict', async () => {
    setup({ calls: [call] })
    renderApp('/demo')

    const table = await screen.findByRole('table', { name: 'Under the hood' })
    const row = within(table).getAllByRole('row')[1]
    expect(row).toHaveTextContent('A3 Script writer')
    expect(row).toHaveTextContent('a3-script v2')
    expect(row).toHaveTextContent('claude-opus-5-5')
    expect(row).toHaveTextContent('900 in / 400 out')
    expect(row).toHaveTextContent('5.2 s')
    expect(row).toHaveTextContent('ready')
  })

  it('resets after a confirmation, then lists AI calls from the reset', async () => {
    const user = userEvent.setup()
    const calls = setup({ calls: [call] })
    renderApp('/demo')

    await user.click(await screen.findByRole('button', { name: 'Reset demo' }))
    const dialog = screen.getByRole('dialog', { name: 'Reset the demo?' })
    expect(within(dialog).getByRole('button', { name: 'Cancel' })).toHaveFocus()
    await user.click(within(dialog).getByRole('button', { name: 'Reset' }))

    expect(await screen.findByText('Demo reset. A pre-made catch-up is ready to approve.')).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    const reset = calls.find((c) => c.key === 'POST /api/demo/reset')
    expect(reset?.body).toEqual({ clientRequestId: expect.any(String) })
    await waitFor(() =>
      expect(calls.map((c) => c.key)).toContain(`GET /api/demo/ai-calls?since=${encodeURIComponent(resetAt)}`),
    )
    expect(await screen.findByText(/No AI calls since the reset yet/)).toBeInTheDocument()
    expect(localStorage.getItem('slate.demo.resetAt')).toBe(resetAt)
  })

  it('lists AI calls from the last reset anyone made, in a keyboard-reachable region', async () => {
    const calls = setup({ demo: { ...info, lastResetAt: resetAt } })
    renderApp('/demo')

    expect(await screen.findByText(/No AI calls since the reset yet/)).toBeInTheDocument()
    expect(calls.map((c) => c.key)).toContain(`GET /api/demo/ai-calls?since=${encodeURIComponent(resetAt)}`)
    expect(calls.map((c) => c.key)).not.toContain('GET /api/demo/ai-calls')
  })

  it('makes the scrolling AI table focusable and named', async () => {
    setup({ calls: [call] })
    renderApp('/demo')

    const region = await screen.findByRole('region', { name: 'Under the hood' })
    expect(region).toHaveAttribute('tabindex', '0')
  })

  it("brings in today's attendance from the simulated feed", async () => {
    const user = userEvent.setup()
    const calls = setup({ extra: { 'POST /api/demo/sats-feed': () => ({ status: 200, body: {} }) } })
    renderApp('/demo')

    await user.click(await screen.findByRole('button', { name: "Bring in today's attendance" }))

    expect(await screen.findByText("Today's attendance is in.")).toBeInTheDocument()
    expect(calls.find((c) => c.key === 'POST /api/demo/sats-feed')?.body).toEqual({
      classId: teacher.classId,
      date: istToday(),
      mode: 'arrive',
    })
  })

  it('shows one panel at a time through tabs on a phone', async () => {
    const user = userEvent.setup()
    setup()
    renderApp('/demo')

    const aiTab = await screen.findByRole('tab', { name: 'Under the hood' })
    expect(screen.getByRole('tab', { name: 'Teacher' })).toHaveAttribute('aria-selected', 'true')
    expect(document.getElementById('panel-ai')).toHaveClass('hidden')

    await user.click(aiTab)

    expect(aiTab).toHaveAttribute('aria-selected', 'true')
    expect(document.getElementById('panel-ai')).toHaveClass('block')
    expect(document.getElementById('panel-teacher')).toHaveClass('hidden')
  })
})
