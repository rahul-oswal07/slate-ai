import { screen, within } from '@testing-library/react'
import type { BuddyCard } from '../features/back/types'
import { istToday } from '../features/today/schoolTime'
import { initI18n } from '../i18n'
import { saveSnapshot } from '../offline/db'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../test/renderApp'

const i18n = initI18n()
const today = istToday()
const cardsKey = `GET /api/classes/${teacher.classId}/buddy-cards/${today}`

function card(childId: string, firstName: string): BuddyCard {
  return {
    childId,
    firstName,
    rollNo: 4,
    daysAway: 2,
    buddy: { childId: 'asha', firstName: 'Asha', presentDays: 2 },
    missedDays: 2,
    competencyId: 'M3.SUB.BORROW',
    practiceKind: 'sub',
    sums: [{ question: '52 − 27', answer: 25 }],
    keyWordsKn: ['ಕಳೆ'],
    homework: { book: 'textbook', part: 2, page: 15 },
    homeworkReturned: false,
    done: false,
  }
}

function setup(cards: BuddyCard[] | 'offline', extra: Record<string, FakeRoute> = {}) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    [`GET /api/classes/${teacher.classId}/packages`]: () => ({ status: 200, body: { weekOf: today, packages: [] } }),
    [cardsKey]: () => (cards === 'offline' ? 'offline' : { status: 200, body: { date: today, cards } }),
    ...extra,
  })
}

const nav = async () => screen.findByRole('navigation', { name: i18n.t('nav.label') })

/** Five tabs again, with a count on Back today (D-117, T-129). */
describe('BottomNav (D-117)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('has five tabs in order, each to its own screen', async () => {
    setup([])
    renderApp('/')

    const links = within(await nav()).getAllByRole('link')
    expect(links.map((link) => link.textContent)).toEqual(['Today', 'Approve', 'Missed', 'Back today', 'Week'])
    expect(links.map((link) => link.getAttribute('href'))).toEqual(['/', '/approve', '/missed', '/back', '/week'])
  })

  it.each([
    ['/back', 'Back today'],
    ['/missed', 'Missed'],
    ['/lesson', 'Today'],
    ['/settings', 'Today'],
    ['/profile', 'Today'],
  ])('on %s, %s is the current tab', async (path, current) => {
    setup([])
    renderApp(path)

    const links = within(await nav()).getAllByRole('link')
    const marked = links.filter((link) => link.getAttribute('aria-current') === 'page')
    expect(marked.map((link) => link.textContent)).toEqual([current])
  })

  it('no badge when nobody is back today', async () => {
    const calls = setup([])
    renderApp('/')

    const back = within(await nav()).getByRole('link', { name: 'Back today' })
    await vi.waitFor(() => expect(calls.some((c) => c.key === cardsKey)).toBe(true))
    expect(back).toHaveAccessibleName('Back today')
    expect(within(back).queryByText(/\d/)).not.toBeInTheDocument()
  })

  it('shows how many are back today, in the neutral style, and screen readers hear it in the tab name', async () => {
    setup([card('ravi', 'Ravi'), card('manju', 'Manju')])
    renderApp('/')

    const back = await within(await nav()).findByRole('link', { name: 'Back today, 2 children' })
    const badge = within(back).getByText('2')
    expect(badge).toHaveAttribute('aria-hidden', 'true')
    expect(badge).toHaveClass('bg-ink', 'text-surface')
    expect(badge.className).not.toMatch(/error/)
  })

  it('says "1 child" for one', async () => {
    setup([card('ravi', 'Ravi')])
    renderApp('/')

    expect(await within(await nav()).findByRole('link', { name: 'Back today, 1 child' })).toBeInTheDocument()
  })

  it('offline, counts the saved buddy cards', async () => {
    await saveSnapshot(`buddy-cards:${teacher.classId}:${today}`, { date: today, cards: [card('ravi', 'Ravi')] })
    setup('offline')
    renderApp('/')

    expect(await within(await nav()).findByRole('link', { name: 'Back today, 1 child' })).toBeInTheDocument()
  })

  it.each([
    ['kn', 'ತಪ್ಪಿದ ಪಾಠ', 'ಮರಳಿ ಬಂದವರು'],
    ['te', 'తప్పిన పాఠం', 'తిరిగి వచ్చినవారు'],
    ['ta', 'தவறிய பாடம்', 'திரும்பி வந்தோர்'],
    ['hi', 'छूटे', 'आज लौटे'],
  ])('%s: the labels used before D-96', async (language, missed, back) => {
    await i18n.changeLanguage(language)
    setup([])
    renderApp('/')

    const links = within(await nav()).getAllByRole('link')
    expect(links.map((link) => link.textContent).slice(2, 4)).toEqual([missed, back])
  })
})
