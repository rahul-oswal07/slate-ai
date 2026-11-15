import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { db } from '../../offline/db'
import { installFakeMedia } from '../../test/fakeMedia'
import { fakeApi, renderApp, teacher } from '../../test/renderApp'
import { todaysMessage } from '../approve/fixtures'
import type { TodaysMessage } from '../approve/todaysTypes'
import type { EveningDay } from '../evening/types'
import type { Curriculum, LessonDay } from '../lessons/types'
import { istToday } from './schoolTime'
import type { AttendanceDay } from './types'

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

const attendance = (absent: string[]): AttendanceDay => ({
  date,
  status: 'sats',
  receivedAt: `${date}T09:00:00+05:30`,
  corrected: false,
  satsDiffers: false,
  syncAt: null,
  retryAt: null,
  absentChildIds: absent,
  children: absent.map((id, i) => ({ id, rollNo: i + 1, firstName: `Child ${i + 1}` })),
})

const confirmed: LessonDay = {
  date,
  lesson: { competencyIds: ['M3.SUB.MAKE'], source: 'picked', status: 'confirmed', summaryKn: null, summaryEn: null, examplesUsed: [] },
  nextInBook: null,
}

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

function today(opts: { todays: TodaysMessage | null; sent?: number; absent?: string[] }) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    [`GET ${base}/attendance/${date}`]: () => ({ status: 200, body: attendance(opts.absent ?? ['a', 'b']) }),
    'GET /api/curriculum': () => ({ status: 200, body: curriculum }),
    [`GET ${base}/lessons/${date}`]: () => ({ status: 200, body: confirmed }),
    [`GET ${base}/evening-messages/${date}`]: () => ({ status: 200, body: evening(opts.sent ?? 0) }),
    [`GET ${base}/todays-message/${date}`]: () => (opts.todays ? { status: 200, body: opts.todays } : { status: 204 }),
  })
}

const line = 'Today: Making subtraction sums · p 117'

/** Once confirmed, Today keeps the record card while a new note can still replace the lesson (T-114, D-104). */
describe('Today record card after confirming (T-114)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await Promise.all([db.outbox.clear(), db.notes.clear(), db.photos.clear(), db.snapshots.clear()])
    await i18n.changeLanguage('en')
    installFakeMedia()
  })

  afterEach(() => vi.unstubAllGlobals())

  it('with today’s message not approved yet: the mic stays, with the lesson as one line and Change', async () => {
    today({ todays: todaysMessage({ status: 'ready' }) })
    renderApp('/')

    expect(await screen.findByText(line)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Record' })).toBeInTheDocument()
    expect(screen.queryByText('Done for today. Thank you.')).not.toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Change' }))
    expect(within(screen.getByRole('dialog')).getByRole('radio', { name: /Making subtraction sums/ })).toBeChecked()
  })

  it('once today’s message is approved: only the confirmed lesson', async () => {
    today({ todays: todaysMessage({ status: 'approved', approvedVersion: 1 }) })
    renderApp('/')

    expect(await screen.findByText('Done for today. Thank you.')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Record' })).not.toBeInTheDocument()
    expect(screen.queryByText(line)).not.toBeInTheDocument()
  })

  it('without today’s message: the mic stays until tonight’s message has gone', async () => {
    today({ todays: null, sent: 0 })
    renderApp('/')

    expect(await screen.findByText(line)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Record' })).toBeInTheDocument()
  })

  it('without today’s message, after tonight’s send: only the confirmed lesson', async () => {
    today({ todays: null, sent: 2 })
    renderApp('/')

    expect(await screen.findByText('Done for today. Thank you.')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Record' })).not.toBeInTheDocument())
  })

  it('nobody away: nothing goes tonight, so the mic stays all day', async () => {
    today({ todays: null, absent: [] })
    renderApp('/')

    expect(await screen.findByText(line)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Record' })).toBeInTheDocument()
  })
})
