import { useQuery } from '@tanstack/react-query'
import { api, ApiError } from '../../api/client'

/** D-95's levels, worked out by code from the child's taps (T-90). */
export type Level = 'gotIt' | 'gettingThere' | 'needsHelp' | 'notAnswered'

export interface TodaysAnswer {
  childId: string
  firstName: string
  rollNo: number
  /** The child's last tap, or null. */
  answer: '>' | '<' | '=' | null
  level: Level
}

/** GET …/todays-message/{date}/answers (T-92). */
export interface TodaysAnswers {
  sent: number
  answered: number
  rows: TodaysAnswer[]
}

/**
 * The answers to today's picture question (T-92). null: no today's message. While some families haven't finished it,
 * checks again every 5 s (only while the page is in front), so a child's tap shows on Today within seconds.
 */
export function useTodaysAnswers(classId: string, date: string) {
  return useQuery({
    queryKey: ['todays-answers', classId, date],
    queryFn: async () => {
      try {
        return await api<TodaysAnswers>(`/api/classes/${classId}/todays-message/${date}/answers`)
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) {
          return null
        }
        throw error
      }
    },
    enabled: classId !== '',
    retry: false,
    refetchInterval: (query) => {
      const data = query.state.data
      return data && data.sent > 0 && data.answered < data.sent ? 5000 : false
    },
    refetchIntervalInBackground: false,
  })
}
