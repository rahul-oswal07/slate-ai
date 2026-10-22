import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import { findLesson, lessonName, pageText } from '../lessons/lessonText'
import type { Chapter, LessonDay } from '../lessons/types'

/**
 * Today's lesson (T1). Not recorded: the mic (enabled with T-07 PR B) and "Or pick the lesson from the list".
 * Confirmed: every lesson with its page and one "Change" (D-33, D-55).
 */
export function LessonCard({
  day,
  chapters,
  onPick,
}: {
  day: LessonDay | undefined
  chapters: Chapter[]
  onPick: () => void
}) {
  const { t, i18n } = useTranslation()
  const confirmed = day?.lesson?.status === 'confirmed' ? day.lesson.competencyIds : []

  if (confirmed.length > 0) {
    return (
      <section aria-labelledby="lesson-title" className="rounded-md border border-border bg-surface p-[14px]">
        <h2 id="lesson-title" className="text-label">
          {confirmed.length > 1 ? t('today.lesson.titlePlural') : t('today.lesson.title')}
        </h2>
        <ul className="mt-2 flex flex-col gap-2">
          {confirmed.map((id) => {
            const found = findLesson(chapters, id)
            return (
              <li key={id} className="flex items-start gap-2">
                <Icon name="checkCircle" className="mt-0.5 shrink-0 text-primary-strong" />
                <span>
                  <span className="block text-body">{found ? lessonName(found.lesson, i18n.language) : id}</span>
                  {found && (
                    <span className="block text-caption text-ink-muted">{pageText(t, found.lesson.textbookPage)}</span>
                  )}
                </span>
              </li>
            )
          })}
        </ul>
        <button type="button" onClick={onPick} className="mt-2 min-h-target w-full text-label text-primary-strong">
          {t('today.lesson.change')}
        </button>
      </section>
    )
  }

  return (
    <section aria-labelledby="lesson-title" className="rounded-md border border-border bg-surface p-[14px] text-center">
      <h2 id="lesson-title" className="text-left text-label">
        {t('today.lesson.title')}
      </h2>
      <p className="mt-1 text-body">{t('today.lesson.prompt')}</p>
      <button
        type="button"
        disabled
        aria-describedby="lesson-not-yet"
        aria-label={t('mic.record')}
        className="mx-auto mt-3 flex size-target-mic items-center justify-center rounded-full bg-primary text-on-primary opacity-50"
      >
        <Icon name="mic" size={40} />
      </button>
      <p id="lesson-not-yet" className="mt-2 text-caption text-ink-muted">
        {t('today.lesson.notYet')}
      </p>
      <button
        type="button"
        onClick={onPick}
        // Wait for the day and the curriculum, so the sheet opens on "Next in the book".
        disabled={!day || chapters.length === 0}
        className="mt-1 min-h-target w-full text-label text-primary-strong"
      >
        {t('today.lesson.pickInstead')}
      </button>
    </section>
  )
}
