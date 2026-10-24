import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { type StepState, stepStates } from './stepStates'

const disc: Record<StepState, string> = {
  done: 'bg-primary text-on-primary',
  current: 'bg-accent text-on-accent',
  upcoming: 'border-[2.5px] border-dashed border-chalk bg-canvas text-ink-muted',
}

/**
 * The day at a glance: Attendance, Lesson, Tonight 6 pm. Looks only: every fact in it is on the cards below, so the
 * drawing is hidden from screen readers and the list says each step and its state in words.
 */
export function DayStrip({
  attendanceDone,
  attendanceTime,
  lessonDone,
  tonightDone,
}: {
  attendanceDone: boolean
  attendanceTime: string | null
  lessonDone: boolean
  tonightDone: boolean
}) {
  const { t } = useTranslation()
  const states = stepStates(attendanceDone, lessonDone, tonightDone)
  const steps = [
    { key: 'attendance', icon: 'check', sub: attendanceTime },
    { key: 'lesson', icon: 'mic', sub: t(lessonDone ? 'today.strip.done' : 'today.strip.next') },
    { key: 'tonight', icon: 'clock', sub: t('today.strip.tonightTime') },
  ] as const

  return (
    <ol className="flex items-start" aria-label={t('today.strip.label')}>
      {steps.map((step, i) => {
        const state = states[i] ?? 'upcoming'
        const stateName = t(`today.strip.step${state === 'done' ? 'Done' : state === 'current' ? 'Current' : 'Upcoming'}`)
        return (
          <li key={step.key} className="relative flex flex-1 flex-col items-center text-center">
            {i > 0 && (
              <span
                aria-hidden="true"
                className={`absolute right-1/2 top-[20px] h-0 w-full border-t-[3px] ${
                  states[i - 1] === 'done' ? 'border-primary' : 'border-dashed border-chalk'
                }`}
              />
            )}
            <span aria-hidden="true" className={`relative z-10 flex size-10 items-center justify-center rounded-full ${disc[state]}`}>
              <Icon name={state === 'done' ? 'check' : step.icon} size={20} strokeWidth={2.5} />
            </span>
            <span className="mt-1 text-label">
              {t(`today.strip.${step.key}`)}
              <span className="sr-only">, {stateName}</span>
            </span>
            {step.sub && <span className="text-caption text-ink-muted">{step.sub}</span>}
          </li>
        )
      })}
    </ol>
  )
}
