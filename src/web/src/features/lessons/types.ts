/** Where the child finds the lesson in their own book (D-22). */
export type TextbookPage = { book: 'textbook' | 'workbook'; part: number | null; page: number }

export type CurriculumLesson = {
  id: string
  nameKn: string
  nameEn: string
  sequence: number
  textbookPage: TextbookPage | null
}

export type Chapter = { number: number; nameKn: string; nameEn: string; competencies: CurriculumLesson[] }

/** GET /api/curriculum */
export type Curriculum = { chapters: Chapter[] }

/** GET/PUT /api/classes/{id}/lessons/{date} */
export type LessonStatus = 'processing' | 'mapped' | 'low' | 'unclear' | 'confirmed'

export type LessonDay = {
  date: string
  lesson: {
    /** A1's ranked match until she confirms; then what she confirmed. */
    competencyIds: string[]
    source: string
    status: LessonStatus
    summaryKn: string | null
    summaryEn: string | null
    examplesUsed: string[]
  } | null
  nextInBook: string | null
}
