import { istToday } from './schoolTime'

describe('istToday', () => {
  it('is the date in India, whatever the phone\'s zone', () => {
    expect(istToday(new Date('2026-11-12T19:00:00Z'))).toBe('2026-11-13')
    expect(istToday(new Date('2026-11-13T18:00:00Z'))).toBe('2026-11-13')
  })
})
