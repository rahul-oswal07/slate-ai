import type { Chapter } from '../lessons/types'
import { findLesson, lessonName } from '../lessons/lessonText'

/** "1:24" from seconds (code, not the LLM: T3). */
export function duration(seconds: number) {
  const whole = Math.round(seconds)
  return `${Math.floor(whole / 60)}:${String(whole % 60).padStart(2, '0')}`
}

export function lessonNames(chapters: Chapter[], ids: string[], language: string) {
  return ids.map((id) => {
    const found = findLesson(chapters, id)
    return found ? lessonName(found.lesson, language) : id
  })
}
