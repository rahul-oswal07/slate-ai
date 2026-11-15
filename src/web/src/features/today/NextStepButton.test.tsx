import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { db } from '../../offline/db'
import { installFakeMedia } from '../../test/fakeMedia'
import { fakeApi, renderApp, teacher } from '../../test/renderApp'
import { todaysMessage } from '../approve/fixtures'
import type { TodaysMessage } from '../approve/todaysTypes'
import type { EveningDay } from '../evening/types'
import type { Curriculum, LessonDay, LessonStatus } from '../lessons/types'
import { istToday } from './schoolTime'
import type { AttendanceDay } from './types'
import type { TodaysAnswers } from './useTodaysAnswers'

const i18n = initI18n()
const date = istToday()
const base = `/api/classes/${teacher.classId}`

const curriculum: Curriculum = {
  chapters: [
    {
      number: 5,
      nameKn: 'ಕಳೆಯುವ ಮೋಜು',
      nameEn: 'The fun of subtracting',
      competencies: [
        { id: 'M3.SUB.MAKE', nameKn: 'ಸಮಸ್ಯಾ ರಚನೆ', nameEn: 'Making subtraction sums', sequence: 14, textbookPage: { book: 'textbook', part: 1, page: 117 }, hasExercises: false },
      ],
    },
  ],
}

const attendance: AttendanceDay = {
  date,
  status: 'sats',
  receivedAt: `${date}T09:00:00+05:30`,
  corrected: false,
  satsDiffers: false,
  syncAt: null,
  retryAt: null,
  absentChildIds: ['a', 'b'],
  children: [
    { id: 'a', rollNo: 1, firstName: 'Ravi' },
    { id: 'b', rollNo: 2, firstName: 'Asha' },
  ],
}

const lessonDay = (status: LessonStatus | null): LessonDay => ({
  date,
  lesson: status
    ? { competencyIds: ['M3.SUB.MAKE'], source: 'voice', status, summaryKn: null, summaryEn: null, examplesUsed: [] }
    : null,
  nextInBook: null,
})

const evening = (sent: number): EveningDay => ({
  date,
  blocked: null,
  templateApproved: true,
  lessonConfirmed: true,
  absent: 2,
  families: 2,
  sent,
  sendsAt: '18:00:00',
  children: [],
})

const answers: TodaysAnswers = {
  sent: 2,
  answered: 1,
  locked: 2,
  rows: [
    { childId: 'a', firstName: 'Ravi', rollNo: 1, answer: '>', level: 'gotIt' },
    { childId: 'b', firstName: 'Asha', rollNo: 2, answer: null, level: 'notAnswered' },
  ],
}

function today(opts: { lesson: LessonStatus | null; todays?: TodaysMessage | null; answers?: TodaysAnswers }) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    [`GET ${base}/attendance/${date}`]: () => ({ status: 200, body: attendance }),
    'GET /api/curriculum': () => ({ status: 200, body: curriculum }),
    [`GET ${base}/lessons/${date}`]: () => ({ status: 200, body: lessonDay(opts.lesson) }),
    [`GET ${base}/evening-messages/${date}`]: () => ({ status: 200, body: evening(opts.answers ? 2 : 0) }),
    [`GET ${base}/todays-message/${date}`]: () => (opts.todays ? { status: 200, body: opts.todays } : { status: 204 }),
    [`GET ${base}/todays-message/${date}/answers`]: () => (opts.answers ? { status: 200, body: opts.answers } : { status: 204 }),
  })
}

/** The one primary button on Today, if any: the next step sits right under the day strip. */
const primaries = () => document.querySelectorAll('.btn-primary')

/** One next step on Today (T-96, G3): the only primary button, and it follows the day. */
describe('Today’s next step (T-96)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await Promise.all([db.outbox.clear(), db.notes.clear(), db.photos.clear(), db.snapshots.clear()])
    await i18n.changeLanguage('en')
    installFakeMedia()
  })

  afterEach(() => vi.unstubAllGlobals())

  it('no lesson yet: Record today’s lesson takes her to the mic', async () => {
    today({ lesson: null })
    renderApp('/')

    await userEvent.click(await screen.findByRole('button', { name: 'Record today’s lesson' }))

    expect(screen.getByRole('button', { name: 'Record' })).toHaveFocus()
    expect(primaries()).toHaveLength(1)
  })

  it('a note heard but not confirmed: Confirm today’s lesson opens Lesson confirm', async () => {
    today({ lesson: 'mapped' })
    renderApp('/')

    expect(await screen.findByRole('link', { name: 'Confirm today’s lesson' })).toHaveAttribute('href', '/lesson')
    expect(primaries()).toHaveLength(1)
  })

  it('today’s message waiting: Check today’s message is the one primary; the card’s button and the mic are not', async () => {
    today({ lesson: 'confirmed', todays: todaysMessage({ status: 'ready' }) })
    renderApp('/')

    const next = await screen.findByRole('link', { name: 'Check today’s message' })

    expect(next).toHaveAttribute('href', '/approve')
    expect(screen.getByRole('link', { name: 'Check and approve' })).toHaveClass('btn-secondary')
    expect(screen.getByRole('button', { name: 'Record' })).not.toHaveClass('btn-hero')
    expect(primaries()).toHaveLength(1)
  })

  it('approved and waiting for 6 pm: no next step', async () => {
    today({ lesson: 'confirmed', todays: todaysMessage({ status: 'approved', approvedVersion: 1 }) })
    renderApp('/')

    expect(await screen.findByText('Today’s message: approved · goes after 6 pm')).toBeInTheDocument()
    expect(primaries()).toHaveLength(0)
  })

  it('confirmed without a today’s message: no next step', async () => {
    today({ lesson: 'confirmed', todays: null })
    renderApp('/')

    // Nothing goes until tonight's send, so the record card stays with the lesson as one line (T-114).
    expect(await screen.findByText('Today: Making subtraction sums · p 117')).toBeInTheDocument()
    await waitFor(() => expect(screen.getByRole('button', { name: 'Record' })).toBeInTheDocument())
    expect(screen.queryByRole('button', { name: 'Record today’s lesson' })).not.toBeInTheDocument()
    expect(primaries()).toHaveLength(0)
  })

  it('once families have it: See answers takes her to the answers', async () => {
    today({ lesson: 'confirmed', todays: todaysMessage({ status: 'approved', approvedVersion: 1, sent: true }), answers })
    renderApp('/')

    await userEvent.click(await screen.findByRole('button', { name: 'See answers' }))

    expect(screen.getByRole('heading', { name: /Today's message: sent to 2/ })).toHaveFocus()
    expect(primaries()).toHaveLength(1)
  })
})
