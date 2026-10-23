import { act, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { db } from '../../offline/db'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import { istToday } from './schoolTime'
import type { AttendanceDay } from './types'

const i18n = initI18n()
const date = istToday()
const dayUrl = `GET /api/classes/${teacher.classId}/attendance/${date}`
const saveUrl = `POST /api/classes/${teacher.classId}/attendance`

const children = ['Ananya', 'Bhavana', 'Chetan', 'Ravi', 'Darshan', 'Deepa'].map((firstName, i) => ({
  id: `child-${i + 1}`,
  rollNo: i + 1,
  firstName,
}))

function day(overrides: Partial<AttendanceDay> = {}): AttendanceDay {
  return {
    date,
    status: 'sats',
    receivedAt: `${date}T12:00:00+05:30`,
    corrected: false,
    satsDiffers: false,
    syncAt: null,
    retryAt: null,
    absentChildIds: ['child-3', 'child-4'],
    children,
    ...overrides,
  }
}

function today(attendance: AttendanceDay | FakeRoute, extra: Record<string, FakeRoute> = {}) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: teacher }),
    [dayUrl]: typeof attendance === 'function' ? attendance : () => ({ status: 200, body: attendance }),
    ...extra,
  })
}

describe('TodayScreen', () => {
  beforeEach(async () => {
    localStorage.clear()
    await Promise.all([db.outbox.clear(), db.failed.clear(), db.snapshots.clear()])
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('shows the SATS register with its source and time, and nothing waiting to send', async () => {
    today(day())
    renderApp('/')

    const card = await screen.findByRole('region', { name: /Attendance · SATS \(simulated\), 12:00/i })
    expect(within(card).getByText('2 absent')).toBeInTheDocument()
    expect(within(card).getByText('Chetan')).toBeInTheDocument()
    expect(within(card).getByText('Ravi')).toBeInTheDocument()
    expect(screen.getByText('Saved')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Record' })).toHaveAttribute('aria-pressed', 'false')
  })

  it('a correction edits a draft, saves through the outbox and shows "corrected by you"', async () => {
    let saved: unknown
    let server = day()
    today(() => ({ status: 200, body: server }), {
      [saveUrl]: ({ body }) => {
        saved = body
        server = day({ corrected: true, absentChildIds: ['child-3'] })
        return { status: 200, body: { date, present: 5, absent: 1 } }
      },
    })
    renderApp('/')

    await userEvent.click(await screen.findByRole('button', { name: 'Wrong? Correct it' }))
    expect(screen.getByRole('heading', { name: 'Correct attendance' })).toHaveFocus()
    expect(screen.getByText('2 absent · 4 present')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('checkbox', { name: '4 Ravi, absent' }))
    expect(screen.getByText('1 absent · 5 present')).toBeInTheDocument()
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('region', { name: /corrected by you/ })).toBeInTheDocument()
    await waitFor(() => expect(saved).toMatchObject({ date, absentChildIds: ['child-3'] }))
    await waitFor(() => expect(screen.getByText('Saved')).toBeInTheDocument())
    await waitFor(() => expect(screen.getByTestId('announcer')).toHaveTextContent('Attendance saved'))
  })

  it('Cancel discards the draft and sends nothing', async () => {
    const calls = today(day())
    renderApp('/')

    await userEvent.click(await screen.findByRole('button', { name: 'Wrong? Correct it' }))
    await userEvent.click(screen.getByRole('checkbox', { name: '1 Ananya, present' }))
    await userEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(await screen.findByText('2 absent')).toBeInTheDocument()
    expect(calls.some((c) => c.key === saveUrl)).toBe(false)
    await waitFor(() => expect(screen.getByTestId('announcer')).toHaveTextContent('Changes discarded'))
  })

  it('SATS not in yet: names the sync and retry times, and she can mark it here with everyone present', async () => {
    today(day({ status: 'waiting', receivedAt: null, syncAt: '12:00:00', retryAt: '14:00:00', absentChildIds: [] }))
    renderApp('/')

    expect(await screen.findByText('SATS attendance has not come in yet')).toBeInTheDocument()
    expect(screen.getByText(/It comes in at 12:00.*If it is late, we check again at 2:00/)).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Mark it here if you prefer' }))

    expect(screen.getByRole('heading', { name: 'Mark attendance here' })).toBeInTheDocument()
    expect(screen.getByText('0 absent · 6 present')).toBeInTheDocument()
  })

  it('her own marking, with a later SATS register that differs', async () => {
    today(day({ status: 'manual', satsDiffers: true }))
    renderApp('/')

    expect(await screen.findByRole('region', { name: 'Attendance · marked by you' })).toBeInTheDocument()
    expect(screen.getByText(/SATS attendance came in at 12:00.*We kept your marking\./)).toBeInTheDocument()
  })

  it('says so when nobody is absent', async () => {
    today(day({ absentChildIds: [] }))
    renderApp('/')

    expect(await screen.findByText('Everyone is here today.')).toBeInTheDocument()
  })

  it('still missing after the 2 pm retry: asks her to mark it so tonight’s messages can go', async () => {
    today(day({ status: 'missing', receivedAt: null, absentChildIds: [] }))
    renderApp('/')

    expect(await screen.findByText('Still missing after 2 pm')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /Attendance didn’t come in today/ })).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Mark attendance' }))

    expect(screen.getByRole('heading', { name: 'Mark attendance here' })).toBeInTheDocument()
  })

  it('offline: the correction waits in the outbox and is sent once when the network is back', async () => {
    let online = false
    const calls = today(day(), {
      [saveUrl]: () => (online ? { status: 200, body: { date, present: 3, absent: 3 } } : 'offline'),
    })
    renderApp('/')

    await userEvent.click(await screen.findByRole('button', { name: 'Wrong? Correct it' }))
    await userEvent.click(screen.getByRole('checkbox', { name: '1 Ananya, present' }))
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('1 waiting to send')).toBeInTheDocument()
    expect(screen.getByText('3 absent')).toBeInTheDocument()

    online = true
    await act(async () => {
      window.dispatchEvent(new Event('online'))
    })

    expect(await screen.findByText('Saved')).toBeInTheDocument()
    const posts = calls.filter((c) => c.key === saveUrl)
    expect(posts).toHaveLength(2)
    expect(posts[0]?.body).toEqual(posts[1]?.body)
  })

  it('offline on open: shows the last register it saw', async () => {
    let offline = false
    today(() => (offline ? 'offline' : { status: 200, body: day() }))
    const first = renderApp('/')
    expect(await screen.findByText('2 absent')).toBeInTheDocument()
    first.unmount()

    offline = true
    vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(false)
    renderApp('/')

    expect(await screen.findByText('2 absent')).toBeInTheDocument()
    expect(screen.getByText(/^No network\. Showing what was saved at .+\. It updates when the network returns\.$/)).toBeInTheDocument()
    vi.restoreAllMocks()
  })

  it('bottom nav marks Today and opens the other tabs', async () => {
    today(day())
    renderApp('/')

    const nav = await screen.findByRole('navigation', { name: 'Main' })
    expect(within(nav).getByRole('link', { name: 'Today' })).toHaveAttribute('aria-current', 'page')

    // Week is still a placeholder tab (Approve became T3 in T-14, Missed T4 in T-13, Back today T5 in T-16).
    await userEvent.click(within(nav).getByRole('link', { name: 'Week' }))

    expect(screen.getByRole('heading', { level: 1, name: 'Week' })).toHaveFocus()
    expect(within(nav).getByRole('link', { name: 'Week' })).toHaveAttribute('aria-current', 'page')
  })
})
