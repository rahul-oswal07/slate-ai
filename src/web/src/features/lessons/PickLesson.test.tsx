import { fireEvent, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { db } from '../../offline/db'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import { istToday } from '../today/schoolTime'
import type { AttendanceDay } from '../today/types'
import type { Curriculum, LessonDay } from './types'

const i18n = initI18n()
const date = istToday()
const lessonUrl = `/api/classes/${teacher.classId}/lessons/${date}`

const curriculum: Curriculum = {
  chapters: [
    {
      number: 4,
      nameKn: 'ಕೂಡಿ ನಲಿಯೋಣ',
      nameEn: 'Let’s add and enjoy',
      competencies: [
        { id: 'M3.ADD.CARRY', nameKn: 'ದಶಕ ಸಂಕಲನ', nameEn: '3-digit addition with carrying', sequence: 11, textbookPage: { book: 'textbook', part: 1, page: 85 } },
      ],
    },
    {
      number: 5,
      nameKn: 'ಕಳೆಯುವ ಮೋಜು',
      nameEn: 'The fun of subtracting',
      competencies: [
        { id: 'M3.SUB.NOBORROW', nameKn: 'ಕಳೆಯುವಿಕೆ', nameEn: '3-digit subtraction without borrowing', sequence: 13, textbookPage: { book: 'textbook', part: 2, page: 6 } },
        { id: 'M3.SUB.CHECK', nameKn: 'ಪರಿಶೀಲನೆ', nameEn: 'Check subtraction by adding', sequence: 14, textbookPage: { book: 'textbook', part: 2, page: 4 } },
        { id: 'M3.SUB.BORROW', nameKn: 'ಎರವಲು', nameEn: '3-digit subtraction with borrowing', sequence: 15, textbookPage: { book: 'textbook', part: 2, page: 15 } },
      ],
    },
  ],
}

const attendance = (absent: string[]): AttendanceDay => ({
  date,
  status: 'sats',
  receivedAt: `${date}T12:00:00+05:30`,
  corrected: false,
  satsDiffers: false,
  syncAt: null,
  retryAt: null,
  absentChildIds: absent,
  children: absent.map((id, i) => ({ id, rollNo: i + 1, firstName: `Child ${i + 1}` })),
})

function today(lesson: LessonDay | (() => LessonDay), extra: Record<string, FakeRoute> = {}, absent = ['a', 'b']) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    [`GET /api/classes/${teacher.classId}/attendance/${date}`]: () => ({ status: 200, body: attendance(absent) }),
    'GET /api/curriculum': () => ({ status: 200, body: curriculum }),
    [`GET ${lessonUrl}`]: () => ({ status: 200, body: typeof lesson === 'function' ? lesson() : lesson }),
    ...extra,
  })
}

const noLesson: LessonDay = { date, lesson: null, nextInBook: 'M3.SUB.BORROW' }
const confirmed = (...ids: string[]): LessonDay => ({
  date,
  lesson: { competencyIds: ids, source: 'picked', status: 'confirmed' },
  nextInBook: 'M3.SUB.BORROW',
})

async function openPicker() {
  const pick = await screen.findByRole('button', { name: 'Or pick the lesson from the list' })
  await waitFor(() => expect(pick).toBeEnabled())
  await userEvent.click(pick)
  return pick
}

describe('picking today’s lesson', () => {
  beforeEach(async () => {
    localStorage.clear()
    await Promise.all([db.outbox.clear(), db.failed.clear(), db.snapshots.clear()])
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('opens the chapter of "Next in the book", lists it first, and one tap confirms it', async () => {
    let server = noLesson
    let saved: unknown
    today(() => server, {
      [`PUT ${lessonUrl}`]: ({ body }) => {
        saved = body
        server = confirmed('M3.SUB.BORROW')
        return { status: 200, body: server }
      },
    })
    renderApp('/')

    const pick = await screen.findByRole('button', { name: 'Or pick the lesson from the list' })
    await waitFor(() => expect(pick).toBeEnabled())
    await userEvent.click(pick)

    const sheet = screen.getByRole('dialog', { name: 'Chapter 5 · The fun of subtracting' })
    const radios = within(sheet).getAllByRole('radio')
    expect(radios).toHaveLength(3)
    expect(radios[0]).toHaveAccessibleName(/Next in the book.*3-digit subtraction with borrowing.*Part 2, page 15/)

    await userEvent.click(within(sheet).getByRole('radio', { name: /Next in the book/ }))

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(await screen.findByText('Done for today. Thank you.')).toBeInTheDocument()
    const card = screen.getByRole('region', { name: 'Today’s lesson' })
    expect(within(card).getByText('3-digit subtraction with borrowing')).toBeInTheDocument()
    expect(within(card).getByText('Part 2, page 15')).toBeInTheDocument()
    expect(saved).toMatchObject({ competencyIds: ['M3.SUB.BORROW'] })
    await waitFor(() => expect(screen.getByTestId('announcer')).toHaveTextContent('Lesson saved. You can change it.'))
  })

  it('shows "Added to missed lessons" with the absent count and the lessons joined by +', async () => {
    today(confirmed('M3.SUB.NOBORROW', 'M3.SUB.CHECK'))
    renderApp('/')

    const added = await screen.findByRole('region', { name: 'Added to missed lessons: 2 children' })
    expect(within(added).getByText('who missed 3-digit subtraction without borrowing + Check subtraction by adding')).toBeInTheDocument()
    expect(within(added).getByRole('link', { name: 'See the 2 children' })).toHaveAttribute('href', '/missed')
    expect(screen.getByRole('region', { name: 'Today’s lessons' })).toBeInTheDocument()
  })

  it('no "Added" card when nobody is absent (D-54)', async () => {
    today(confirmed('M3.SUB.CHECK'), {}, [])
    renderApp('/')

    expect(await screen.findByText('Check subtraction by adding')).toBeInTheDocument()
    expect(screen.queryByText(/Added to missed lessons/)).not.toBeInTheDocument()
  })

  it('Change opens the sheet with today’s lesson checked', async () => {
    today(confirmed('M3.SUB.CHECK'))
    renderApp('/')

    await userEvent.click(await screen.findByRole('button', { name: 'Change' }))

    const sheet = screen.getByRole('dialog')
    expect(within(sheet).getByRole('radio', { name: /Check subtraction by adding/ })).toBeChecked()
  })

  it('"Another chapter" lists the chapters and opens the one she chooses', async () => {
    today(noLesson)
    renderApp('/')

    await openPicker()
    await userEvent.click(screen.getByRole('button', { name: 'Another chapter' }))
    await userEvent.click(screen.getByRole('button', { name: 'Chapter 4 · Let’s add and enjoy' }))

    expect(screen.getByRole('dialog', { name: 'Chapter 4 · Let’s add and enjoy' })).toBeInTheDocument()
    expect(screen.getByRole('radio', { name: /3-digit addition with carrying/ })).toBeInTheDocument()
  })

  it('Esc closes the sheet without saving and focus goes back to the opener', async () => {
    const calls = today(noLesson)
    renderApp('/')
    const opener = await openPicker()

    fireEvent(screen.getByRole('dialog'), new Event('cancel', { cancelable: true }))

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(opener).toHaveFocus()
    expect(calls.some((c) => c.key === `PUT ${lessonUrl}`)).toBe(false)
  })

  it('offline: the pick waits in the outbox and Today already shows it', async () => {
    today(noLesson, { [`PUT ${lessonUrl}`]: () => 'offline' })
    renderApp('/')

    await openPicker()
    await userEvent.click(screen.getByRole('radio', { name: /Check subtraction by adding/ }))

    expect(await screen.findByText('1 waiting to send')).toBeInTheDocument()
    expect(screen.getByText('Check subtraction by adding')).toBeInTheDocument()
  })

  it('Kannada UI shows the Kannada lesson name', async () => {
    await i18n.changeLanguage('kn')
    today(confirmed('M3.SUB.CHECK'))
    renderApp('/')

    expect(await screen.findByText('ಪರಿಶೀಲನೆ')).toBeInTheDocument()
    expect(screen.getByText('ಭಾಗ 2, ಪುಟ 4')).toBeInTheDocument()
  })
})
