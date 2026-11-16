import { type ReactNode, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { Icon } from '../components/Icon'
import { useMe } from '../features/auth/useMe'
import { setSchoolDate, stepSchoolDate, useSchoolDate } from '../features/today/schoolDate'
import { istToday } from '../features/today/schoolTime'

/** The admin switch (T-134): the demo teacher may act as a past day. */
function useSwitchedOn() {
  return useMe().data?.demo?.schoolDate === true
}

/**
 * Arrows beside a screen's date (T-134, D-120): `◀ Today, Thursday 12 November ▶` on Today (a day per step) and
 * `◀ Week of 9 Nov ▶` on Missed, Back today and Week (a week per step). Tapping the date opens the calendar. Only for the
 * demo teacher with the /admin/demo switch on; otherwise it is just `children`, as before. `children` must be inline
 * content (it goes inside a button), so the screen keeps its own heading around it.
 */
export function SchoolDateNav({ mode, children }: { mode: 'day' | 'week'; children: ReactNode }) {
  const { t } = useTranslation()
  const on = useSwitchedOn()
  const chosen = useSchoolDate()
  const calendar = useRef<HTMLInputElement>(null)

  if (!on) {
    return <>{children}</>
  }

  const step = mode === 'week' ? 7 : 1
  const arrow =
    'inline-flex min-h-target min-w-target shrink-0 items-center justify-center rounded-button border-2 border-border bg-surface align-middle text-ink disabled:opacity-40'

  return (
    <span className="inline-flex max-w-full items-center gap-1">
      <button
        type="button"
        onClick={() => stepSchoolDate(-step)}
        aria-label={t(mode === 'week' ? 'schoolDate.prevWeek' : 'schoolDate.prevDay')}
        className={arrow}
      >
        <Icon name="chevronLeft" size={20} />
      </button>
      <button
        type="button"
        onClick={() => openCalendar(calendar.current)}
        className="min-h-target min-w-0 rounded-button px-1 text-left underline decoration-dotted underline-offset-4"
      >
        {children}
        <span className="sr-only">, {t('schoolDate.choose')}</span>
      </button>
      <button
        type="button"
        onClick={() => stepSchoolDate(step)}
        disabled={!chosen}
        aria-label={t(mode === 'week' ? 'schoolDate.nextWeek' : 'schoolDate.nextDay')}
        className={arrow}
      >
        <Icon name="chevronRight" size={20} />
      </button>
      {/* The native calendar, opened from the date; one tab stop (the date button), so it is out of the tab order. */}
      <input
        ref={calendar}
        type="date"
        tabIndex={-1}
        aria-label={t('schoolDate.choose')}
        value={chosen ?? istToday()}
        max={istToday()}
        onChange={(event) => setSchoolDate(event.target.value || null)}
        className="sr-only"
      />
    </span>
  )
}

/** "Back to today", beside the date while a past day is chosen (T-134); nothing otherwise. */
export function BackToToday() {
  const { t } = useTranslation()
  const on = useSwitchedOn()
  const chosen = useSchoolDate()
  if (!on || !chosen) {
    return null
  }
  return (
    <button
      type="button"
      onClick={() => setSchoolDate(null)}
      className="min-h-target self-start text-label text-primary-strong underline underline-offset-4"
    >
      {t('schoolDate.backToday')}
    </button>
  )
}

function openCalendar(input: HTMLInputElement | null) {
  if (!input) {
    return
  }
  try {
    input.showPicker()
  } catch {
    // No showPicker (older browsers, tests) or not allowed here: focus it, which opens it on most phones.
    input.focus()
  }
}

/**
 * "◀ Week of 9 Nov ▶" under a week screen's title (Missed, Back today), with Back to today (T-134). Nothing while the
 * admin switch is off, so those screens look as before.
 */
export function SchoolWeekNav() {
  const { t, i18n } = useTranslation()
  const on = useSwitchedOn()
  const chosen = useSchoolDate()
  if (!on) {
    return null
  }
  return (
    <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
      <p className="text-label">
        <SchoolDateNav mode="week">
          {t('schoolDate.weekOf', { date: shortDate(mondayOf(chosen ?? istToday()), i18n.language) })}
        </SchoolDateNav>
      </p>
      <BackToToday />
    </div>
  )
}

/** The Monday of the school week of `date` (yyyy-mm-dd), as the server's week (CompressionPlanner.Week). */
function mondayOf(date: string) {
  const day = new Date(`${date}T12:00:00+05:30`)
  day.setUTCDate(day.getUTCDate() - ((day.getUTCDay() + 6) % 7))
  return istToday(day)
}

/** "9 Nov" in the UI language. */
function shortDate(date: string, language: string) {
  return new Intl.DateTimeFormat(language, { timeZone: 'Asia/Kolkata', day: 'numeric', month: 'short' }).format(
    new Date(`${date}T12:00:00+05:30`),
  )
}
