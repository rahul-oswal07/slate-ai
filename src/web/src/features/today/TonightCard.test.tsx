import { QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { createQueryClient } from '../../app/queryClient'
import { initI18n } from '../../i18n'
import { db } from '../../offline/db'
import { fakeApi, renderApp, teacher } from '../../test/renderApp'
import { todaysMessage } from '../approve/fixtures'
import type { TodaysMessage } from '../approve/todaysTypes'
import type { EveningChild, EveningDay, EveningPreview } from '../evening/types'
import type { Chapter, LessonDay } from '../lessons/types'
import { formatSchoolTime, istToday } from './schoolTime'
import { TonightCard } from './TonightCard'
import type { AttendanceDay } from './types'

const i18n = initI18n()
const base = `/api/classes/${teacher.classId}`

// A fixed school day, so each state's times are known.
const date = '2026-11-24'
const ist = (time: string) => `${date}T${time}+05:30`
const time = (value: string) => formatSchoolTime(value, 'en')

const roster = ['Ravi', 'Manju', 'Asha', 'Esha'].map((firstName, i) => ({ id: `c${i + 1}`, rollNo: i + 1, firstName }))

const chapters: Chapter[] = [
  {
    number: 1,
    nameKn: 'ಸಂಖ್ಯೆಗಳು',
    nameEn: 'Numbers',
    competencies: [
      {
        id: 'M3.BRIDGE.COMPARE',
        nameKn: 'ಹೆಚ್ಚು ಕಡಿಮೆ ಸಮ',
        nameEn: 'More, less, equal',
        sequence: 1,
        textbookPage: { book: 'textbook', part: null, page: 4 },
        hasExercises: false,
      },
    ],
  },
]

const child = (childId: string, outcome: EveningChild['outcome'], language = 'kn', withTodays = false): EveningChild => ({
  childId,
  outcome,
  language,
  withTodays,
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
    children: [child('c1', 'willSend', 'te'), child('c2', 'noPhone'), child('c3', 'willSend'), child('c4', 'stopped')],
    goesAt: ist('18:00:00'),
    waiting: null,
    sentAt: null,
    withTodays: false,
    ...overrides,
  }
}

function show(day: EveningDay, todays?: TodaysMessage) {
  return render(
    <QueryClientProvider client={createQueryClient()}>
      <MemoryRouter>
        <TonightCard
          classId={teacher.classId}
          day={day}
          roster={roster}
          todays={todays}
          lessonIds={['M3.BRIDGE.COMPARE']}
          chapters={chapters}
        />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

const card = (name: string | RegExp) => screen.getByRole('region', { name })

describe("Tonight's message on Today (T-86)", () => {
  beforeEach(async () => {
    vi.useFakeTimers({ toFake: ['Date'] })
    vi.setSystemTime(new Date(ist('15:00:00')))
    await i18n.changeLanguage('en')
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.unstubAllGlobals()
  })

  it('before 6 pm: says when it goes, the checklist in order, and every absent family', () => {
    show(evening())

    const tonight = card('Tonight: message to 2 of 4 families')
    expect(within(tonight).getByText(`Goes at ${time(ist('18:00:00'))}.`)).toBeInTheDocument()
    const items = within(tonight).getAllByRole('listitem').map((li) => li.textContent)
    expect(items.slice(0, 2)).toEqual([
      'Your evening message, approved onceSee an example',
      'Lesson: More, less, equal · p 4',
    ])
    expect(within(tonight).getByRole('link', { name: 'See an example' })).toHaveAttribute('href', '/setup/evening')
    // One line per absent child: language, and Preview, or why nothing goes.
    expect(within(tonight).getByText('తెలుగు')).toBeInTheDocument()
    expect(within(tonight).getByRole('button', { name: 'Preview the message to Ravi’s family' })).toBeInTheDocument()
    expect(within(tonight).getByRole('button', { name: 'Preview the message to Asha’s family' })).toBeInTheDocument()
    expect(within(tonight).getByText('No phone · buddy card')).toBeInTheDocument()
    expect(within(tonight).getByText('Stopped by family')).toBeInTheDocument()
    expect(within(tonight).queryByRole('button', { name: /Manju|Esha/ })).not.toBeInTheDocument()
  })

  it.each(['ready', 'failed'] as const)(
    "today's message %s before 6 pm: waiting for her, with Check and approve and the time it goes without it",
    (status) => {
      show(evening({ goesAt: ist('18:05:00'), waiting: 'forYou' }), todaysMessage({ status }))

      const tonight = card('Tonight: message to 2 of 4 families')
      expect(within(tonight).getByText('Today’s message is waiting for you')).toBeInTheDocument()
      expect(within(tonight).getByRole('link', { name: 'Check and approve' })).toHaveAttribute('href', '/approve')
      expect(
        within(tonight).getByText(
          `Goes at ${time(ist('18:00:00'))}. Approve today’s message by then, or it goes without it at ${time(ist('18:05:00'))}.`,
        ),
      ).toBeInTheDocument()
    },
  )

  it("today's message being written: no time yet, it goes once it's ready", () => {
    vi.setSystemTime(new Date(ist('18:01:00')))
    show(evening({ goesAt: null, waiting: 'writing' }), todaysMessage({ status: 'generating' }))

    const tonight = card('Tonight: message to 2 of 4 families')
    expect(within(tonight).getByText('Today’s message is being written')).toBeInTheDocument()
    expect(within(tonight).getByText('Tonight’s message goes once today’s message is ready.')).toBeInTheDocument()
  })

  it('after 6 pm, ready and waiting: counts down the minutes left', () => {
    vi.setSystemTime(new Date(ist('19:12:00')))
    show(evening({ goesAt: ist('19:15:00'), waiting: 'forYou' }), todaysMessage())

    expect(
      within(card('Tonight: message to 2 of 4 families')).getByText(
        `Waiting for you until ${time(ist('19:15:00'))} (3 min). Then it goes without today’s message.`,
      ),
    ).toBeInTheDocument()
  })

  it("approved: goes with today's message, and each family says whether theirs carries it", () => {
    show(
      evening({
        withTodays: true,
        children: [child('c1', 'willSend', 'te', false), child('c3', 'willSend', 'kn', true)],
        absent: 2,
      }),
      todaysMessage({ status: 'approved', approvedVersion: 1 }),
    )

    const tonight = card('Tonight: message to 2 of 2 families')
    expect(within(tonight).getByText(`Goes at ${time(ist('18:00:00'))} with today’s message.`)).toBeInTheDocument()
    expect(within(tonight).getByText('Today’s message: approved')).toBeInTheDocument()
    expect(within(tonight).getByRole('link', { name: 'See today’s message' })).toHaveAttribute('href', '/approve')
    expect(within(tonight).queryByRole('link', { name: 'Check and approve' })).not.toBeInTheDocument()
    expect(within(tonight).getByText('· text only')).toBeInTheDocument()
    expect(within(tonight).getByText('· with today’s message')).toBeInTheDocument()
  })

  it('due and not held: going now', () => {
    vi.setSystemTime(new Date(ist('18:00:20')))
    show(evening())

    expect(within(card('Tonight: message to 2 of 4 families')).getByText('Going now.')).toBeInTheDocument()
  })

  it.each([
    [true, 'with today’s message'],
    [false, 'text only'],
  ])('sent (with today’s message: %s): a receipt with the time, and only who was left out', (withTodays, mode) => {
    vi.setSystemTime(new Date(ist('19:00:00')))
    show(
      evening({
        sent: 2,
        sentAt: ist('18:01:00'),
        goesAt: null,
        withTodays,
        children: [child('c1', 'sent', 'te'), child('c2', 'noPhone'), child('c3', 'sent', 'kn', withTodays), child('c4', 'stopped')],
      }),
      todaysMessage({ status: 'approved', approvedVersion: 1, sent: withTodays }),
    )

    const receipt = card('Sent tonight to 2 of 4 families')
    expect(within(receipt).getByText(`Sent at ${time(ist('18:01:00'))} · ${mode}`)).toBeInTheDocument()
    expect(within(receipt).getByText('Didn’t get it tonight:')).toBeInTheDocument()
    expect(within(receipt).getByText('Manju')).toBeInTheDocument()
    expect(within(receipt).getByText('Esha')).toBeInTheDocument()
    // The answers card lists who got it; no checklist or Preview once it has gone.
    expect(within(receipt).queryByText('Ravi')).not.toBeInTheDocument()
    expect(within(receipt).queryByRole('button')).not.toBeInTheDocument()
    expect(within(receipt).queryByText(/approved once/)).not.toBeInTheDocument()
  })

  it('no family can get it: "No messages tonight." with who and why', () => {
    show(evening({ families: 0, goesAt: null, children: [child('c2', 'noPhone')] }))

    const tonight = card('No messages tonight.')
    expect(within(tonight).getByText('No phone · buddy card')).toBeInTheDocument()
    expect(within(tonight).queryByText(/Goes at/)).not.toBeInTheDocument()
  })

  it('template not approved: links to the approval step', () => {
    show(evening({ templateApproved: false, blocked: 'templateNotApproved', goesAt: null }))

    const tonight = card(/not approved yet/)
    expect(within(tonight).getByRole('link', { name: 'Review and approve' })).toHaveAttribute('href', '/setup/evening')
  })

  it("Preview opens that family's real message in their language, and Close returns to the button", async () => {
    const preview: EveningPreview = {
      childId: 'c1',
      language: 'te',
      text: 'ఈ రోజు Ravi … పోల్చడం … పేజీ 4',
      withTodays: false,
      sent: false,
      sentAt: null,
    }
    const calls = fakeApi({ [`GET ${base}/evening-messages/${date}/children/c1`]: () => ({ status: 200, body: preview }) })
    const user = userEvent.setup()
    show(evening())

    const button = screen.getByRole('button', { name: 'Preview the message to Ravi’s family' })
    await user.click(button)

    const sheet = await screen.findByRole('dialog', { name: 'Ravi’s family · తెలుగు' })
    expect(within(sheet).getByText(preview.text)).toHaveAttribute('lang', 'te')
    expect(within(sheet).getByText('Only this text goes.')).toBeInTheDocument()
    expect(calls.map((c) => c.key)).toEqual([`GET ${base}/evening-messages/${date}/children/c1`])

    await user.click(within(sheet).getByRole('button', { name: 'Close' }))
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(button).toHaveFocus()
  })
})

describe("Tonight's message on the Today screen", () => {
  const today = istToday()
  const attendance: AttendanceDay = {
    date: today,
    status: 'sats',
    receivedAt: `${today}T09:00:00+05:30`,
    corrected: false,
    satsDiffers: false,
    syncAt: null,
    retryAt: null,
    absentChildIds: ['c1', 'c2'],
    children: roster,
  }
  const lesson = (confirmed: boolean): LessonDay => ({
    date: today,
    lesson: confirmed
      ? { competencyIds: ['M3.SUB.BORROW'], source: 'picked', status: 'confirmed', summaryKn: null, summaryEn: null, examplesUsed: [] }
      : null,
    nextInBook: 'M3.SUB.BORROW',
  })

  function api(eveningDay: EveningDay | null, opts: { confirmed?: boolean; todays?: TodaysMessage } = {}) {
    fakeApi({
      'GET /api/me': () => ({ status: 200, body: teacher }),
      [`GET ${base}/attendance/${today}`]: () => ({ status: 200, body: attendance }),
      [`GET ${base}/curriculum`]: () => ({ status: 200, body: { chapters: [] } }),
      [`GET ${base}/lessons/${today}`]: () => ({ status: 200, body: lesson(opts.confirmed !== false) }),
      [`GET ${base}/evening-messages/${today}`]: () => (eveningDay ? { status: 200, body: eveningDay } : { status: 500 }),
      [`GET ${base}/todays-message/${today}`]: () => (opts.todays ? { status: 200, body: opts.todays } : { status: 204 }),
    })
  }

  beforeEach(async () => {
    localStorage.clear()
    await Promise.all([db.outbox.clear(), db.failed.clear(), db.snapshots.clear()])
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it("one card: today's message is inside it, not a card of its own", async () => {
    api(
      evening({ date: today, absent: 2, children: [child('c1', 'willSend', 'te'), child('c2', 'noPhone')], goesAt: `${today}T18:00:00+05:30` }),
      { todays: todaysMessage() },
    )
    renderApp('/')

    const tonight = await screen.findByRole('region', { name: 'Tonight: message to 2 of 2 families' })
    expect(await within(tonight).findByText('Today’s message is waiting for you')).toBeInTheDocument()
    expect(screen.queryByRole('region', { name: 'Today’s message is waiting for you' })).not.toBeInTheDocument()
  })

  it('hidden until the lesson is confirmed, and when it cannot load', async () => {
    api(evening({ date: today }), { confirmed: false })
    const { unmount } = renderApp('/')
    expect(await screen.findByRole('button', { name: 'Pick from the list' })).toBeInTheDocument()
    expect(screen.queryByRole('region', { name: /tonight/i })).not.toBeInTheDocument()
    unmount()
    vi.unstubAllGlobals()

    api(null)
    renderApp('/')
    expect(await screen.findByText('Done for today. Thank you.')).toBeInTheDocument()
    expect(screen.queryByRole('region', { name: /tonight/i })).not.toBeInTheDocument()
  })
})
