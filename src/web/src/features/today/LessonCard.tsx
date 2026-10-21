import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'

/**
 * Today's lesson, "not recorded yet" look (T1 `nolesson`). The mic, upload and lesson matching arrive with
 * T-07 PR 2, so the button is disabled for now and says so.
 */
export function LessonCard() {
  const { t } = useTranslation()

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
    </section>
  )
}
