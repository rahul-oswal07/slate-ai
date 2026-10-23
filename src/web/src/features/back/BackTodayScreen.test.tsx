import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { saveSnapshot } from '../../offline/db'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import { istToday } from '../today/schoolTime'
import type { BuddyCard, BuddyCards } from './types'

const i18n = initI18n()
const today = istToday()
const cardsUrl = `/api/classes/${teacher.classId}/buddy-cards/${today}`

function card(overrides: Partial<BuddyCard> & Pick<BuddyCard, 'childId' | 'firstName'>): BuddyCard {
  return {
    rollNo: 4,
    daysAway: 2,
    buddy: { childId: 'asha', firstName: 'Asha', presentDays: 2 },
    missedDays: 2,
    competencyId: 'M3.SUB.BORROW',
    practiceKind: 'sub',
    sums: [
      { question: '52 − 27', answer: 25 },
      { question: '81 − 46', answer: 35 },
      { question: '70 − 18', answer: 52 },
    ],
    keyWordsKn: ['ಕಳೆ', 'ದಶಕ', 'ಹತ್ತು'],
    homework: { book: 'textbook', part: 2, page: 15 },
    homeworkReturned: false,
    done: false,
    ...overrides,
  }
}

const ravi = card({ childId: 'ravi', firstName: 'Ravi' })
const manju = card({
  childId: 'manju',
  firstName: 'Manju',
  rollNo: 9,
  daysAway: 3,
  missedDays: 3,
  buddy: { childId: 'kiran', firstName: 'Kiran', presentDays: 2 },
})

function setup(current: () => BuddyCards | 'offline', extra: Record<string, FakeRoute> = {}) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    [`GET /api/classes/${teacher.classId}/packages`]: () => ({ status: 200, body: { weekOf: today, packages: [] } }),
    [`GET ${cardsUrl}`]: () => {
      const value = current()
      return value === 'offline' ? 'offline' : { status: 200, body: value }
    },
    ...extra,
  })
}

describe('BackTodayScreen (T5)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await i18n.changeLanguage('en')
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('shows one card at a time: position as text, buddy, sums without answers, words and the homework page', async () => {
    setup(() => ({ date: today, cards: [ravi, manju] }))
    renderApp('/back')

    expect(await screen.findByRole('heading', { level: 1, name: 'Back today' })).toHaveFocus()
    expect(await screen.findByText('Card 1 of 2 · next: Manju')).toBeInTheDocument()
    const article = screen.getByRole('article', { name: 'Ravi' })
    expect(article).toHaveTextContent('Back after 2 days')
    expect(article).toHaveTextContent('Buddy (was present both days)')
    expect(article).toHaveTextContent('Asha · 10 min')
    expect(within(article).getByText('52 − 27 = ?')).toBeVisible()
    // Answers are for the teacher, folded away so the card can be shown to the children.
    expect(within(article).getByText('52 − 27 → 25')).not.toBeVisible()
    expect(within(article).getByText('ಕಳೆ')).toHaveAttribute('lang', 'kn')
    expect(within(article).getByRole('checkbox', { name: 'Brought homework' })).toHaveAccessibleDescription('Part 2, page 15')
    expect(screen.getByRole('button', { name: 'Previous card' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Print or share card' })).toBeEnabled()
  })

  it('Next and Previous move between cards with focus on the child’s name; the ends are disabled', async () => {
    setup(() => ({ date: today, cards: [ravi, manju] }))
    renderApp('/back')

    await userEvent.click(await screen.findByRole('button', { name: 'Next card' }))

    expect(screen.getByRole('heading', { level: 2, name: 'Manju' })).toHaveFocus()
    expect(screen.getByText('Card 2 of 2')).toBeInTheDocument()
    expect(screen.getByRole('article', { name: 'Manju' })).toHaveTextContent('Buddy (was present 2 of 3 days)')
    expect(screen.getByRole('button', { name: 'Next card' })).toBeDisabled()

    await userEvent.click(screen.getByRole('button', { name: 'Previous card' }))
    expect(screen.getByRole('heading', { level: 2, name: 'Ravi' })).toHaveFocus()
  })

  it('Brought homework and buddy time done are saved; Undo takes done back', async () => {
    let current = ravi
    const calls = setup(() => ({ date: today, cards: [current] }), {
      [`PUT ${cardsUrl}/ravi`]: ({ body }) => {
        const ticks = body as { homeworkReturned?: boolean; done?: boolean }
        current = {
          ...current,
          homeworkReturned: ticks.homeworkReturned ?? current.homeworkReturned,
          done: ticks.done ?? current.done,
        }
        return { status: 200, body: current }
      },
    })
    renderApp('/back')

    await userEvent.click(await screen.findByRole('checkbox', { name: 'Brought homework' }))
    await waitFor(() => expect(screen.getByRole('checkbox', { name: 'Brought homework' })).toBeChecked())
    await userEvent.click(screen.getByRole('button', { name: 'Mark buddy time done' }))

    expect(await screen.findByText('Buddy time done')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByTestId('announcer')).toHaveTextContent('Buddy time done'))
    // No auto-advance (B-31): still Ravi's card.
    expect(screen.getByRole('article', { name: 'Ravi' })).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Undo' }))
    expect(await screen.findByRole('button', { name: 'Mark buddy time done' })).toBeInTheDocument()
    const puts = calls.filter((c) => c.key === `PUT ${cardsUrl}/ravi`).map((c) => c.body)
    expect(puts).toMatchObject([{ homeworkReturned: true }, { done: true }, { done: false }])
  })

  it('a failed save says nothing changed and leaves the tick as it was', async () => {
    setup(() => ({ date: today, cards: [ravi] }), { [`PUT ${cardsUrl}/ravi`]: () => 'offline' })
    renderApp('/back')

    await userEvent.click(await screen.findByRole('checkbox', { name: 'Brought homework' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Could not save. Nothing changed.')
    expect(screen.getByRole('checkbox', { name: 'Brought homework' })).not.toBeChecked()
  })

  it('no classmate on those days: teacher-led; no lesson recorded: says so', async () => {
    setup(() => ({
      date: today,
      cards: [card({ childId: 'ravi', firstName: 'Ravi', buddy: null, competencyId: null, sums: [], keyWordsKn: [], homework: null })],
    }))
    renderApp('/back')

    const article = await screen.findByRole('article', { name: 'Ravi' })
    expect(article).toHaveTextContent('Teacher-led: 10 minutes')
    expect(article).toHaveTextContent('No lesson was recorded on the days away.')
    expect(within(article).queryByRole('checkbox')).toBeNull()
  })

  it('words the three items to suit the lesson: numbers to make with sticks, or questions', async () => {
    setup(() => ({
      date: today,
      cards: [
        card({ childId: 'ravi', firstName: 'Ravi', practiceKind: 'count', sums: [{ question: '528', answer: 528 }] }),
        card({ childId: 'manju', firstName: 'Manju', practiceKind: 'placeValue', sums: [{ question: '4 in 347', answer: 400 }] }),
      ],
    }))
    renderApp('/back')

    const ravi = await screen.findByRole('article', { name: 'Ravi' })
    expect(ravi).toHaveTextContent('3 numbers to make with sticks')
    expect(within(ravi).getByText('Make 528 with sticks')).toBeInTheDocument()
    expect(ravi).not.toHaveTextContent('528 = ?')
    expect(within(ravi).queryByText('Answers (for you)')).toBeNull() // nothing to look up

    await userEvent.click(screen.getByRole('button', { name: 'Next card' }))
    const manju = screen.getByRole('article', { name: 'Manju' })
    expect(manju).toHaveTextContent('3 questions to do together')
    expect(within(manju).getByText('Value of 4 in 347 = ?')).toBeInTheDocument()
  })

  it('nobody back today: the empty state', async () => {
    setup(() => ({ date: today, cards: [] }))
    renderApp('/back')

    expect(await screen.findByRole('heading', { name: 'No one is back today' })).toBeInTheDocument()
  })

  it('offline: the saved cards with a banner', async () => {
    await saveSnapshot(`buddy-cards:${teacher.classId}:${today}`, { date: today, cards: [ravi] })
    setup(() => 'offline')
    renderApp('/back')

    expect(await screen.findByText('No network. The cards are saved on your phone.')).toBeInTheDocument()
    expect(screen.getByRole('article', { name: 'Ravi' })).toBeInTheDocument()
  })

  it('never shows the family’s channel or a score on the card (B-20)', async () => {
    setup(() => ({ date: today, cards: [ravi] }))
    renderApp('/back')

    const article = await screen.findByRole('article', { name: 'Ravi' })
    expect(article).not.toHaveTextContent(/stopped|no phone|consent|caught up|needs buddy|correct/i)
  })
})
