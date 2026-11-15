import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { TodaysMessage } from '../approve/todaysTypes'
import { useNoteQueued } from '../lessons/useLessons'
import type { LessonStatus } from '../lessons/types'
import { nextStep } from './nextStep'
import { useTodaysAnswers } from './useTodaysAnswers'

/** Brings an element on Today into view and gives it focus; no smooth scroll if the phone asks for reduced motion. */
function bringToFocus(element: HTMLElement | null) {
  if (!element) {
    return
  }

  const reduced = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false
  element.scrollIntoView?.({ block: 'center', behavior: reduced ? 'auto' : 'smooth' })
  element.focus({ preventScroll: true })
}

/**
 * The one next step on Today (T-96, G3), under the day strip: Record today's lesson → Confirm today's lesson → Check
 * today's message → See answers. The only primary button on Today; the cards below keep their own buttons, secondary.
 * Nothing shows when there is nothing for her to do now.
 */
export function NextStepButton({
  classId,
  date,
  lessonStatus,
  todays,
}: {
  classId: string
  date: string
  lessonStatus: LessonStatus | null
  todays: TodaysMessage | null | undefined
}) {
  const { t } = useTranslation()
  const noteQueued = useNoteQueued(classId, date)
  const answersSent = useTodaysAnswers(classId, date).data?.sent ?? 0
  const step = nextStep({ lessonStatus, noteQueued, todays, answersSent })
  const className = 'btn-primary flex items-center justify-center'

  switch (step) {
    case 'record':
      return (
        <button
          type="button"
          className={className}
          onClick={() => bringToFocus(document.querySelector<HTMLElement>('[data-next-step="record"]'))}
        >
          {t('today.next.record')}
        </button>
      )
    case 'confirm':
      return (
        <Link to="/lesson" className={className}>
          {t('today.next.confirm')}
        </Link>
      )
    case 'check':
      return (
        <Link to="/approve" className={className}>
          {t('today.next.check')}
        </Link>
      )
    case 'answers':
      return (
        <button type="button" className={className} onClick={() => bringToFocus(document.getElementById('answers-title'))}>
          {t('today.next.answers')}
        </button>
      )
    default:
      return null
  }
}
