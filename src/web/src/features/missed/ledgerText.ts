import type { TFunction } from 'i18next'
import { findLesson, lessonName } from '../lessons/lessonText'
import type { Chapter } from '../lessons/types'
import type { LearningStatus, LedgerChild, MissedLesson } from './types'

/** Lesson names from curriculum data (Kannada UI shows Kannada names); an unknown ID shows as itself. */
export function lessonNames(chapters: Chapter[], ids: string[], language: string) {
  return ids.map((id) => {
    const found = findLesson(chapters, id)
    return found ? lessonName(found.lesson, language) : id
  })
}

/** "Mon", "ಸೋಮ": the weekday in the app's language, from the date (no locale keys needed). */
export function dayName(date: string, language: string) {
  return new Intl.DateTimeFormat(language, { weekday: 'short', timeZone: 'UTC' }).format(new Date(`${date}T00:00:00Z`))
}

/** This week's missed lessons: the ledger lists every week. */
export const thisWeek = (child: LedgerChild, weekOf: string): MissedLesson[] =>
  child.missedLessons.filter((l) => l.date >= weekOf)

/** The row's learning tag: the most recent missed lesson this week. */
export const latestStatus = (child: LedgerChild, weekOf: string): LearningStatus | null =>
  thisWeek(child, weekOf).at(-1)?.status ?? null

export function missedText(t: TFunction, chapters: Chapter[], child: LedgerChild, weekOf: string, language: string) {
  const ids = [...new Set(thisWeek(child, weekOf).flatMap((l) => l.competencyIds))]
  return ids.length > 0
    ? t('missed.missed', { lessons: lessonNames(chapters, ids, language).join(', ') })
    : t('missed.noneMissed')
}
