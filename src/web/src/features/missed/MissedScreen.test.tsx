import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { saveSnapshot } from '../../offline/db'
import { fakeApi, renderApp, teacher } from '../../test/renderApp'
import { curriculum, ledger } from './fixtures'
import type { Ledger } from './types'

const i18n = initI18n()
/** The i-th element, failing the test if it isn't there. */
function at<T>(items: T[], i: number): T {
  const item = items[i]
  if (item === undefined) {
    throw new Error(`No item ${i}`)
  }
  return item
}

const ledgerUrl = `GET /api/classes/${teacher.classId}/ledger`

function setup(current: () => Ledger | 'offline') {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    [`GET /api/classes/${teacher.classId}/curriculum`]: () => ({ status: 200, body: curriculum }),
    [`GET /api/classes/${teacher.classId}/packages`]: () => ({ status: 200, body: { weekOf: '2026-10-19', packages: [] } }),
    [ledgerUrl]: () => {
      const value = current()
      return value === 'offline' ? 'offline' : { status: 200, body: value }
    },
  })
}

describe('MissedScreen (T4)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('opens on “Absent this week”: a table of name · days away · lessons missed · level, no lesson list (T-98)', async () => {
    setup(() => ledger())
    renderApp('/missed')

    expect(await screen.findByRole('heading', { level: 1, name: 'Missed lessons' })).toHaveFocus()
    expect(await screen.findByRole('button', { name: 'Absent this week · 2' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: 'Everyone · 4' })).toHaveAttribute('aria-pressed', 'false')
    expect(screen.getByRole('button', { name: 'By child' })).toHaveAttribute('aria-pressed', 'true')
    // Back today has its own tab again, so it is not a filter here (D-117, T-129).
    expect(within(screen.getByRole('group', { name: 'Filter' })).getAllByRole('button')).toHaveLength(2)
    expect(screen.queryByRole('button', { name: /Back today/ })).not.toBeInTheDocument()

    const table = screen.getByRole('table', { name: 'Children this week: days away, lessons missed and level' })
    expect(within(table).getAllByRole('columnheader').map((h) => h.textContent)).toEqual([
      'Child',
      'Days away',
      'Lessons missed',
      'Level',
    ])
    const rows = within(table).getAllByRole('row').slice(1)
    expect(rows.map((r) => within(r).getAllByRole('cell').map((c) => c.textContent))).toEqual([
      // Asha's last-week lesson is not "this week"; her catch-up hasn't gone, so no level yet.
      ['1', '1', '—No level yet'],
      // Ravi got Monday's lesson but needs help with Tuesday's: the one that needs her shows (D-130).
      ['2', '2', 'Needs help'],
    ])
    const [asha, ravi] = [at(rows, 0), at(rows, 1)]
    expect(within(ravi).getByRole('link', { name: 'Ravi' })).toHaveAttribute('href', '/missed/ravi')
    // The channel is its own line under the name, so a stop never hides the level (B-08).
    expect(within(ravi).getByRole('rowheader')).toHaveTextContent('Stopped by family')
    expect(within(asha).getByRole('rowheader')).toHaveTextContent('No phone · buddy card')
    // The level is an icon and a word, in the level's own colour (T-97).
    expect(within(ravi).getByText('Needs help')).toHaveAttribute('data-level', 'needsHelp')
    expect(within(ravi).getByText('Needs help').querySelector('svg')).not.toBeNull()
    // No lesson names in the rows: they are on the child view.
    await screen.findByRole('link', { name: 'Asha' })
    expect(table).not.toHaveTextContent(/Subtraction|Addition/)
    expect(screen.getByText('Filled automatically from attendance × your lesson records. No AI in this.')).toBeInTheDocument()
  })

  it('Everyone lists the whole class; children with nothing missed show 0 and no level', async () => {
    setup(() => ledger())
    renderApp('/missed')

    await userEvent.click(await screen.findByRole('button', { name: 'Everyone · 4' }))

    const rows = within(screen.getByRole('table')).getAllByRole('row').slice(1)
    expect(rows.map((r) => within(r).getByRole('link').textContent)).toEqual(['Asha', 'Chetan', 'Ravi', 'Kavya'])
    const kavya = at(rows, 3)
    expect(within(kavya).getAllByRole('cell').map((c) => c.textContent)).toEqual(['0', '0', '—No level yet'])
    expect(kavya).toHaveTextContent('No consent')
  })

  it('By lesson: newest first, counts per child × lesson, and the children behind an expander', async () => {
    setup(() => ledger())
    renderApp('/missed')

    await userEvent.click(await screen.findByRole('button', { name: 'By lesson' }))
    await screen.findByRole('heading', { name: 'Subtraction with borrowing' })

    const cards = screen.getAllByRole('listitem').filter((li) => li.querySelector('h2'))
    expect(cards.map((c) => within(c).getByRole('heading').textContent)).toEqual([
      'Addition with carrying',
      'Subtraction with borrowing',
    ])
    const subtraction = at(cards, 1)
    expect(subtraction).toHaveTextContent('Part 2, page 15')
    expect(subtraction).toHaveTextContent('2 of 4 children missed it')
    // Ravi caught up on Monday's lesson even though his latest status is "needs buddy".
    expect(subtraction).toHaveTextContent('1 Missed lesson')
    expect(subtraction).toHaveTextContent('1 Caught up')
    expect(subtraction).not.toHaveTextContent('Needs buddy')
    expect(cards[0]).toHaveTextContent('1 of 4 children missed it')

    const expander = within(subtraction).getByRole('button', { name: 'Show the children' })
    expect(expander).toHaveAttribute('aria-expanded', 'false')
    await userEvent.click(expander)
    expect(expander).toHaveAttribute('aria-expanded', 'true')
    expect(within(subtraction).getByText('Ravi')).toBeInTheDocument()
    expect(within(subtraction).getByText('Asha')).toBeInTheDocument()
  })

  it('before any lesson is recorded: the empty state sends her to Today', async () => {
    setup(() => ledger({ lessons: [], children: ledger().children.map((c) => ({ ...c, missedLessons: [] })) }))
    renderApp('/missed')

    expect(await screen.findByRole('heading', { name: 'No missed lessons yet' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Go to Today' })).toHaveAttribute('href', '/')
  })

  it('offline: shows the saved list with its time, never a blank screen', async () => {
    await saveSnapshot(`ledger:${teacher.classId}`, ledger())
    setup(() => 'offline')
    renderApp('/missed')

    expect(await screen.findByText(/No network\. Showing the saved list \(today .+\)\./)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Absent this week · 2' })).toBeInTheDocument()
  })

  it('the old /missed?filter=back link opens the Back today tab (D-117, T-129)', async () => {
    setup(() => ledger())
    renderApp('/missed?filter=back')

    expect(await screen.findByRole('heading', { level: 1, name: 'Back today' })).toHaveFocus()
    const nav = screen.getByRole('navigation', { name: 'Main' })
    expect(within(nav).getByRole('link', { name: /^Back today/ })).toHaveAttribute('aria-current', 'page')
  })
})
