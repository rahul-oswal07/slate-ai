import type { EveningDay } from '../evening/types'

/**
 * The status line of Tonight's message (T-86): what happens next and when. Every time comes from the API (`goesAt`,
 * `sentAt`), which works it out with the 6 pm job's own rule; this only picks the sentence and counts the minutes.
 */
export type TonightStatus =
  /** Messages have gone: the card is a receipt. */
  | { kind: 'sent'; at: string | null; withTodays: boolean }
  /** Today's message is being written; the job holds until it is ready. */
  | { kind: 'writing' }
  /** Before the send time, today's message waits for her: at the send time if approved, else without it at `until`. */
  | { kind: 'approveBy'; at: string; until: string }
  /** After the send time, the job waits for her for `minutes` more, then sends without today's message. */
  | { kind: 'waiting'; until: string; minutes: number }
  /** Goes at `at` (with today's message if `withTodays`). */
  | { kind: 'goesAt'; at: string; withTodays: boolean }
  /** Due now: the job sends on its next tick (within a minute). */
  | { kind: 'goingNow' }
  /** Nothing goes: nobody to send to, blocked, or a past day that wasn't sent. */
  | { kind: 'none' }

export function tonightStatus(day: EveningDay, now: number): TonightStatus {
  if (day.sent > 0) {
    return { kind: 'sent', at: day.sentAt, withTodays: day.withTodays }
  }
  if (day.waiting === 'writing') {
    return { kind: 'writing' }
  }
  if (!day.goesAt) {
    return { kind: 'none' }
  }

  const goesAt = Date.parse(day.goesAt)
  if (day.waiting === 'forYou') {
    const sendsAt = `${day.date}T${day.sendsAt}+05:30`
    if (now < Date.parse(sendsAt)) {
      return { kind: 'approveBy', at: sendsAt, until: day.goesAt }
    }
    if (now < goesAt) {
      // Rounded up, so it never says 0 while it still waits.
      return { kind: 'waiting', until: day.goesAt, minutes: Math.max(1, Math.ceil((goesAt - now) / 60_000)) }
    }
  }

  return now < goesAt ? { kind: 'goesAt', at: day.goesAt, withTodays: day.withTodays } : { kind: 'goingNow' }
}
