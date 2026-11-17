/** Where the child finds the lesson in their own book (D-22). */
/** `nalikali`: Nalikali Grade 3 Maths 2026-27 (D-90), named on screen; `textbook` / `workbook`: the D-89 book. */
export type TextbookPage = { book: 'textbook' | 'workbook' | 'nalikali'; part: number | null; page: number }

export type CurriculumLesson = {
  id: string
  nameKn: string
  nameEn: string
  sequence: number
  textbookPage: TextbookPage | null
  /** The lesson has the book's own exercise numbers (D-93, T-128): the "Book exercises" tag. */
  hasExercises: boolean
}

export type Chapter = { number: number; nameKn: string; nameEn: string; competencies: CurriculumLesson[] }

/** GET /api/classes/{id}/curriculum (T-145) */
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
  /** Tonight's message has gone to families, so the confirmed lesson can't be changed any more (T-143, D-124). */
  sentToFamilies?: boolean
}
