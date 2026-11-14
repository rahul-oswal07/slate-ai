import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { fakeApi, renderApp, teacher } from '../../test/renderApp'
import { curriculum, ledger } from '../missed/fixtures'
import { AnswersCard } from './AnswersCard'
import { istToday } from './schoolTime'
import type { TodaysAnswers } from './useTodaysAnswers'

const i18n = initI18n()
const date = istToday()
const url = `GET /api/classes/${teacher.classId}/todays-message/${date}/answers`

const answers = (overrides: Partial<TodaysAnswers> = {}): TodaysAnswers => ({
  sent: 4,
  answered: 3,
  rows: [
    { childId: 'asha', firstName: 'Asha', rollNo: 1, answer: '>', level: 'gotIt' },
    { childId: 'ravi', firstName: 'Ravi', rollNo: 4, answer: '>', level: 'gettingThere' },
    { childId: 'chetan', firstName: 'Chetan', rollNo: 3, answer: '=', level: 'needsHelp' },
    { childId: 'manju', firstName: 'Manjunath', rollNo: 15, answer: null, level: 'notAnswered' },
  ],
  ...overrides,
})

function renderCard() {
  return render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter>
        <AnswersCard classId={teacher.classId} date={date} />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('Answers back on Today (T-92)', () => {
  beforeEach(async () => {
    await i18n.changeLanguage('en')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.useRealTimers()
  })

  it('shows how many got it and answered, and each child with answer and level (icon and word)', async () => {
    fakeApi({ [url]: () => ({ status: 200, body: answers() }) })
    renderCard()

    expect(await screen.findByRole('heading', { name: "Today's message: sent to 4 · 3 answered" })).toBeInTheDocument()
    const table = screen.getByRole('table', { name: /Each child's answer/ })
    const row = (name: string) => within(table).getByRole('row', { name: new RegExp(`^${name}`) })
    expect(row('Asha')).toHaveTextContent('Got it')
    expect(row('Ravi')).toHaveTextContent('Getting there')
    expect(row('Chetan')).toHaveTextContent('=')
    expect(row('Chetan')).toHaveTextContent('Needs help')
    expect(row('Manjunath')).toHaveTextContent('Not answered yet')
    expect(within(row('Manjunath')).getByLabelText('No answer yet')).toBeInTheDocument()
    expect(row('Asha').querySelector('svg')).not.toBeNull() // never colour alone
  })

  it('stays hidden before anything was sent, or without a today’s message', async () => {
    const calls = fakeApi({ [url]: () => ({ status: 204 }) })
    const { container } = renderCard()

    await waitFor(() => expect(calls.length).toBeGreaterThan(0))
    expect(container).toBeEmptyDOMElement()
  })

  it('checks again by itself until everyone has answered, then stops', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    let current = answers({ sent: 1, answered: 0, rows: [{ childId: 'manju', firstName: 'Manjunath', rollNo: 15, answer: null, level: 'notAnswered' }] })
    const calls = fakeApi({ [url]: () => ({ status: 200, body: current }) })
    renderCard()
    expect(await screen.findByText('Not answered yet')).toBeInTheDocument()

    current = answers({ sent: 1, answered: 1, rows: [{ childId: 'manju', firstName: 'Manjunath', rollNo: 15, answer: '>', level: 'gotIt' }] })
    await vi.advanceTimersByTimeAsync(5100)

    expect(await screen.findByText('Got it')).toBeInTheDocument()
    const after = calls.length
    await vi.advanceTimersByTimeAsync(15000)
    expect(calls.length).toBe(after)
  })

  it('Missed lessons shows the same level next to the child', async () => {
    fakeApi({
      'GET /api/me': () => ({ status: 200, body: teacher }),
      'GET /api/curriculum': () => ({ status: 200, body: curriculum }),
      [`GET /api/classes/${teacher.classId}/packages`]: () => ({ status: 200, body: { weekOf: '2026-10-19', packages: [] } }),
      [`GET /api/classes/${teacher.classId}/ledger`]: () => ({ status: 200, body: ledger() }),
      [url]: () => ({ status: 200, body: answers() }),
    })
    renderApp('/missed')

    const ravi = await screen.findByRole('link', { name: /Ravi/ })
    await waitFor(() => expect(ravi).toHaveTextContent('Getting there'))
  })
})
