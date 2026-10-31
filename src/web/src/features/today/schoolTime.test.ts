import { istToday, setSchoolToday } from './schoolTime'

describe('istToday (T-75)', () => {
  afterEach(() => setSchoolToday(null))

  it('is the server\'s fixed demo date while it has one, and the phone\'s clock again once cleared', () => {
    setSchoolToday('2026-10-31')
    expect(istToday()).toBe('2026-10-31')

    setSchoolToday(null)
    expect(istToday()).toBe(new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Kolkata' }).format(new Date()))
  })

  it('a given instant still wins, in IST', () => {
    setSchoolToday('2026-10-31')
    expect(istToday(new Date('2026-11-12T19:00:00Z'))).toBe('2026-11-13')
  })
})
