/** Learning status of one missed lesson (B-08): code-computed from catch-ups and answers. */
export type LearningStatus = 'missed' | 'sent' | 'caughtUp' | 'needsBuddy'

/** How the family can be reached, a separate tag (B-08, D-39). */
export type Channel = 'stopped' | 'noPhone' | 'noConsent'

export type MissedLesson = { lessonId: string; date: string; competencyIds: string[]; status: LearningStatus }

export type LedgerEvent = {
  date: string
  kind: 'away' | 'reason' | 'catchUpSent' | 'answered' | 'stopped'
  competencyIds: string[] | null
  category: string | null
  language: string | null
  outcome: 'caughtUp' | 'needsBuddy' | null
}

export type LedgerChild = {
  childId: string
  rollNo: number
  firstName: string
  currentAbsenceDays: number
  missedLessons: MissedLesson[]
  homeLanguage: string
  channel: Channel | null
  daysAwayThisWeek: number
  history: LedgerEvent[]
}

/** GET /api/classes/{id}/ledger. `savedAt` is set when shown from the offline snapshot. */
export type Ledger = {
  classId: string
  weekOf: string
  lessons: { lessonId: string; date: string; competencyIds: string[] }[]
  children: LedgerChild[]
  savedAt?: number
}
