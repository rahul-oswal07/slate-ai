import { api } from '../../api/client'
import { fakeApi, teacher } from '../../test/renderApp'
import { saveCachedMe } from '../auth/session'
import { chosenSchoolDate, schoolToday, setSchoolDate, SCHOOL_DATE_HEADER, stepSchoolDate } from './schoolDate'
import { istToday } from './schoolTime'

/** yyyy-mm-dd `days` from the real IST today. */
function fromToday(days: number) {
  const date = new Date(`${istToday()}T12:00:00+05:30`)
  date.setUTCDate(date.getUTCDate() + days)
  return istToday(date)
}

describe('the chosen school day (T-134, D-120)', () => {
  // The demo teacher with the admin switch on, as the last /api/me said.
  beforeEach(() => saveCachedMe({ ...teacher, demo: { sampleVoiceNote: false, schoolDate: true } }))

  afterEach(() => {
    setSchoolDate(null)
    vi.unstubAllGlobals()
  })

  it('is the real day until a past day is chosen, and again after Back to today', () => {
    expect(chosenSchoolDate()).toBeNull()
    expect(schoolToday()).toBe(istToday())

    setSchoolDate(fromToday(-4))
    expect(chosenSchoolDate()).toBe(fromToday(-4))
    expect(schoolToday()).toBe(fromToday(-4))

    setSchoolDate(null)
    expect(schoolToday()).toBe(istToday())
  })

  it('never goes to today or later: that is the real day', () => {
    setSchoolDate(fromToday(-4))
    setSchoolDate(istToday())
    expect(chosenSchoolDate()).toBeNull()

    setSchoolDate(fromToday(3))
    expect(chosenSchoolDate()).toBeNull()
  })

  it('counts only while the admin switch is on', () => {
    setSchoolDate(fromToday(-4))
    saveCachedMe({ ...teacher, demo: { sampleVoiceNote: false, schoolDate: false } })
    expect(chosenSchoolDate()).toBeNull()
    expect(schoolToday()).toBe(istToday())

    saveCachedMe({ ...teacher, demo: null })
    expect(chosenSchoolDate()).toBeNull()
  })

  it('ignores a stored value that is not a past day', () => {
    localStorage.setItem('slate.schoolDate', 'garbage')
    expect(chosenSchoolDate()).toBeNull()
    localStorage.setItem('slate.schoolDate', fromToday(2))
    expect(chosenSchoolDate()).toBeNull()
  })

  it('steps by days or weeks, and stepping to today or past it is the real day', () => {
    setSchoolDate(fromToday(-10))
    stepSchoolDate(-1)
    expect(chosenSchoolDate()).toBe(fromToday(-11))
    stepSchoolDate(7)
    expect(chosenSchoolDate()).toBe(fromToday(-4))
    stepSchoolDate(7)
    expect(chosenSchoolDate()).toBeNull()

    stepSchoolDate(-7)
    expect(chosenSchoolDate()).toBe(fromToday(-7))
  })

  it('is sent with every API call while chosen, and not otherwise', async () => {
    const calls = fakeApi({ 'GET /api/x': () => ({ status: 200, body: {} }) })

    await api('/api/x')
    setSchoolDate(fromToday(-4))
    await api('/api/x')
    await api('/api/x', { schoolDate: null })

    expect(calls.map((c) => c.headers[SCHOOL_DATE_HEADER])).toEqual([undefined, fromToday(-4), undefined])
  })
})
