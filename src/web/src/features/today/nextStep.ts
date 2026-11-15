import type { TodaysMessage } from '../approve/todaysTypes'
import type { LessonStatus } from '../lessons/types'

/** The one next step Today offers at the top (T-96, G3), or null when there is nothing for her to do now. */
export type NextStep = 'record' | 'confirm' | 'check' | 'answers' | null

/**
 * Worked out by code from the day: answers back → see them; today's message not approved yet → check it; a note heard
 * but not confirmed → confirm it; no lesson yet → record it. Approved and waiting for 6 pm, no today's message, or a
 * note still waiting to upload: nothing to do now.
 */
export function nextStep(day: {
  lessonStatus: LessonStatus | null
  noteQueued: boolean
  todays: TodaysMessage | null | undefined
  answersSent: number
}): NextStep {
  if (day.answersSent > 0) {
    return 'answers'
  }

  if (day.todays && !day.todays.sent && day.todays.status !== 'approved') {
    return 'check'
  }

  if (day.lessonStatus === 'processing' || day.lessonStatus === 'mapped' || day.lessonStatus === 'low') {
    return 'confirm'
  }

  if ((day.lessonStatus === null || day.lessonStatus === 'unclear') && !day.noteQueued) {
    return 'record'
  }

  return null
}
