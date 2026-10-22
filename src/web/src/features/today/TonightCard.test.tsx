import { screen, within } from '@testing-library/react'
import { initI18n } from '../../i18n'
import { db } from '../../offline/db'
import { fakeApi, renderApp, teacher } from '../../test/renderApp'
import type { EveningDay } from '../evening/types'
import type { LessonDay } from '../lessons/types'
import { istToday } from './schoolTime'
import type { AttendanceDay } from './types'

const i18n = initI18n()
const date = istToday()
const base = `/api/classes/${teacher.classId}`

const children = ['Ravi', 'Manju', 'Asha', 'Esha'].map((firstName, i) => ({ id: `c${i + 1}`, rollNo: i + 1, firstName }))

const attendance = (absent: string[]): AttendanceDay => ({
  date,
  status: 'sats',
  receivedAt: `${date}T12:00:00+05:30`,
  corrected: false,
  satsDiffers: false,
  syncAt: null,
  retryAt: null,
  absentChildIds: absent,
  children,
})

const lesson = (status: 'confirmed' | null): LessonDay => ({
  date,
  lesson: status
    ? { competencyIds: ['M3.SUB.BORROW'], source: 'picked', status, summaryKn: null, summaryEn: null, examplesUsed: [] }
    : null,
  nextInBook: 'M3.SUB.BORROW',
})

function evening(overrides: Partial<EveningDay> = {}): EveningDay {
  return {
    date,
    blocked: null,
    templateApproved: true,
    lessonConfirmed: true,
    absent: 4,
    families: 2,
    sent: 0,
    sendsAt: '18:00:00',
    children: [
      { childId: 'c1', outcome: 'willSend' },
      { childId: 'c2', outcome: 'noPhone' },
      { childId: 'c3', outcome: 'willSend' },
      { childId: 'c4', outcome: 'stopped' },
    ],
    ...overrides,
  }
}

function today(eveningDay: EveningDay | null, opts: { absent?: string[]; confirmed?: boolean } = {}) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    [`GET ${base}/attendance/${date}`]: () => ({ status: 200, body: attendance(opts.absent ?? ['c1', 'c2', 'c3', 'c4']) }),
    'GET /api/curriculum': () => ({ status: 200, body: { chapters: [] } }),
    [`GET ${base}/lessons/${date}`]: () => ({ status: 200, body: lesson(opts.confirmed === false ? null : 'confirmed') }),
    [`GET ${base}/evening-messages/${date}`]: () => (eveningDay ? { status: 200, body: eveningDay } : { status: 500 }),
  })
}

describe('TonightCard', () => {
  beforeEach(async () => {
    localStorage.clear()
    await Promise.all([db.outbox.clear(), db.failed.clear(), db.snapshots.clear()])
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('states the arithmetic and names each family left out with a tag', async () => {
    today(evening())
    renderApp('/')

    const card = await screen.findByRole('region', { name: 'After 6 pm tonight: message to 2 of 4 families' })
    expect(within(card).getByText('The message you approved once, with today’s lesson and page.')).toBeInTheDocument()
    expect(within(card).getByText('Manju')).toBeInTheDocument()
    expect(within(card).getByText('No phone · buddy card')).toBeInTheDocument()
    expect(within(card).getByText('Esha')).toBeInTheDocument()
    expect(within(card).getByText('Stopped by family')).toBeInTheDocument()
    expect(within(card).queryByText('Ravi')).not.toBeInTheDocument()
    expect(within(card).getByRole('link', { name: 'See the message' })).toHaveAttribute('href', '/setup/evening')
  })

  it('after the send it says how many went', async () => {
    today(evening({ sent: 2 }))
    renderApp('/')

    expect(await screen.findByRole('region', { name: 'Sent tonight to 2 of 4 families' })).toBeInTheDocument()
  })

  it('no family can get it: "No messages tonight."', async () => {
    today(evening({ families: 0, children: [{ childId: 'c2', outcome: 'noPhone' }] }))
    renderApp('/')

    const card = await screen.findByRole('region', { name: 'No messages tonight.' })
    expect(within(card).queryByText(/approved once/)).not.toBeInTheDocument()
  })

  it('template not approved: links to the approval step', async () => {
    today(evening({ templateApproved: false, blocked: 'templateNotApproved' }))
    renderApp('/')

    const card = await screen.findByRole('region', { name: /not approved yet/ })
    expect(within(card).getByRole('link', { name: 'Review and approve' })).toHaveAttribute('href', '/setup/evening')
  })

  it('hidden until the lesson is confirmed, and when it cannot load', async () => {
    today(evening(), { confirmed: false })
    const { unmount } = renderApp('/')
    expect(await screen.findByRole('button', { name: 'Or pick the lesson from the list' })).toBeInTheDocument()
    expect(screen.queryByRole('region', { name: /tonight/i })).not.toBeInTheDocument()
    unmount()
    vi.unstubAllGlobals()

    today(null)
    renderApp('/')
    expect(await screen.findByText('Done for today. Thank you.')).toBeInTheDocument()
    expect(screen.queryByRole('region', { name: /tonight/i })).not.toBeInTheDocument()
  })
})
