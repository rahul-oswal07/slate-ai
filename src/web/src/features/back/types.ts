import type { TextbookPage } from '../lessons/types'

/** GET /api/classes/{id}/buddy-cards/{date}: one card per child back that day (T5). No channel, no score (B-20). */
export type BuddyCard = {
  childId: string
  firstName: string
  rollNo: number
  daysAway: number
  /** Null: "Teacher-led: 10 minutes" (D-59). */
  buddy: { childId: string; firstName: string; presentDays: number } | null
  missedDays: number
  competencyId: string | null
  /** add, sub, expanded, count, placeValue, biggest or smallest: how the three items are worded. */
  practiceKind: string | null
  /** Made by code (guardrail 4); answers are for the teacher. */
  sums: { question: string; answer: number }[]
  keyWordsKn: string[]
  homework: TextbookPage | null
  homeworkReturned: boolean
  done: boolean
}

export type BuddyCards = { date: string; cards: BuddyCard[]; savedAt?: number }
