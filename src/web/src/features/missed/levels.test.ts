import { ledger } from './fixtures'
import { childLevel, lessonLevel, levelOf, weekSummary } from './levels'
import type { LearningStatus, LedgerChild, MissedLesson } from './types'

const lesson = (status: LearningStatus, date = '2026-10-20'): MissedLesson => ({
  lessonId: `${status}-${date}`,
  date,
  competencyIds: ['M3.ADD.CARRY'],
  status,
})

describe('learner levels from the ledger (T-97, D-130)', () => {
  it('maps a catch-up’s answer to a level; nothing sent yet is no level', () => {
    expect(lessonLevel('caughtUp')).toBe('gotIt')
    expect(lessonLevel('needsBuddy')).toBe('needsHelp')
    expect(lessonLevel('sent')).toBe('notAnswered')
    expect(lessonLevel('missed')).toBeNull()
  })

  it('one level for several lessons: Needs help first, then Not answered yet, then Got it', () => {
    expect(levelOf([lesson('caughtUp'), lesson('needsBuddy'), lesson('sent')])).toBe('needsHelp')
    expect(levelOf([lesson('caughtUp'), lesson('sent')])).toBe('notAnswered')
    expect(levelOf([lesson('caughtUp'), lesson('missed')])).toBe('gotIt')
    expect(levelOf([lesson('missed')])).toBeNull()
    expect(levelOf([])).toBeNull()
  })

  it('a child’s level counts only this week’s lessons', () => {
    const child = { missedLessons: [lesson('needsBuddy', '2026-10-16'), lesson('caughtUp', '2026-10-19')] } as LedgerChild
    expect(childLevel(child, '2026-10-19')).toBe('gotIt')
  })

  it('Week’s headline: children away this week, and how many got it or need help', () => {
    // Ravi: one lesson got, one needs help → needs help. Asha: catch-up not sent → neither.
    expect(weekSummary(ledger())).toEqual({ away: 2, gotIt: 0, needsHelp: 1 })

    const all = ledger()
    const [asha, chetan, ravi, kavya] = all.children as [LedgerChild, LedgerChild, LedgerChild, LedgerChild]
    const gotAsha = { ...asha, missedLessons: [lesson('caughtUp', '2026-10-19')] }
    const awayKavya = { ...kavya, daysAwayThisWeek: 1, missedLessons: [lesson('sent', '2026-10-21')] }
    expect(weekSummary({ ...all, children: [gotAsha, chetan, ravi, awayKavya] })).toEqual({ away: 3, gotIt: 1, needsHelp: 1 })
  })
})
