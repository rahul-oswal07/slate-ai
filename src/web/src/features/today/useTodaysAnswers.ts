import { useQuery } from '@tanstack/react-query'
import { api } from '../../api/client'

/** The levels, worked out by code from the child's latest submit (D-108, changing D-95). */
export type Level = 'gotIt' | 'needsHelp' | 'notAnswered'

export interface TodaysAnswer {
  childId: string
  firstName: string
  rollNo: number
  /** The child's latest submit, or null. */
  answer: '>' | '<' | '=' | null
  level: Level
}

/** GET …/todays-message/{date}/answers (T-92). */
export interface TodaysAnswers {
  sent: number
  /** At least one submit. */
  answered: number
  /** Both submits used: that answer can't change any more. */
  locked: number
  rows: TodaysAnswer[]
}

/**
 * The answers to today's picture question (T-92). null: no today's message (204). While an answer can still come or change
 * (a family hasn't submitted, or has a try left), checks again every 5 s (only while the page is in front), so a child's
 * submit shows on Today within seconds.
 */
export function useTodaysAnswers(classId: string, date: string) {
  return useQuery({
    queryKey: ['todays-answers', classId, date],
    queryFn: async () => (await api<TodaysAnswers | undefined>(`/api/classes/${classId}/todays-message/${date}/answers`)) ?? null,
    enabled: classId !== '',
    retry: false,
    refetchInterval: (query) => {
      const data = query.state.data
      return data && data.sent > 0 && data.locked < data.sent ? 5000 : false
    },
    refetchIntervalInBackground: false,
  })
}
