import { act, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { initI18n } from '../../i18n'
import { db } from '../../offline/db'
import { fakeApi, renderApp, teacher, type FakeRoute } from '../../test/renderApp'
import type { ClassInfo, Me } from '../auth/session'
import { istToday } from './schoolTime'
import type { AttendanceDay } from './types'

const i18n = initI18n()
const date = istToday()

const classA: ClassInfo = { id: teacher.classId, name: '3A', grade: 3, medium: 'kn', classTeacherName: 'Shobha', demo: null }
const classB: ClassInfo = { id: 'class-b', name: '3B', grade: 3, medium: 'kn', classTeacherName: 'Monika', demo: null }
// T-115: Monika is the main teacher of 3B (her default) and a co-teacher of 3A.
const monika: Me = {
  ...teacher,
  teacherId: 'monika',
  displayName: 'Monika',
  classId: classB.id,
  grade: 3,
  medium: 'kn',
  classTeacherName: 'Monika',
  classes: [classB, classA],
}

const dayUrl = (classId: string) => `GET /api/classes/${classId}/attendance/${date}`
const saveUrl = (classId: string) => `POST /api/classes/${classId}/attendance`

function day(names: string[]): AttendanceDay {
  return {
    date,
    status: 'sats',
    receivedAt: `${date}T09:00:00+05:30`,
    corrected: false,
    satsDiffers: false,
    syncAt: null,
    retryAt: null,
    absentChildIds: ['child-2'],
    children: names.map((firstName, i) => ({ id: `child-${i + 1}`, rollNo: i + 1, firstName })),
  }
}

function api(me: Me, extra: Record<string, FakeRoute> = {}) {
  return fakeApi({
    'GET /api/me': () => ({ status: 200, body: me }),
    [dayUrl(classA.id)]: () => ({ status: 200, body: day(['Ananya', 'Bhavana']) }),
    [dayUrl(classB.id)]: () => ({ status: 200, body: day(['Aditi', 'Bharath']) }),
    ...extra,
  })
}

describe('ClassPicker on Today (T-115)', () => {
  beforeEach(async () => {
    localStorage.clear()
    await Promise.all([db.outbox.clear(), db.failed.clear(), db.snapshots.clear()])
    await i18n.changeLanguage('en')
  })

  afterEach(() => vi.unstubAllGlobals())

  it('lists her classes above the date and switches the whole app, and the choice survives a reload', async () => {
    api(monika)
    const { unmount } = renderApp('/')

    const picker = await screen.findByRole('combobox', { name: 'Class' })
    expect(within(picker).getAllByRole('option').map((o) => o.textContent)).toEqual([
      '3B · Grade 3 · ಕನ್ನಡ medium',
      '3A · Grade 3 · ಕನ್ನಡ medium',
    ])
    expect(picker).toHaveValue(classB.id)
    expect(await screen.findByText("3B · Grade 3 · ಕನ್ನಡ medium · Monika's class")).toBeInTheDocument()
    await userEvent.click(await screen.findByRole('button', { name: 'Wrong? Correct it' }))
    expect(screen.getByRole('checkbox', { name: '1 Aditi, present' })).toBeInTheDocument()

    await userEvent.selectOptions(picker, classA.id)

    // The half-done correction in 3B is left behind; 3A's register and header show.
    expect(await screen.findByText("3A · Grade 3 · ಕನ್ನಡ medium · Shobha's class")).toBeInTheDocument()
    await userEvent.click(await screen.findByRole('button', { name: 'Wrong? Correct it' }))
    expect(screen.getByRole('checkbox', { name: '1 Ananya, present' })).toBeInTheDocument()

    unmount()
    renderApp('/')
    expect(await screen.findByRole('combobox', { name: 'Class' })).toHaveValue(classA.id)
  })

  it('is there with one class too, with that class chosen', async () => {
    api({ ...teacher, grade: 3, medium: 'kn', classTeacherName: 'Shobha', classes: [classA] })
    renderApp('/')

    const picker = await screen.findByRole('combobox', { name: 'Class' })
    expect(picker).toHaveValue(classA.id)
    expect(within(picker).getAllByRole('option')).toHaveLength(1)
  })

  it('a mark saved offline in one class reaches that class after she switches to another', async () => {
    let online = false
    const calls = api(monika, {
      [saveUrl(classB.id)]: () => (online ? { status: 200, body: { date, present: 2, absent: 0 } } : 'offline'),
      [saveUrl(classA.id)]: () => ({ status: 200, body: { date, present: 2, absent: 0 } }),
    })
    renderApp('/')

    await userEvent.click(await screen.findByRole('button', { name: 'Wrong? Correct it' }))
    await userEvent.click(screen.getByRole('checkbox', { name: '2 Bharath, absent' }))
    await userEvent.click(screen.getByRole('button', { name: 'Save' }))
    expect(await screen.findByText('1 waiting to send')).toBeInTheDocument()

    await userEvent.selectOptions(screen.getByRole('combobox', { name: 'Class' }), classA.id)
    online = true
    await act(async () => {
      window.dispatchEvent(new Event('online'))
    })

    await waitFor(() => expect(calls.filter((c) => c.key === saveUrl(classB.id))).toHaveLength(2))
    expect(calls.some((c) => c.key === saveUrl(classA.id))).toBe(false)
  })
})
