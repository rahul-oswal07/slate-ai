import type { EveningDay } from '../evening/types'
import { tonightStatus } from './tonightStatus'

const date = '2026-11-24'
const ist = (time: string) => `${date}T${time}+05:30`
const at = (time: string) => Date.parse(ist(time))

function day(overrides: Partial<EveningDay> = {}): EveningDay {
  return {
    date,
    blocked: null,
    templateApproved: true,
    lessonConfirmed: true,
    absent: 3,
    families: 2,
    sent: 0,
    sendsAt: '18:00:00',
    children: [],
    goesAt: ist('18:00:00'),
    waiting: null,
    sentAt: null,
    withTodays: false,
    ...overrides,
  }
}

describe("Tonight's status line (T-86)", () => {
  it('before 6 pm: goes at the send time, with today’s message once approved', () => {
    expect(tonightStatus(day(), at('15:00:00'))).toEqual({ kind: 'goesAt', at: ist('18:00:00'), withTodays: false })
    expect(tonightStatus(day({ withTodays: true }), at('15:00:00'))).toEqual({
      kind: 'goesAt',
      at: ist('18:00:00'),
      withTodays: true,
    })
  })

  it('today’s message ready before 6 pm: approve by 6, or it goes without it when the wait ends', () => {
    const waiting = day({ goesAt: ist('18:05:00'), waiting: 'forYou' })

    expect(tonightStatus(waiting, at('15:00:00'))).toEqual({ kind: 'approveBy', at: ist('18:00:00'), until: ist('18:05:00') })
  })

  it('after 6 pm: counts down the job’s wait, never showing 0 while it still waits', () => {
    const waiting = day({ goesAt: ist('19:15:00'), waiting: 'forYou' })

    expect(tonightStatus(waiting, at('19:12:00'))).toEqual({ kind: 'waiting', until: ist('19:15:00'), minutes: 3 })
    expect(tonightStatus(waiting, at('19:14:50'))).toMatchObject({ kind: 'waiting', minutes: 1 })
    // The screen hasn't refreshed yet but the wait is over: it goes on the job's next tick.
    expect(tonightStatus(waiting, at('19:15:00'))).toEqual({ kind: 'goingNow' })
  })

  it('being written: no time yet', () => {
    expect(tonightStatus(day({ goesAt: null, waiting: 'writing' }), at('18:01:00'))).toEqual({ kind: 'writing' })
  })

  it('due and not held: going now', () => {
    expect(tonightStatus(day(), at('18:00:30'))).toEqual({ kind: 'goingNow' })
  })

  it('sent: a receipt with the time and whether today’s message went', () => {
    expect(tonightStatus(day({ sent: 2, sentAt: ist('18:01:00'), goesAt: null, withTodays: true }), at('19:00:00'))).toEqual({
      kind: 'sent',
      at: ist('18:01:00'),
      withTodays: true,
    })
  })

  it('nothing due (blocked, or a past day not sent): no status', () => {
    expect(tonightStatus(day({ goesAt: null }), at('15:00:00'))).toEqual({ kind: 'none' })
  })
})
