import { useTranslation } from 'react-i18next'
import { Icon } from '../../components/Icon'
import type { Chapter } from '../lessons/types'
import { lessonNames } from '../missed/ledgerText'
import { lastWeekOf, useWeek } from './useWeek'

/**
 * The reminder for a recap she planned on last week's T6 (FR-13): shown on Today all of the following week. Nothing
 * when she skipped, didn't decide, or last week had no nudge.
 */
export function RecapCard({ classId, date, chapters }: { classId: string; date: string; chapters: Chapter[] }) {
  const { t, i18n } = useTranslation()
  const last = useWeek(classId, lastWeekOf(date)).data
  const nudge = last?.nudge
  if (last?.decision !== 'recap' || !nudge) {
    return null
  }

  const lesson = lessonNames(chapters, nudge.competencyIds, i18n.language).join(', ')
  return (
    <section
      aria-labelledby="recap-title"
      className="flex items-start gap-3 rounded-md border border-border border-l-4 border-l-primary bg-surface p-3"
    >
      <Icon name="clock" size={20} className="mt-1 shrink-0 text-primary-strong" />
      <div>
        <h2 id="recap-title" className="text-label">
          {t('week.recapTitle')}
        </h2>
        <p className="text-body">
          {nudge.nextCompetencyId
            ? t('week.recapBody', { lesson, next: lessonNames(chapters, [nudge.nextCompetencyId], i18n.language)[0] })
            : t('week.recapBodyNoNext', { lesson })}
        </p>
      </div>
    </section>
  )
}
