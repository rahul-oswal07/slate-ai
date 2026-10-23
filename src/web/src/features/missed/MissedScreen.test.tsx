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
    'GET /api/curriculum': () => ({ status: 200, body: curriculum }),
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

  it('opens on “Absent this week”: rows with days away, lessons missed this week and two separate tags', async () => {
    setup(() => ledger())
    renderApp('/missed')

    expect(await screen.findByRole('heading', { level: 1, name: 'Missed lessons' })).toHaveFocus()
    expect(await screen.findByRole('button', { name: 'Absent this week · 2' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: 'Everyone · 4' })).toHaveAttribute('aria-pressed', 'false')
    expect(screen.getByRole('button', { name: 'By child' })).toHaveAttribute('aria-pressed', 'true')
    // Lesson names come from the curriculum, which may arrive after the ledger.
    await screen.findAllByText(/Subtraction with borrowing/)

    const rows = screen.getAllByRole('link', { name: /Ravi|Asha|Chetan|Kavya/ })
    expect(rows.map((r) => within(r).getByText(/^(Ravi|Asha|Chetan|Kavya)/).textContent)).toEqual(['Asha · 1 day away', 'Ravi · 2 days away'])
    const ravi = at(rows, 1)
    expect(ravi).toHaveTextContent('Missed: Subtraction with borrowing, Addition with carrying')
    // Learning status (the latest missed lesson) and the channel are two tags: a stop never hides it (B-08).
    expect(ravi).toHaveTextContent('Needs buddy')
    expect(ravi).toHaveTextContent('Stopped by family')
    expect(ravi).toHaveAttribute('href', '/missed/ravi')
    // Asha's last-week lesson is not "this week".
    expect(rows[0]).toHaveTextContent('Missed: Subtraction with borrowing')
    expect(rows[0]).toHaveTextContent('Missed lesson')
    expect(rows[0]).toHaveTextContent('No phone · buddy card')
    expect(screen.getByText('Filled automatically from attendance × your lesson records. No AI in this.')).toBeInTheDocument()
  })

  it('Everyone lists the whole class; children with nothing missed say so', async () => {
    setup(() => ledger())
    renderApp('/missed')

    await userEvent.click(await screen.findByRole('button', { name: 'Everyone · 4' }))

    const rows = screen.getAllByRole('link', { name: /Ravi|Asha|Chetan|Kavya/ })
    expect(rows).toHaveLength(4)
    const kavya = at(rows.filter((r) => r.textContent?.includes('Kavya')), 0)
    expect(kavya).toHaveTextContent('No missed lessons this week')
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
})
