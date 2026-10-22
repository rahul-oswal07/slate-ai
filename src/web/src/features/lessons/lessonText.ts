import type { TFunction } from 'i18next'
import type { Chapter, CurriculumLesson, TextbookPage } from './types'

/** Lesson and chapter names come from curriculum data, not locale files: Kannada UI shows Kannada names. */
export const lessonName = (lesson: { nameKn: string; nameEn: string }, language: string) =>
  language === 'kn' ? lesson.nameKn : lesson.nameEn

export function pageText(t: TFunction, page: TextbookPage | null) {
  if (!page) {
    return ''
  }
  if (page.book === 'workbook') {
    return t('lesson.workbookPage', { page: page.page })
  }
  return page.part ? t('lesson.partPage', { part: page.part, page: page.page }) : t('lesson.page', { page: page.page })
}

/** "Chapter 5 · The fun of subtracting"; the bridge course (chapter 0) has no number. */
export function chapterTitle(t: TFunction, chapter: Chapter, language: string) {
  const name = lessonName(chapter, language)
  return chapter.number > 0 ? t('pick.title', { n: chapter.number, chapter: name }) : name
}

export function findLesson(chapters: Chapter[], id: string): { lesson: CurriculumLesson; chapter: Chapter } | null {
  for (const chapter of chapters) {
    const lesson = chapter.competencies.find((c) => c.id === id)
    if (lesson) {
      return { lesson, chapter }
    }
  }
  return null
}
