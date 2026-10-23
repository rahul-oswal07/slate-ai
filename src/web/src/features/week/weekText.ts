import type { TFunction } from 'i18next'
import type { Chapter } from '../lessons/types'
import { lessonNames } from '../missed/ledgerText'
import type { PacingNudge } from './types'

/** The nudge sentence: a fixed template filled with numbers counted by code and curriculum lesson names (no AI). */
export function nudgeText(t: TFunction, nudge: PacingNudge, chapters: Chapter[], language: string) {
  const lesson = lessonNames(chapters, nudge.competencyIds, language).join(', ')
  const values = { n: nudge.n, total: nudge.total, minutes: nudge.minutes, lesson }
  return nudge.nextCompetencyId
    ? t('week.nudge', { ...values, next: lessonNames(chapters, [nudge.nextCompetencyId], language)[0] })
    : t('week.nudgeNoNext', values)
}
