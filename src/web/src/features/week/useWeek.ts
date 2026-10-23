import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, NetworkError } from '../../api/client'
import { db, saveSnapshot } from '../../offline/db'
import type { Week } from './types'

export const weekKey = (classId: string, date: string) => ['week', classId, date] as const

/** A school week (any date in it). Offline it shows the last snapshot with its time (D-68). */
export function useWeek(classId: string, date: string) {
  return useQuery({
    queryKey: weekKey(classId, date),
    queryFn: async () => {
      const key = weekKey(classId, date).join(':')
      try {
        const week = await api<Week>(`/api/classes/${classId}/week/${date}`)
        await saveSnapshot(key, week)
        return week
      } catch (error) {
        const snapshot = error instanceof NetworkError ? await db.snapshots.get(key) : undefined
        if (!snapshot) {
          throw error
        }
        return { ...(snapshot.value as Week), savedAt: snapshot.savedAt }
      }
    },
    enabled: classId !== '',
    retry: false,
  })
}

/** Plan recap / Not this week / Change (FR-13). Online only (D-70); a 409 shows the latest week. */
export function useDecide(classId: string, date: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ decision, lessonId }: { decision: 'recap' | 'skip'; lessonId: string }) =>
      api<Week>(`/api/classes/${classId}/week/${date}/pacing`, {
        method: 'PUT',
        body: { clientRequestId: crypto.randomUUID(), decision, lessonId },
      }),
    onSuccess: (week) => queryClient.setQueryData(weekKey(classId, date), week),
    onError: () => void queryClient.invalidateQueries({ queryKey: weekKey(classId, date) }),
  })
}

/** The Monday a week before `date` (yyyy-mm-dd), for the recap reminder on Today. */
export function lastWeekOf(date: string) {
  const d = new Date(`${date}T00:00:00Z`)
  d.setUTCDate(d.getUTCDate() - 7)
  return d.toISOString().slice(0, 10)
}
