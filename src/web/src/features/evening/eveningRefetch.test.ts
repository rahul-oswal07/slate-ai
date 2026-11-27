import type { EveningDay } from './types'
import { eveningRefetchMs } from './useEvening'

const ist = (time: string) => `2026-11-24T${time}+05:30`
const at = (time: string) => Date.parse(ist(time))

const day = (overrides: Partial<EveningDay>): EveningDay => ({
  date: '2026-11-24',
  blocked: null,
  templateApproved: true,
  lessonConfirmed: true,
  absent: 2,
  families: 2,
  sent: 0,
  sendsAt: '18:00:00',
  children: [],
  goesAt: ist('18:00:00'),
  waiting: null,
  sentAt: null,
  withTodays: false,
  ...overrides,
})

describe('when Today asks again about tonight (T-86)', () => {
  it('asks again just after it goes, so the card becomes the receipt by itself', () => {
    expect(eveningRefetchMs(day({}), at('17:00:00'))).toBe(60 * 60_000 + 5_000)
  })

  it('every 30 s once it is due or nearly due', () => {
    expect(eveningRefetchMs(day({}), at('17:59:50'))).toBe(30_000)
    expect(eveningRefetchMs(day({}), at('18:02:00'))).toBe(30_000)
  })

  it('every few seconds while today’s message is written', () => {
    expect(eveningRefetchMs(day({ goesAt: null, waiting: 'writing' }), at('18:01:00'))).toBe(5_000)
  })

  it('not at all once sent, or when nothing is due', () => {
    expect(eveningRefetchMs(day({ sent: 2, goesAt: null, sentAt: ist('18:01:00') }), at('19:00:00'))).toBe(false)
    expect(eveningRefetchMs(undefined)).toBe(false)
  })
})
