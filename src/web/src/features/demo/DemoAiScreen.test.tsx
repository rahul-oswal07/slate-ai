import { screen, within } from '@testing-library/react'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher } from '../../test/renderApp'
import type { AiCall, DemoInfo } from './types'

const i18n = initI18n()

const lastResetAt = '2026-10-23T04:30:00Z'
const info: DemoInfo = {
  classId: teacher.classId,
  parentChildId: 'manjunath-1',
  lastResetAt,
  parentChildName: 'Manjunath',
  parentLanguage: 'kn',
}

const call: AiCall = {
  id: 'call-1',
  createdAt: '2026-10-23T05:00:00Z',
  purpose: 'a2-compression',
  model: 'claude-opus-5-5',
  promptVersion: 1,
  inputTokens: 500,
  outputTokens: 120,
  cacheReadTokens: 0,
  latencyMs: 2100,
  verdict: 'valid',
  error: null,
  children: ['Manjunath'],
  otherChildren: 0,
}

beforeEach(async () => {
  localStorage.clear()
  await i18n.changeLanguage('en')
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('DemoAiScreen (T-126)', () => {
  it('signs in as the demo teacher and lists the AI calls since the last reset, on its own', async () => {
    const calls = fakeApi({
      'POST /api/demo/login': () => ({ status: 200, body: teacher }),
      'GET /api/demo': () => ({ status: 200, body: info }),
      [`GET /api/demo/ai-calls?since=${encodeURIComponent(lastResetAt)}`]: () => ({ status: 200, body: [call] }),
    })
    renderApp('/demo/ai')

    const table = await screen.findByRole('table', { name: 'Under the hood' })
    expect(within(table).getByText('A2 Compression')).toBeInTheDocument()
    expect(within(table).getByText('Manjunath')).toBeInTheDocument()
    expect(calls.map((c) => c.key).slice(0, 2)).toEqual(['POST /api/demo/login', 'GET /api/demo'])
    // Only the AI panel: no phones, no step guide.
    expect(screen.queryByTitle("Teacher's phone")).not.toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Try the loop in 3 minutes' })).not.toBeInTheDocument()
    expect(screen.getByRole('link', { name: '‹ Back to the demo' })).toHaveAttribute('href', '/demo')
  })

  it('says so when the demo sign-in fails', async () => {
    fakeApi({ 'POST /api/demo/login': () => ({ status: 404 }) })
    renderApp('/demo/ai')

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not sign in to the demo')
  })
})
