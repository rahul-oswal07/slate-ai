import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import { curriculum, ledger } from './fixtures'
import type { Ledger } from './types'

const i18n = initI18n()

/** The fake API's fetch, so a test can wrap it for the DELETE whose URL carries a fresh clientRequestId. */
function routedFetch(): typeof fetch {
  const routed = (globalThis.fetch as ReturnType<typeof vi.fn>).getMockImplementation()
  if (!routed) {
    throw new Error('fakeApi was not set up')
  }
  return routed as typeof fetch
}

function setup(current: () => Ledger, extra: Record<string, FakeRoute> = {}) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    'GET /api/curriculum': () => ({ status: 200, body: curriculum }),
    [`GET /api/classes/${teacher.classId}/packages`]: () => ({ status: 200, body: { weekOf: '2026-10-19', packages: [] } }),
    [`GET /api/classes/${teacher.classId}/ledger`]: () => ({ status: 200, body: current() }),
    ...extra,
  })
}

describe('ChildScreen (T4 child history + delete)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('shows this week as a timeline with the day on every event; the reason is labelled “from the parent”', async () => {
    setup(() => ledger())
    renderApp('/missed/ravi')

    expect(await screen.findByRole('heading', { level: 1, name: 'Ravi' })).toHaveFocus()
    expect(screen.getByText('Roll 4 · home language: Telugu')).toBeInTheDocument()
    // Lesson names come from the curriculum, which may arrive after the ledger.
    await screen.findByText(/Subtraction with borrowing/)
    const timeline = screen.getByRole('list', { name: 'This week' })
    expect(within(timeline).getAllByRole('listitem').map((li) => li.textContent)).toEqual([
      'Mon · away · missed: Subtraction with borrowing',
      'Mon · reason (from the parent): Unwell',
      'Tue · away · missed: Addition with carrying',
      'Wed · catch-up sent (Telugu)',
      'Thu · picture question: right',
      'Thu · family stopped messages. Buddy card continues.',
    ])
    expect(screen.getByRole('link', { name: 'Back' })).toHaveAttribute('href', '/missed')
  })

  it('delete asks first with Keep focused; Keep sends nothing', async () => {
    const calls = setup(() => ledger())
    renderApp('/missed/ravi')

    await userEvent.click(await screen.findByRole('button', { name: 'Delete all of Ravi’s data' }))
    const dialog = screen.getByRole('dialog', { name: 'Delete Ravi’s data?' })
    expect(within(dialog).getByRole('button', { name: 'Keep' })).toHaveFocus()
    expect(dialog).toHaveTextContent('The name, answers and audio are removed for good.')
    expect(dialog).toHaveTextContent('This can’t be undone.')

    await userEvent.click(within(dialog).getByRole('button', { name: 'Keep' }))
    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull())
    expect(calls.some((c) => c.key.startsWith('DELETE'))).toBe(false)
  })

  it('Delete removes the child: back on the list, announced, gone from the counts', async () => {
    let current = ledger()
    const calls = fakeApi({
      'GET /api/me': () => ({ status: 200, body: teacher }),
      'GET /api/curriculum': () => ({ status: 200, body: curriculum }),
      [`GET /api/classes/${teacher.classId}/packages`]: () => ({ status: 200, body: { weekOf: '2026-10-19', packages: [] } }),
      [`GET /api/classes/${teacher.classId}/ledger`]: () => ({ status: 200, body: current }),
    })
    // The delete URL carries a fresh clientRequestId, so match it by prefix.
    const fetch = globalThis.fetch as ReturnType<typeof vi.fn>
    const routed = routedFetch()
    fetch.mockImplementation(async (input: RequestInfo | URL, init?: RequestInit) => {
      if (init?.method === 'DELETE' && String(input).startsWith(`/api/classes/${teacher.classId}/children/ravi?clientRequestId=`)) {
        calls.push({ key: `DELETE ${String(input)}`, body: undefined })
        current = { ...current, children: current.children.filter((c) => c.childId !== 'ravi') }
        return new Response(null, { status: 204 })
      }
      return routed(input, init)
    })
    renderApp('/missed/ravi')

    await userEvent.click(await screen.findByRole('button', { name: 'Delete all of Ravi’s data' }))
    await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Delete' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Missed lessons' })).toBeInTheDocument()
    expect(await screen.findByRole('button', { name: 'Absent this week · 1' })).toBeInTheDocument()
    expect(screen.queryByText(/Ravi/)).toBeNull()
    await waitFor(() => expect(screen.getByTestId('announcer')).toHaveTextContent('Deleted: name, answers and audio'))
    expect(calls.filter((c) => c.key.startsWith('DELETE'))).toHaveLength(1)
  })

  it('a failed delete says nothing changed and keeps the dialog open', async () => {
    setup(() => ledger())
    const fetch = globalThis.fetch as ReturnType<typeof vi.fn>
    const routed = routedFetch()
    fetch.mockImplementation(async (input: RequestInfo | URL, init?: RequestInit) =>
      init?.method === 'DELETE' ? Promise.reject(new TypeError('Failed to fetch')) : routed(input, init),
    )
    renderApp('/missed/ravi')

    await userEvent.click(await screen.findByRole('button', { name: 'Delete all of Ravi’s data' }))
    await userEvent.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Delete' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not delete. Nothing changed.')
    expect(screen.getByRole('dialog')).toBeInTheDocument()
  })
})
