import type { Curriculum } from '../lessons/types'
import type { Ledger, LedgerChild } from './types'

export const curriculum: Curriculum = {
  chapters: [
    {
      number: 5,
      nameKn: 'ಕಳೆಯುವ ಮೋಜು',
      nameEn: 'The fun of subtracting',
      competencies: [
        {
          id: 'M3.SUB.BORROW',
          nameKn: 'ದಶಕ ಸಹಿತ ವ್ಯವಕಲನ',
          nameEn: 'Subtraction with borrowing',
          sequence: 15,
          textbookPage: { book: 'textbook', part: 2, page: 15 },
        },
        { id: 'M3.ADD.CARRY', nameKn: 'ದಶಕ ಸಹಿತ ಸಂಕಲನ', nameEn: 'Addition with carrying', sequence: 12, textbookPage: null },
      ],
    },
  ],
}

const MON = 'mon-lesson'
const TUE = 'tue-lesson'

function child(overrides: Partial<LedgerChild> & Pick<LedgerChild, 'childId' | 'rollNo' | 'firstName'>): LedgerChild {
  return {
    currentAbsenceDays: 0,
    missedLessons: [],
    homeLanguage: 'kn',
    channel: null,
    daysAwayThisWeek: 0,
    history: [],
    ...overrides,
  }
}

/** This week (Mon 19 Oct): subtraction on Monday, addition on Tuesday. 4 children by roll, as the API sends them; Ravi and Asha were away. */
export function ledger(overrides: Partial<Ledger> = {}): Ledger {
  return {
    classId: 'class-1',
    weekOf: '2026-10-19',
    lessons: [
      { lessonId: MON, date: '2026-10-19', competencyIds: ['M3.SUB.BORROW'] },
      { lessonId: TUE, date: '2026-10-20', competencyIds: ['M3.ADD.CARRY'] },
    ],
    children: [
      child({
        childId: 'asha',
        rollNo: 1,
        firstName: 'Asha',
        daysAwayThisWeek: 1,
        channel: 'noPhone',
        missedLessons: [
          // Last week's lesson is listed by the API but is not "this week".
          { lessonId: 'old', date: '2026-10-16', competencyIds: ['M3.ADD.CARRY'], status: 'sent' },
          { lessonId: MON, date: '2026-10-19', competencyIds: ['M3.SUB.BORROW'], status: 'missed' },
        ],
      }),
      child({ childId: 'chetan', rollNo: 3, firstName: 'Chetan' }),
      child({
        childId: 'ravi',
        rollNo: 4,
        firstName: 'Ravi',
        homeLanguage: 'te',
        daysAwayThisWeek: 2,
        channel: 'stopped',
        missedLessons: [
          { lessonId: MON, date: '2026-10-19', competencyIds: ['M3.SUB.BORROW'], status: 'caughtUp' },
          { lessonId: TUE, date: '2026-10-20', competencyIds: ['M3.ADD.CARRY'], status: 'needsBuddy' },
        ],
        history: [
          { date: '2026-10-19', kind: 'away', competencyIds: ['M3.SUB.BORROW'], category: null, language: null, outcome: null },
          { date: '2026-10-19', kind: 'reason', competencyIds: null, category: 'unwell', language: null, outcome: null },
          { date: '2026-10-20', kind: 'away', competencyIds: ['M3.ADD.CARRY'], category: null, language: null, outcome: null },
          { date: '2026-10-21', kind: 'catchUpSent', competencyIds: null, category: null, language: 'te', outcome: null },
          { date: '2026-10-22', kind: 'answered', competencyIds: null, category: null, language: null, outcome: 'caughtUp' },
          { date: '2026-10-22', kind: 'stopped', competencyIds: null, category: null, language: null, outcome: null },
        ],
      }),
      child({ childId: 'kavya', rollNo: 11, firstName: 'Kavya', channel: 'noConsent' }),
    ],
    ...overrides,
  }
}
