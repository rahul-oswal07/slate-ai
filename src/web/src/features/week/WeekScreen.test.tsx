import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { saveSnapshot } from '../../offline/db'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import { curriculum } from '../missed/fixtures'
import { istToday } from '../today/schoolTime'
import type { ReturnSpeed, Week } from './types'

const i18n = initI18n()
const today = istToday()
const weekUrl = `/api/classes/${teacher.classId}/week/${today}`

const speed = (o: Partial<ReturnSpeed>): ReturnSpeed => ({
  from: '2026-10-19',
  to: '2026-10-24',
  oneDay: 0,
  twoDays: 0,
  threeDays: 0,
  fourPlusDays: 0,
  backWithinTwoDays: 0,
  total: 0,
  ...o,
})

/** The card's example (T6 §7): this week 7, 3, 1, 1 → 10 of 12; last week 5, 4, 2, 2 → 9 of 13. */
function week(overrides: Partial<Week> = {}): Week {
  return {
    from: '2026-10-19',
    to: '2026-10-24',
    empty: false,
    thisWeek: speed({ oneDay: 7, twoDays: 3, threeDays: 1, fourPlusDays: 1, backWithinTwoDays: 10, total: 12 }),
    lastWeek: speed({ from: '2026-10-12', to: '2026-10-17', oneDay: 5, twoDays: 4, threeDays: 2, fourPlusDays: 2, backWithinTwoDays: 9, total: 13 }),
    reasons: { unwell: 3, travelOrWork: 1, festival: 2, neededAtHome: 0, other: 1, notGiven: 5 },
    nudge: {
      lessonId: 'lesson-1',
      date: '2026-10-20',
      competencyIds: ['M3.SUB.BORROW'],
      n: 6,
      total: 36,
      minutes: 10,
      nextCompetencyId: 'M3.ADD.CARRY',
    },
    decision: null,
    ...overrides,
  }
}

function setup(current: () => Week | 'offline', extra: Record<string, FakeRoute> = {}) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    'GET /api/curriculum': () => ({ status: 200, body: curriculum }),
    [`GET /api/classes/${teacher.classId}/packages`]: () => ({ status: 200, body: { weekOf: today, packages: [] } }),
    [`GET ${weekUrl}`]: () => {
      const value = current()
      return value === 'offline' ? 'offline' : { status: 200, body: value }
    },
    ...extra,
  })
}

describe('WeekScreen (T6)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('return speed: headline, sentence and the screen-reader table show the same numbers for both weeks', async () => {
    setup(() => week())
    renderApp('/week')

    expect(await screen.findByRole('heading', { level: 1, name: 'This week' })).toHaveFocus()
    expect(await screen.findByText('19–24 October')).toBeInTheDocument()
    const card = screen.getByRole('region', { name: 'How quickly children came back' })
    expect(within(card).getByText('10 of 12')).toBeInTheDocument()
    expect(card).toHaveTextContent('absent children were back within 2 school days. Last week: 9 of 13.')
    const table = within(card).getByRole('table', { name: 'Absent children by days away' })
    const rows = within(table).getAllByRole('row').slice(1).map((r) => r.textContent)
    expect(rows).toEqual(['1 day75', '2 days34', '3 days12', '4+ days12'])
  })

  it('reasons: a count per category and “Not given”, never a name', async () => {
    setup(() => week())
    renderApp('/week')

    const card = await screen.findByRole('region', { name: 'Reasons (from parents)' })
    expect(within(card).getAllByRole('listitem').map((li) => li.textContent)).toEqual([
      'Unwell3',
      'Family travel or work1',
      'Festival or function2',
      'Needed at home0',
      'Other1',
      'Not given5',
    ])
    expect(card).toHaveTextContent('Only for you. Not sent to anyone.')
  })

  it('nudge: the sentence uses the counted numbers and lesson names; Plan recap stores it, Change reopens', async () => {
    let current = week()
    const calls = setup(() => current, {
      [`PUT ${weekUrl}/pacing`]: ({ body }) => {
        current = { ...current, decision: (body as { decision: 'recap' | 'skip' }).decision }
        return { status: 200, body: current }
      },
    })
    renderApp('/week')

    const panel = await screen.findByRole('region', { name: 'Before next week' })
    await waitFor(() =>
      expect(panel).toHaveTextContent(
        '6 of 36 children missed Subtraction with borrowing. Recap 10 minutes before Addition with carrying.',
      ),
    )
    await userEvent.click(within(panel).getByRole('button', { name: 'Plan recap' }))

    expect(await within(panel).findByText('We’ll remind you on Today next week.')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByTestId('announcer')).toHaveTextContent('We’ll remind you on Today next week.'))
    await userEvent.click(within(panel).getByRole('button', { name: 'Change' }))
    await userEvent.click(within(panel).getByRole('button', { name: 'Not this week' }))

    expect(await within(panel).findByText('OK. Skipped this week.')).toBeInTheDocument()
    const puts = calls.filter((c) => c.key === `PUT ${weekUrl}/pacing`).map((c) => c.body)
    expect(puts).toMatchObject([
      { decision: 'recap', lessonId: 'lesson-1' },
      { decision: 'skip', lessonId: 'lesson-1' },
    ])
  })

  it('no nudge: most children are on track', async () => {
    setup(() => week({ nudge: null }))
    renderApp('/week')

    expect(await screen.findByText('Most children are on track. No recap needed this week.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Plan recap' })).toBeNull()
  })

  it('no absences: says so, with no comparison', async () => {
    setup(() => week({ empty: true, nudge: null, thisWeek: speed({}) }))
    renderApp('/week')

    expect(await screen.findByText('No absences this week.')).toBeInTheDocument()
    expect(screen.queryByText(/Last week/)).toBeNull()
    expect(screen.queryByText(/on track/)).toBeNull()
    expect(screen.getByText('Numbers are counted from attendance.')).toBeInTheDocument()
  })

  it('offline: the saved week with its time', async () => {
    await saveSnapshot(`week:${teacher.classId}:${today}`, week())
    setup(() => 'offline')
    renderApp('/week')

    expect(await screen.findByText(/No network\. Showing the saved week \(today .+\)\./)).toBeInTheDocument()
    expect(screen.getByText('10 of 12')).toBeInTheDocument()
  })
})
