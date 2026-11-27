import type { Level } from '../today/useTodaysAnswers'
import { thisWeek } from './ledgerText'
import type { LearningStatus, Ledger, LedgerChild, MissedLesson } from './types'

/**
 * A missed lesson's learner level (D-95, D-108, D-130): the catch-up's picture answer, as the ledger already counts it.
 * Right → Got it, wrong → Needs help, sent but no answer → Not answered yet. Null while no catch-up has gone.
 */
export function lessonLevel(status: LearningStatus): Level | null {
  switch (status) {
    case 'caughtUp':
      return 'gotIt'
    case 'needsBuddy':
      return 'needsHelp'
    case 'sent':
      return 'notAnswered'
    case 'missed':
      return null
  }
}

// The level that needs her most comes first, so one lesson needing help is never hidden by another that was got.
const PRIORITY: Level[] = ['needsHelp', 'notAnswered', 'gotIt']

/** One level for a set of missed lessons (D-130): Needs help if any, else Not answered yet if any, else Got it. */
export function levelOf(lessons: MissedLesson[]): Level | null {
  const levels = new Set(lessons.map((l) => lessonLevel(l.status)))
  return PRIORITY.find((level) => levels.has(level)) ?? null
}

/** The child's level for this week's missed lessons (D-130): the one level on Missed, the child view and Week. */
export function childLevel(child: LedgerChild, weekOf: string): Level | null {
  return levelOf(thisWeek(child, weekOf))
}

/** Week's headline (T-99): children away this week, and how many of them got it or need help. Counted in code. */
export type WeekSummary = { away: number; gotIt: number; needsHelp: number }

export function weekSummary(ledger: Ledger): WeekSummary {
  const away = ledger.children.filter((c) => c.daysAwayThisWeek > 0)
  const levels = away.map((c) => childLevel(c, ledger.weekOf))
  return {
    away: away.length,
    gotIt: levels.filter((l) => l === 'gotIt').length,
    needsHelp: levels.filter((l) => l === 'needsHelp').length,
  }
}
