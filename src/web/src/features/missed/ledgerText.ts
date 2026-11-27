import { findLesson, lessonName } from '../lessons/lessonText'
import type { Chapter } from '../lessons/types'
import type { LedgerChild, MissedLesson } from './types'

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
